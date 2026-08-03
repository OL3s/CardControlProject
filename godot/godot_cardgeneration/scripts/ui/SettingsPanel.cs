using System;
using CardGeneration.App;
using CardGeneration.Services;
using Godot;

namespace CardGeneration.Ui;

public partial class SettingsPanel : CardToolScreen
{
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
    private SpinBox _defaultPrintCompensation = null!;

    public override void _Ready()
    {
        BuildUi();
        LoadConfigIntoFields();
    }

    private void BuildUi()
    {
        var form = BuildScreen("Settings", "Startup and CLI defaults. Change one-off export choices in Export; CLI flags override these for one run.");

        _defaultCardId = AddLineEdit(form, "Default Card ID");
        _defaultDeckId = AddLineEdit(form, "Default Deck ID");
        _defaultOutputPath = AddLineEdit(form, "Default Output Path");
        _defaultFormat = AddOptionButton(form, "Default Format", ["png"]);
        _defaultPaper = AddOptionButton(form, "Default Paper", ["A4", "A3"]);
        _defaultDpi = AddOptionButton(form, "Default DPI", ["150", "300", "600", "1200"]);
        _defaultBackMirror = AddOptionButton(form, "Default Back Mirror", ["none", "width", "height", "both"]);
        _defaultDeckLayout = AddOptionButton(form, "Default Deck Layout", ["individual", "grid", "strip"]);
        _defaultGridColumns = AddSpinBox(form, "Default Grid Columns", 0, 24, 1);
        _defaultSpacing = AddSpinBox(form, "Default Spacing", 0, 256, 1);
        _defaultPrintCompensation = AddSpinBox(form, "Default Print Compensation (%)", PrintSheetLayout.MinCompensationPercent, PrintSheetLayout.MaxCompensationPercent, 0.1);

        var buttons = new HBoxContainer
        {
            Alignment = BoxContainer.AlignmentMode.Begin
        };
        buttons.AddThemeConstantOverride("separation", 10);
        form.AddChild(buttons);

        AddIconButton(buttons, SaveIconPath, "Save settings", SaveSettings);
        AddIconButton(buttons, RefreshIconPath, "Reload", LoadConfigIntoFields);
        AddIconButton(buttons, ClearIconPath, "Reset settings defaults", ResetSettingsDefaults);
        AddIconButton(buttons, DeleteIconPath, "Delete saved cards/decks and regenerate defaults", ResetSavedContent);
    }

    private void LoadConfigIntoFields()
    {
        var config = CardToolService.LoadConfig();
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
        _defaultPrintCompensation.Value = config.DefaultPrintCompensationPercent;
        SetStatus($"Loaded {ConfigRepository.ConfigPath}");
    }

    private void SaveSettings()
    {
        var result = CardToolService.SetConfig(new CardToolConfigUpdate
        {
            DefaultCardId = _defaultCardId.Text,
            DefaultDeckId = _defaultDeckId.Text,
            DefaultOutputPath = _defaultOutputPath.Text,
            DefaultFormat = GetSelectedText(_defaultFormat),
            DefaultPaper = GetSelectedText(_defaultPaper).ToLowerInvariant(),
            DefaultDpi = int.Parse(GetSelectedText(_defaultDpi)),
            DefaultBackMirror = GetSelectedText(_defaultBackMirror),
            DefaultDeckLayout = GetSelectedText(_defaultDeckLayout),
            DefaultGridColumns = (int)_defaultGridColumns.Value,
            DefaultSpacing = (int)_defaultSpacing.Value,
            DefaultPrintCompensationPercent = _defaultPrintCompensation.Value
        });

        SetStatus(result.Message, !result.Success);
    }

    private void ResetSettingsDefaults()
    {
        var result = CardToolService.ResetConfig();
        LoadConfigIntoFields();
        SetStatus(result.Message, !result.Success);
    }

    private void ResetSavedContent()
    {
        var result = CardToolService.ResetSavedContent();
        SetStatus(result.Message, !result.Success);
    }
}
