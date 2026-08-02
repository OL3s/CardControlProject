using System;
using CardGeneration.App;
using CardGeneration.Resources;
using CardGeneration.Services;
using Godot;

namespace CardGeneration.Ui;

public partial class SettingsPanel : PanelContainer
{
    private const string BackIconPath = "res://assets/icons/actions/back.svg";
    private const string ClearIconPath = "res://assets/icons/actions/clear.svg";
    private const string DeleteIconPath = "res://assets/icons/actions/delete.svg";
    private const string RefreshIconPath = "res://assets/icons/actions/refresh.svg";
    private const string SaveIconPath = "res://assets/icons/actions/save.svg";

    private readonly CardToolService _cardToolService = new();
    private LineEdit _defaultCardId = null!;
    private LineEdit _defaultDeckId = null!;
    private LineEdit _defaultOutputPath = null!;
    private OptionButton _defaultFormat = null!;
    private OptionButton _defaultPaper = null!;
    private OptionButton _defaultDpi = null!;
    private OptionButton _defaultBackMirror = null!;
    private OptionButton _defaultDeckLayout = null!;
    private SpinBox _defaultGridColumns = null!;
    private SpinBox _defaultSpacing = null!;
    private Label _status = null!;

    public event Action? BackRequested;

    public override void _Ready()
    {
        BuildUi();
        LoadConfigIntoFields();
    }

    private void BuildUi()
    {
        CustomMinimumSize = new Vector2(720, 460);

        var scroll = new ScrollContainer
        {
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
            VerticalScrollMode = ScrollContainer.ScrollMode.Auto
        };
        scroll.SetAnchorsPreset(LayoutPreset.FullRect);
        AddChild(scroll);

        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_left", 18);
        margin.AddThemeConstantOverride("margin_right", 18);
        margin.AddThemeConstantOverride("margin_top", 18);
        margin.AddThemeConstantOverride("margin_bottom", 18);
        scroll.AddChild(margin);

        var form = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            CustomMinimumSize = new Vector2(560, 0)
        };
        form.AddThemeConstantOverride("separation", 10);
        margin.AddChild(form);

        var title = new Label
        {
            Text = "Settings",
            HorizontalAlignment = HorizontalAlignment.Left
        };
        title.AddThemeFontSizeOverride("font_size", 26);
        form.AddChild(title);

        var description = new Label
        {
            Text = "These are startup and CLI defaults. Change one-off export choices in Export; CLI flags override these for one run.",
            HorizontalAlignment = HorizontalAlignment.Left,
            AutowrapMode = TextServer.AutowrapMode.WordSmart
        };
        form.AddChild(description);

        _defaultCardId = AddLineEdit(form, "Default Card ID");
        _defaultDeckId = AddLineEdit(form, "Default Deck ID");
        _defaultOutputPath = AddLineEdit(form, "Default Output Path");
        _defaultFormat = AddOptionButton(form, "Default Format", ["png"]);
        _defaultPaper = AddOptionButton(form, "Default Paper", ["a4", "a3"]);
        _defaultDpi = AddOptionButton(form, "Default DPI", ["150", "300", "600", "1200"]);
        _defaultBackMirror = AddOptionButton(form, "Default Back Mirror", ["none", "width", "height", "both"]);
        _defaultDeckLayout = AddOptionButton(form, "Default Deck Layout", ["individual", "grid", "strip"]);
        _defaultGridColumns = AddSpinBox(form, "Default Grid Columns", 0, 24, 1);
        _defaultSpacing = AddSpinBox(form, "Default Spacing", 0, 256, 1);

        _status = new Label
        {
            Text = string.Empty,
            HorizontalAlignment = HorizontalAlignment.Center,
            AutowrapMode = TextServer.AutowrapMode.WordSmart
        };
        form.AddChild(_status);

        var buttons = new HBoxContainer
        {
            Alignment = BoxContainer.AlignmentMode.Center
        };
        buttons.AddThemeConstantOverride("separation", 10);
        form.AddChild(buttons);

        var saveButton = CreateIconButton(SaveIconPath, "Save settings");
        saveButton.Pressed += SaveSettings;
        buttons.AddChild(saveButton);

        var reloadButton = CreateIconButton(RefreshIconPath, "Reload");
        reloadButton.Pressed += LoadConfigIntoFields;
        buttons.AddChild(reloadButton);

        var resetConfigButton = CreateIconButton(ClearIconPath, "Reset settings defaults");
        resetConfigButton.Pressed += ResetSettingsDefaults;
        buttons.AddChild(resetConfigButton);

        var resetContentButton = CreateIconButton(DeleteIconPath, "Delete saved cards/decks and regenerate defaults");
        resetContentButton.Pressed += ResetSavedContent;
        buttons.AddChild(resetContentButton);

        var backButton = CreateIconButton(BackIconPath, "Back");
        backButton.Pressed += () => BackRequested?.Invoke();
        buttons.AddChild(backButton);
    }

    private static Button CreateIconButton(string iconPath, string tooltip)
    {
        return new Button
        {
            Icon = ResourceLoader.Load<Texture2D>(iconPath),
            ExpandIcon = true,
            TooltipText = tooltip,
            CustomMinimumSize = new Vector2(42, 40)
        };
    }

    private void LoadConfigIntoFields()
    {
        var config = _cardToolService.LoadConfig();
        _defaultCardId.Text = config.DefaultCardId;
        _defaultDeckId.Text = config.DefaultDeckId;
        _defaultOutputPath.Text = config.DefaultOutputPath;
        SelectOption(_defaultFormat, config.DefaultFormat);
        SelectOption(_defaultPaper, config.DefaultPaper);
        SelectOption(_defaultDpi, config.DefaultDpi.ToString());
        SelectOption(_defaultBackMirror, config.DefaultBackMirror);
        SelectOption(_defaultDeckLayout, config.DefaultDeckLayout);
        _defaultGridColumns.Value = config.DefaultGridColumns;
        _defaultSpacing.Value = config.DefaultSpacing;
        _status.Text = $"Loaded {ConfigRepository.ConfigPath}";
    }

    private void SaveSettings()
    {
        var result = _cardToolService.SetConfig(new CardToolConfigUpdate
        {
            DefaultCardId = _defaultCardId.Text,
            DefaultDeckId = _defaultDeckId.Text,
            DefaultOutputPath = _defaultOutputPath.Text,
            DefaultFormat = GetSelectedText(_defaultFormat),
            DefaultPaper = GetSelectedText(_defaultPaper),
            DefaultDpi = int.Parse(GetSelectedText(_defaultDpi)),
            DefaultBackMirror = GetSelectedText(_defaultBackMirror),
            DefaultDeckLayout = GetSelectedText(_defaultDeckLayout),
            DefaultGridColumns = (int)_defaultGridColumns.Value,
            DefaultSpacing = (int)_defaultSpacing.Value
        });

        _status.Text = result.Message;
        if (!result.Success)
        {
            GD.PushError(result.Message);
        }
    }

    private void ResetSettingsDefaults()
    {
        var result = _cardToolService.ResetConfig();
        LoadConfigIntoFields();
        _status.Text = result.Message;
        if (!result.Success)
        {
            GD.PushError(result.Message);
        }
    }

    private void ResetSavedContent()
    {
        var result = _cardToolService.ResetSavedContent();
        _status.Text = result.Message;
        if (!result.Success)
        {
            GD.PushError(result.Message);
        }
    }

    private static LineEdit AddLineEdit(VBoxContainer form, string labelText)
    {
        var label = new Label { Text = labelText };
        form.AddChild(label);

        var lineEdit = new LineEdit
        {
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
        };
        form.AddChild(lineEdit);
        return lineEdit;
    }

    private static OptionButton AddOptionButton(VBoxContainer form, string labelText, string[] options)
    {
        var label = new Label { Text = labelText };
        form.AddChild(label);

        var optionButton = new OptionButton
        {
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
        };
        foreach (var option in options)
        {
            optionButton.AddItem(option);
        }

        form.AddChild(optionButton);
        return optionButton;
    }

    private static SpinBox AddSpinBox(VBoxContainer form, string labelText, double minValue, double maxValue, double step)
    {
        var label = new Label { Text = labelText };
        form.AddChild(label);

        var spinBox = new SpinBox
        {
            MinValue = minValue,
            MaxValue = maxValue,
            Step = step,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
        };
        form.AddChild(spinBox);
        return spinBox;
    }

    private static void SelectOption(OptionButton optionButton, string value)
    {
        for (var index = 0; index < optionButton.ItemCount; index++)
        {
            if (optionButton.GetItemText(index) == value)
            {
                optionButton.Select(index);
                return;
            }
        }
    }

    private static string GetSelectedText(OptionButton optionButton)
    {
        return optionButton.GetItemText(optionButton.Selected);
    }
}
