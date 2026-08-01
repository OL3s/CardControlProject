using CardGeneration.Cli;
using CardGeneration.Services;
using Godot;

namespace CardGeneration.Ui;

public partial class MainMenu : Control
{
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
        var service = new CardToolService();
        var runner = new CliRunner(service);
        GetTree().Quit(runner.Run(userArgs));
    }

    private void BuildMenu()
    {
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

        var panel = new PanelContainer
        {
            CustomMinimumSize = new Vector2(480, 0)
        };
        center.AddChild(panel);

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
    }

    private static void AddMenuButton(VBoxContainer parent, string text)
    {
        var button = new Button
        {
            Text = text,
            CustomMinimumSize = new Vector2(0, 44)
        };
        button.Pressed += () => GD.Print($"{text} is not implemented yet.");
        parent.AddChild(button);
    }
}
