using System;
using CardGeneration.App;
using CardGeneration.Cli;
using CardGeneration.Resources.Enums;
using CardGeneration.Services;
using Godot;

namespace CardGeneration.Ui;

public partial class MainMenu : Control
{
    private const string CardIconPath = "res://assets/icons/actions/card.svg";
    private const string DeckIconPath = "res://assets/icons/actions/deck.svg";
    private const string ExportIconPath = "res://assets/icons/actions/export.svg";
    private const string SettingsIconPath = "res://assets/icons/actions/settings.svg";

    private readonly CardToolService _cardToolService = new();

    public override void _Ready()
    {
        AppLogger.RegisterGlobalHandlers();
        AppLogger.GuiInfo($"Application starting. Log file: {AppLogger.CurrentUserLogPath} ({AppLogger.CurrentGlobalLogPath})");

        var defaultResult = _cardToolService.EnsureDefaultResources();
        if (!defaultResult.Success)
        {
            AppLogger.GuiError(defaultResult.Message);
        }
        else
        {
            AppLogger.GuiInfo(defaultResult.Message);
        }

        var userArgs = OS.GetCmdlineUserArgs();
        if (DisplayServer.GetName() == "headless" || userArgs.Length > 0)
        {
            AppLogger.Info($"Starting CLI mode with {userArgs.Length} user argument(s).", "CLI");
            RunCli(userArgs);
            return;
        }

        AppLogger.GuiInfo("Starting GUI mode.");
        BuildMenu();
    }

    private void RunCli(string[] userArgs)
    {
        var runner = new CliRunner(_cardToolService);
        GetTree().Quit(runner.Run(userArgs));
    }

    private void BuildMenu()
    {
        AppLogger.GuiInfo("Build screen: Main Menu");
        ClearChildren();
        SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);

        var background = new ColorRect
        {
            Color = new Color(0.055f, 0.048f, 0.07f)
        };
        background.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        AddChild(background);

        var margin = new MarginContainer();
        margin.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        margin.AddThemeConstantOverride("margin_left", 48);
        margin.AddThemeConstantOverride("margin_right", 48);
        margin.AddThemeConstantOverride("margin_top", 48);
        margin.AddThemeConstantOverride("margin_bottom", 48);
        AddChild(margin);

        var layout = new HBoxContainer
        {
            Alignment = BoxContainer.AlignmentMode.Center
        };
        layout.AddThemeConstantOverride("separation", 32);
        margin.AddChild(layout);

        var panel = new PanelContainer
        {
            CustomMinimumSize = new Vector2(480, 0)
        };
        layout.AddChild(panel);

        var menu = new VBoxContainer
        {
            Alignment = BoxContainer.AlignmentMode.Center
        };
        menu.AddThemeConstantOverride("separation", 12);
        panel.AddChild(menu);

        var title = new Label
        {
            Text = "Godot Card Generation",
            HorizontalAlignment = HorizontalAlignment.Center
        };
        title.AddThemeFontSizeOverride("font_size", 28);
        menu.AddChild(title);

        var subtitle = new Label
        {
            Text = "Card studio for cards, decks, preview and export.",
            HorizontalAlignment = HorizontalAlignment.Center,
            AutowrapMode = TextServer.AutowrapMode.WordSmart
        };
        menu.AddChild(subtitle);

        var grid = new GridContainer
        {
            Columns = 2,
            SizeFlagsHorizontal = SizeFlags.ShrinkCenter
        };
        grid.AddThemeConstantOverride("h_separation", 14);
        grid.AddThemeConstantOverride("v_separation", 14);
        menu.AddChild(grid);

        AddMenuButton(grid, "Cards", CardIconPath, ShowCards);
        AddMenuButton(grid, "Decks", DeckIconPath, ShowDecks);
        AddMenuButton(grid, "Export", ExportIconPath, ShowExportCenter);
        AddMenuButton(grid, "Settings", SettingsIconPath, ShowSettings);

    }

    private static void AddMenuButton(GridContainer parent, string text, string iconPath, Action? onPressed = null)
    {
        var button = new Button
        {
            Text = text,
            Icon = ResourceLoader.Load<Texture2D>(iconPath),
            ExpandIcon = true,
            IconAlignment = HorizontalAlignment.Center,
            VerticalIconAlignment = VerticalAlignment.Top,
            CustomMinimumSize = new Vector2(170, 132),
            TooltipText = text
        };
        button.Pressed += AppLogger.WrapGuiAction(
            $"Open {text}",
            onPressed ?? (() => AppLogger.GuiWarning($"{text} is not implemented yet.")));
        parent.AddChild(button);
    }

    private void ShowSettings()
    {
        ShowScreen(new SettingsPanel());
    }

    private void ShowCards()
    {
        var screen = new SavedCardsScreen();
        screen.EditCardRequested += ShowCardEditor;
        screen.NewCardRequested += ShowCardTypePicker;
        ShowScreen(screen);
    }

    private void ShowDecks()
    {
        var screen = new SavedDecksScreen();
        screen.EditDeckRequested += ShowDeckEditor;
        screen.NewDeckRequested += ShowDeckEditor;
        screen.PreviewDeckRequested += ShowDeckPreview;
        ShowScreen(screen);
    }

    private void ShowDeckPreview(CardGeneration.Resources.CardDeckResource deck)
    {
        var screen = new DeckPreviewScreen();
        screen.SetDeck(deck);
        ShowScreen(screen);
    }

    private void ShowCardEditor(CardGeneration.Resources.CardResource? card)
    {
        var screen = new CardEditorScreen();
        screen.SetCard(card);
        ShowScreen(screen);
    }

    private void ShowDeckEditor(CardGeneration.Resources.CardDeckResource? deck)
    {
        var screen = new DeckEditorScreen();
        screen.SetDeck(deck);
        ShowScreen(screen);
    }

    private void ShowCardTypePicker()
    {
        var screen = new CardTypePickerScreen();
        screen.CardTypeSelected += ShowCardEditorForType;
        ShowScreen(screen);
    }

    private void ShowCardEditorForType(CardType cardType)
    {
        ShowCardEditor(_cardToolService.CreateCard(cardType));
    }

    private void ShowExportCenter()
    {
        ShowScreen(new ExportCenterScreen());
    }

    private void ShowScreen(CardToolScreen screen)
    {
        AppLogger.GuiInfo($"Navigate to {screen.GetType().Name}.");
        ClearChildren();
        SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        screen.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        screen.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        screen.SizeFlagsVertical = SizeFlags.ExpandFill;
        screen.Setup(_cardToolService);
        screen.BackRequested += BuildMenu;
        AddChild(screen);
    }

    private void ClearChildren()
    {
        foreach (var child in GetChildren())
        {
            RemoveChild(child);
            child.QueueFree();
        }
    }
}
