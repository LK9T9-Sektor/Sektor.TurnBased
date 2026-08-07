using CommunityToolkit.Mvvm.ComponentModel;

namespace Sektor.TurnBased.UI.ViewModels.Lobby;

/// <summary>
/// Вариант игры в лобби: заголовок, подзаголовок и признак выбора.
/// Держит идентификатор игры (см. GameKinds); выбор подсвечивается карточкой.
/// </summary>
public sealed partial class LobbyGameOption : ObservableObject
{
    public string Kind { get; }

    public string Title { get; }

    public string Subtitle { get; }

    [ObservableProperty]
    private bool isSelected;

    public LobbyGameOption(string kind, string title, string subtitle)
    {
        Kind = kind;
        Title = title;
        Subtitle = subtitle;
    }
}
