using Sektor.TurnBased.Core;
using Sektor.TurnBased.Core.Abstractions;
using Xunit;

namespace Sektor.TurnBased.Core.Tests;

/// <summary>
/// Тесты пайплайна фаз: переходы, приостановки, команды, вложенные пайплайны.
/// </summary>
public class GamePipelineTests
{
    private sealed class EmptyState : IGameState
    {
    }

    private sealed class MarkerPhase : IGamePhase
    {
        public string Id { get; }
        public int EnterCount { get; private set; }
        public int ExitCount { get; private set; }
        public int ExecuteCount { get; private set; }
        public PhaseTransition NextTransition { get; set; }

        public MarkerPhase(string id, PhaseTransition nextTransition)
        {
            Id = id;
            NextTransition = nextTransition;
        }

        public Result OnEnter(GameContext context)
        {
            EnterCount++;
            return Result.Success();
        }

        public Result<PhaseTransition> Execute(GameContext context)
        {
            ExecuteCount++;
            return Result<PhaseTransition>.Success(NextTransition);
        }

        public Result OnExit(GameContext context)
        {
            ExitCount++;
            return Result.Success();
        }
    }

    private sealed class CommandPhase : IGamePhase
    {
        public string Id { get; }
        public bool IsSuspended { get; private set; } = true;
        public int CommandCount { get; private set; }
        public int ExecuteCount { get; private set; }

        public CommandPhase(string id) => Id = id;

        public Result<PhaseTransition> Execute(GameContext context)
        {
            ExecuteCount++;
            return IsSuspended
                ? Result<PhaseTransition>.Success(PhaseTransition.Suspend())
                : Result<PhaseTransition>.Success(PhaseTransition.Next("end"));
        }

        public Result<PhaseTransition?> OnCommand(GameContext context, IGameCommand command)
        {
            CommandCount++;
            if (command is ResumeCommand)
            {
                IsSuspended = false;
                return Result<PhaseTransition?>.Success(PhaseTransition.Resume());
            }
            return Result<PhaseTransition?>.Success(null);
        }
    }

    private sealed class ResumeCommand : IGameCommand
    {
    }

    private sealed class StopCommand : IGameCommand
    {
    }

    private sealed class AdvanceOnlyCommandPhase : IGamePhase
    {
        public string Id { get; }
        public int CommandCount { get; private set; }

        public AdvanceOnlyCommandPhase(string id) => Id = id;

        public Result<PhaseTransition> Execute(GameContext context) =>
            Result<PhaseTransition>.Success(PhaseTransition.Suspend());

        public Result<PhaseTransition?> OnCommand(GameContext context, IGameCommand command)
        {
            CommandCount++;
            return Result<PhaseTransition?>.Success(null);
        }
    }

    /// <summary>
    /// Фаза-владелец: при первом Execute создаёт дочерний пайплайн и стартует его,
    /// затем приостанавливается в ожидании завершения ребёнка. При повторном Execute
    /// (после завершения ребёнка) переходит к "end".
    /// </summary>
    private sealed class ChildOwnerPhase : IGamePhase
    {
        private readonly IGamePhase _childPhase;
        private GamePipeline? _pipeline;
        private bool _childCreated;

        public string Id { get; }

        public ChildOwnerPhase(string id, IGamePhase childPhase)
        {
            Id = id;
            _childPhase = childPhase;
        }

        public void Bind(GamePipeline pipeline) => _pipeline = pipeline;

        public Result<PhaseTransition> Execute(GameContext context)
        {
            if (!_childCreated)
            {
                _childCreated = true;
                var child = _pipeline!.CreateChildPipeline();
                child.Register(_childPhase);
                child.Start(_childPhase.Id);
                return Result<PhaseTransition>.Success(PhaseTransition.Suspend("awaiting_child"));
            }

            return Result<PhaseTransition>.Success(PhaseTransition.Next("end"));
        }
    }

    private static (GameContext context, GamePipeline pipeline) CreatePipeline()
    {
        var context = new GameContext(new EmptyState(), rng: new DeterministicRng(1));
        var pipeline = new GamePipeline(context);
        return (context, pipeline);
    }

    [Fact]
    public void Start_OnUnknownPhase_Fails()
    {
        var (_, pipeline) = CreatePipeline();
        var result = pipeline.Start("missing");
        Assert.True(result.IsFailure);
        Assert.False(pipeline.IsStarted);
    }

    [Fact]
    public void Register_DuplicateId_Fails()
    {
        var (_, pipeline) = CreatePipeline();
        Assert.True(pipeline.Register(new MarkerPhase("a", PhaseTransition.Next("b"))).IsSuccess);
        var result = pipeline.Register(new MarkerPhase("a", PhaseTransition.Next("b")));
        Assert.True(result.IsFailure);
    }

    [Fact]
    public void Start_ThenAdvance_ExecutesPhasesInOrder()
    {
        var (_, pipeline) = CreatePipeline();
        var a = new MarkerPhase("a", PhaseTransition.Next("b"));
        var b = new MarkerPhase("b", PhaseTransition.Finish());

        Assert.True(pipeline.Register(a).IsSuccess);
        Assert.True(pipeline.Register(b).IsSuccess);
        Assert.True(pipeline.Start("a").IsSuccess);

        Assert.True(pipeline.Advance().IsSuccess);
        Assert.Equal("b", pipeline.CurrentPhaseId);
        Assert.Equal(1, a.EnterCount);
        Assert.Equal(1, a.ExitCount);
        Assert.Equal(1, a.ExecuteCount);

        Assert.True(pipeline.Advance().IsSuccess);
        Assert.True(pipeline.IsFinished);
        Assert.Equal(1, b.ExitCount);
    }

    [Fact]
    public void Suspend_StopsAdvancing_UntilCommandResumes()
    {
        var (_, pipeline) = CreatePipeline();
        var phase = new CommandPhase("wait");
        Assert.True(pipeline.Register(phase).IsSuccess);
        Assert.True(pipeline.Register(new MarkerPhase("end", PhaseTransition.Finish())).IsSuccess);
        Assert.True(pipeline.Start("wait").IsSuccess);

        Assert.True(pipeline.Advance().IsSuccess);
        Assert.True(pipeline.IsSuspended);
        Assert.Equal("wait", pipeline.CurrentPhaseId);

        Assert.True(pipeline.Advance().IsSuccess);
        Assert.Equal(1, phase.ExecuteCount);

        Assert.True(pipeline.ProcessCommand(new ResumeCommand()).IsSuccess);
        Assert.False(pipeline.IsSuspended);

        Assert.True(pipeline.Advance().IsSuccess);
        Assert.Equal("end", pipeline.CurrentPhaseId);
        Assert.Equal(2, phase.ExecuteCount);

        Assert.True(pipeline.Advance().IsSuccess);
        Assert.True(pipeline.IsFinished);
    }

    [Fact]
    public void ProcessCommand_ForwardsToSuspendedChild()
    {
        var (_, pipeline) = CreatePipeline();
        var childPhase = new AdvanceOnlyCommandPhase("child");
        var owner = new ChildOwnerPhase("owner", childPhase);
        Assert.True(pipeline.Register(owner).IsSuccess);
        Assert.True(pipeline.Register(new MarkerPhase("end", PhaseTransition.Finish())).IsSuccess);
        Assert.True(pipeline.Start("owner").IsSuccess);

        Assert.True(pipeline.Advance().IsSuccess);
        Assert.True(pipeline.IsSuspended);
        Assert.Single(pipeline.Children);

        Assert.True(pipeline.Advance().IsSuccess);
        Assert.True(pipeline.Children[0].IsSuspended);

        var result = pipeline.ProcessCommand(new StopCommand());
        Assert.True(result.IsSuccess);
        Assert.Equal(1, childPhase.CommandCount);
    }

    [Fact]
    public void ChildPipeline_AdvancesAndParentResumes()
    {
        var (_, pipeline) = CreatePipeline();
        var childPhase = new MarkerPhase("inner", PhaseTransition.Finish());
        var owner = new ChildOwnerPhase("owner", childPhase);
        Assert.True(pipeline.Register(owner).IsSuccess);
        Assert.True(pipeline.Register(new MarkerPhase("end", PhaseTransition.Finish())).IsSuccess);
        Assert.True(pipeline.Start("owner").IsSuccess);

        Assert.True(pipeline.Advance().IsSuccess);
        Assert.True(pipeline.IsSuspended);
        Assert.Single(pipeline.Children);

        Assert.True(pipeline.Advance().IsSuccess);
        Assert.Equal("end", pipeline.CurrentPhaseId);
        Assert.Equal(1, childPhase.ExitCount);
    }

    [Fact]
    public void JumpTo_ExitsCurrent_AndEntersTarget()
    {
        var (_, pipeline) = CreatePipeline();
        var a = new MarkerPhase("a", PhaseTransition.Suspend());
        var b = new MarkerPhase("b", PhaseTransition.Finish());
        Assert.True(pipeline.Register(a).IsSuccess);
        Assert.True(pipeline.Register(b).IsSuccess);
        Assert.True(pipeline.Start("a").IsSuccess);

        var result = pipeline.JumpTo("b");

        Assert.True(result.IsSuccess);
        Assert.Equal("b", pipeline.CurrentPhaseId);
        Assert.Equal(1, a.ExitCount);
        Assert.Equal(1, b.EnterCount);
    }

    [Fact]
    public void Stop_ExitsCurrentPhase_AndClearsState()
    {
        var (_, pipeline) = CreatePipeline();
        var a = new MarkerPhase("a", PhaseTransition.Suspend());
        Assert.True(pipeline.Register(a).IsSuccess);
        Assert.True(pipeline.Start("a").IsSuccess);

        pipeline.Stop();

        Assert.Null(pipeline.CurrentPhaseId);
        Assert.False(pipeline.IsSuspended);
        Assert.Equal(1, a.ExitCount);
        Assert.True(pipeline.IsFinished);
    }

    [Fact]
    public void Resume_ReExecutesCurrentPhase()
    {
        var (_, pipeline) = CreatePipeline();
        var a = new MarkerPhase("a", PhaseTransition.Suspend());
        Assert.True(pipeline.Register(a).IsSuccess);
        Assert.True(pipeline.Start("a").IsSuccess);

        Assert.True(pipeline.Advance().IsSuccess);
        Assert.True(pipeline.Resume().IsSuccess);
        Assert.Equal(2, a.ExecuteCount);
    }

    [Fact]
    public void Advance_WhenNotStarted_Fails()
    {
        var (_, pipeline) = CreatePipeline();
        var result = pipeline.Advance();
        Assert.True(result.IsFailure);
    }
}
