using Sektor.TurnBased.Core;
using Sektor.TurnBased.Core.Abstractions;
using Sektor.TurnBased.Dialog.Model;

namespace Sektor.TurnBased.Dialog.Content;

/// <summary>
/// Демо-контент квеста: основной диалог с ветвлениями по флагам и вложенный
/// диалог (загадка сфинкса), запускаемый через дочерний пайплайн ядра.
/// Регистрирует узлы в ContentRegistry по Id и возвращает типизированный DialogContent.
/// </summary>
public static class DialogContentCatalog
{
    public const string StartNode = "intro";

    public static Result<DialogContent> Build(ContentRegistry content)
    {
        var failures = new List<string>();

        var flags = new List<string> { "papers_stolen", "riddle_key" };

        var nodes = new List<DialogNodeDefinition>
        {
            new(
                Id: "intro",
                Text: "У ворот крепости стоит стражник.",
                Choices: new List<DialogChoiceDefinition>
                {
                    new("approach", "Подойти", NextNodeId: "talk", RequiresFlags: Array.Empty<string>(), GrantsFlags: Array.Empty<string>()),
                },
                RequiresFlags: Array.Empty<string>(),
                GrantsFlags: Array.Empty<string>()),

            new(
                Id: "talk",
                Text: "Стражник: 'Документы!'",
                Choices: new List<DialogChoiceDefinition>
                {
                    new("pickpocket", "Обыскать карманы", NextNodeId: "guard_check", RequiresFlags: Array.Empty<string>(), GrantsFlags: new[] { "papers_stolen" }),
                    new("persuade", "Убедить словами", NextNodeId: "persuade_try", RequiresFlags: Array.Empty<string>(), GrantsFlags: Array.Empty<string>()),
                },
                RequiresFlags: Array.Empty<string>(),
                GrantsFlags: Array.Empty<string>()),

            new(
                Id: "guard_check",
                Text: "Документы в порядке. Ворота открыты.",
                Choices: new List<DialogChoiceDefinition>
                {
                    new("enter", "Войти в крепость", NextNodeId: "keep", RequiresFlags: Array.Empty<string>(), GrantsFlags: Array.Empty<string>()),
                },
                RequiresFlags: new[] { "papers_stolen" },
                GrantsFlags: Array.Empty<string>()),

            new(
                Id: "persuade_try",
                Text: "Стражник не верит и зовёт подмогу.",
                Choices: new List<DialogChoiceDefinition>
                {
                    new("fight", "Сражаться", NextNodeId: "fight_end", RequiresFlags: Array.Empty<string>(), GrantsFlags: Array.Empty<string>()),
                    new("run", "Бежать", NextNodeId: "run_end", RequiresFlags: Array.Empty<string>(), GrantsFlags: Array.Empty<string>()),
                    new("sneak_past", "Всё же проскользнуть", NextNodeId: "guard_check", RequiresFlags: Array.Empty<string>(), GrantsFlags: Array.Empty<string>()),
                    new("threaten", "Пригрозить", NextNodeId: "threaten_end", RequiresFlags: new[] { "papers_stolen" }, GrantsFlags: Array.Empty<string>()),
                },
                RequiresFlags: Array.Empty<string>(),
                GrantsFlags: Array.Empty<string>()),

            new(
                Id: "keep",
                Text: "Внутри — руническая дверь. Она требует ключевое слово.",
                Choices: Array.Empty<DialogChoiceDefinition>(),
                RequiresFlags: Array.Empty<string>(),
                GrantsFlags: Array.Empty<string>(),
                SubDialogId: "sub_riddle_root",
                ContinueNodeId: "after_riddle"),

            new(
                Id: "sub_riddle_root",
                Text: "Сфинкс: 'Что растёт без корней?'",
                Choices: new List<DialogChoiceDefinition>
                {
                    new("guess_sun", "Солнце", NextNodeId: "riddle_wrong", RequiresFlags: Array.Empty<string>(), GrantsFlags: Array.Empty<string>()),
                    new("guess_time", "Время", NextNodeId: "riddle_right", RequiresFlags: Array.Empty<string>(), GrantsFlags: new[] { "riddle_key" }),
                },
                RequiresFlags: Array.Empty<string>(),
                GrantsFlags: Array.Empty<string>()),

            new(
                Id: "riddle_wrong",
                Text: "Неверно! Попробуй ещё.",
                Choices: new List<DialogChoiceDefinition>
                {
                    new("try_again", "Попробовать снова", NextNodeId: "sub_riddle_root", RequiresFlags: Array.Empty<string>(), GrantsFlags: Array.Empty<string>()),
                },
                RequiresFlags: Array.Empty<string>(),
                GrantsFlags: Array.Empty<string>()),

            new(
                Id: "riddle_right",
                Text: "Верно! Сфинкс дарует ключ.",
                Choices: Array.Empty<DialogChoiceDefinition>(),
                RequiresFlags: Array.Empty<string>(),
                GrantsFlags: Array.Empty<string>()),

            new(
                Id: "after_riddle",
                Text: "Дверь открыта. Внутри — сокровище.",
                Choices: new List<DialogChoiceDefinition>
                {
                    new("take", "Забрать сокровище", NextNodeId: "treasure_end", RequiresFlags: Array.Empty<string>(), GrantsFlags: Array.Empty<string>()),
                    new("leave", "Уйти", NextNodeId: "leave_end", RequiresFlags: Array.Empty<string>(), GrantsFlags: Array.Empty<string>()),
                },
                RequiresFlags: new[] { "riddle_key" },
                GrantsFlags: Array.Empty<string>()),

            new("fight_end", "Вы погибли в бою. Конец.", Array.Empty<DialogChoiceDefinition>(), Array.Empty<string>(), Array.Empty<string>()),
            new("run_end", "Вы сбежали. Конец.", Array.Empty<DialogChoiceDefinition>(), Array.Empty<string>(), Array.Empty<string>()),
            new("threaten_end", "Стражник отступает. Конец.", Array.Empty<DialogChoiceDefinition>(), Array.Empty<string>(), Array.Empty<string>()),
            new("treasure_end", "Сокровище ваше. Победа!", Array.Empty<DialogChoiceDefinition>(), Array.Empty<string>(), Array.Empty<string>()),
            new("leave_end", "Вы ушли ни с чем. Конец.", Array.Empty<DialogChoiceDefinition>(), Array.Empty<string>(), Array.Empty<string>()),
        };

        foreach (var node in nodes)
        {
            var result = content.Register(node.Id, node);
            if (result.IsFailure)
                failures.Add(result.Error!);
        }

        if (failures.Count > 0)
            return Result<DialogContent>.Failure(string.Join("; ", failures));

        return Result<DialogContent>.Success(new DialogContent(nodes, StartNode, flags));
    }
}
