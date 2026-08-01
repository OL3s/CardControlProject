using System;
using CardGeneration.Cli;
using CardGeneration.Resources;
using CardGeneration.Services;
using Godot;

namespace CardGeneration.Ui;

public partial class MainMenu : Control
{
    private readonly CardToolService _cardToolService = new();

    public override void _Ready()
    {
        var userArgs = OS.GetCmdlineUserArgs();
        if (DisplayServer.GetName() == "headless" || userArgs.Length > 0)
        {
            RunCli(userArgs);
            return;
        }

        BuildMenu();
    }

    private void RunCli(string[] userArgs)
    {
        var runner = new CliRunner(_cardToolService);
        GetTree().Quit(runner.Run(userArgs));
    }

    private void BuildMenu()
    {
        ClearChildren();
        SetAnchorsPreset(Control.LayoutPreset.FullRect);

        var background = new ColorRect
        {
            Color = new Color(0.055f, 0.048f, 0.07f)
        };
        background.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        AddChild(background);

        var margin = new MarginContainer();
        margin.SetAnchorsPreset(Control.LayoutPreset.FullRect);
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
            Text = "Card studio for saved cards, decks, preview and export.",
            HorizontalAlignment = HorizontalAlignment.Center,
            AutowrapMode = TextServer.AutowrapMode.WordSmart
        };
        menu.AddChild(subtitle);

        AddMenuButton(menu, "Saved Cards");
        AddMenuButton(menu, "Saved Decks");
        AddMenuButton(menu, "New Card");
        AddMenuButton(menu, "New Deck");
        AddMenuButton(menu, "Export");
        AddMenuButton(menu, "Settings", ShowSettings);

        var preview = BuildSamplePreview();
        if (preview is not null)
        {
            layout.AddChild(preview);
        }
    }

    private static void AddMenuButton(VBoxContainer parent, string text, Action? onPressed = null)
    {
        var button = new Button
        {
            Text = text,
            CustomMinimumSize = new Vector2(0, 44)
        };
        button.Pressed += onPressed ?? (() => GD.Print($"{text} is not implemented yet."));
        parent.AddChild(button);
    }

    private void ShowSettings()
    {
        ClearChildren();
        SetAnchorsPreset(Control.LayoutPreset.FullRect);

        var background = new ColorRect
        {
            Color = new Color(0.055f, 0.048f, 0.07f)
        };
        background.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        AddChild(background);

        var center = new CenterContainer();
        center.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        AddChild(center);

        var settingsPanel = new SettingsPanel();
        settingsPanel.BackRequested += BuildMenu;
        center.AddChild(settingsPanel);
    }

    private static TextureRect? BuildSamplePreview()
    {
        var card = new CardRepository().LoadCardById("monster_flame_1_a");
        if (card is null)
        {
            return null;
        }

        var preview = new CardPreviewControl
        {
            CustomMinimumSize = new Vector2(260, 364),
            ExpandMode = TextureRect.ExpandModeEnum.FitWidthProportional,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered
        };
        preview.SetCard(card);
        return preview;
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
