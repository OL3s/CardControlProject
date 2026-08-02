using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using CardGeneration.App;
using CardGeneration.Resources;
using Godot;

namespace CardGeneration.Ui;

public partial class ExportCenterScreen : CardToolScreen
{
    private IReadOnlyList<CardDeckResource> _decks = Array.Empty<CardDeckResource>();
    private IReadOnlyList<CardResource> _cards = Array.Empty<CardResource>();
    private OptionButton _targetType = null!;
    private Label _cardLabel = null!;
    private OptionButton _card = null!;
    private Label _deckLabel = null!;
    private OptionButton _deck = null!;
    private Label _exportTypeLabel = null!;
    private OptionButton _exportType = null!;
    private OptionButton _layout = null!;
    private OptionButton _paper = null!;
    private OptionButton _dpi = null!;
    private SpinBox _columns = null!;
    private SpinBox _spacing = null!;
    private LineEdit _outputPath = null!;
    private Button _exportButton = null!;
    private Button _reloadButton = null!;
    private ProgressBar _progressBar = null!;
    private Label _progressLabel = null!;
    private VBoxContainer _deckImageOptions = null!;
    private VBoxContainer _gridColumnRow = null!;
    private VBoxContainer _spacingRow = null!;
    private VBoxContainer _printSheetOptions = null!;
    private FileDialog _outputPathDialog = null!;
    private bool _startExportAfterPathSelection;

    public override void _Ready()
    {
        BuildUi();
    }

    private void BuildUi()
    {
        _decks = CardToolService.LoadAllDecks();
        _cards = CardToolService.LoadAllCards();
        var config = CardToolService.LoadConfig();
        var content = BuildScreen("Export", "Export a saved card or deck. Values start from Settings defaults, but can be changed here for this export only.");

        if (_decks.Count == 0 && _cards.Count == 0)
        {
            content.AddChild(new Label
            {
                Text = "No saved cards or decks found. Create content before exporting.",
                AutowrapMode = TextServer.AutowrapMode.WordSmart
            });
            return;
        }

        _targetType = AddOptionButton(content, "Export Target", ["Deck", "Card"]);
        if (_decks.Count == 0 && _cards.Count > 0)
        {
            SelectOption(_targetType, "Card");
        }

        _targetType.ItemSelected += _ => RefreshVisibleOptions();
        _deck = AddDeckOption(content, config.DefaultDeckId);
        _card = AddCardOption(content, config.DefaultCardId);
        AddOutputPathPicker(content, config.DefaultOutputPath);
        _exportTypeLabel = new Label { Text = "Export Type" };
        content.AddChild(_exportTypeLabel);
        _exportType = new OptionButton { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        _exportType.AddItem("Deck Images");
        _exportType.AddItem("Print Sheet");
        content.AddChild(_exportType);
        _exportType.ItemSelected += _ => RefreshVisibleOptions();

        _deckImageOptions = AddOptionGroup(content, "Deck Image Options");
        _layout = AddOptionButton(_deckImageOptions, "Deck Image Layout", ["individual", "grid", "strip"]);
        SelectOption(_layout, config.DefaultDeckLayout);
        _layout.ItemSelected += _ => RefreshVisibleOptions();
        _gridColumnRow = AddOptionGroup(_deckImageOptions);
        _columns = AddSpinBox(_gridColumnRow, "Grid Columns", 0, 24, 1, config.DefaultGridColumns);
        _spacingRow = AddOptionGroup(_deckImageOptions);
        _spacing = AddSpinBox(_spacingRow, "Spacing", 0, 256, 1, config.DefaultSpacing);

        _printSheetOptions = AddOptionGroup(content, "Print Sheet Options");
        _paper = AddOptionButton(_printSheetOptions, "Print Paper", ["a4", "a3"]);
        SelectOption(_paper, config.DefaultPaper);
        _dpi = AddOptionButton(_printSheetOptions, "Print DPI", ["150", "300", "600", "1200"]);
        SelectOption(_dpi, config.DefaultDpi.ToString());

        var buttons = new HBoxContainer();
        buttons.AddThemeConstantOverride("separation", 8);
        content.AddChild(buttons);
        _exportButton = AddIconButton(buttons, ExportIconPath, "Export", ExportSelected);
        _reloadButton = AddIconButton(buttons, RefreshIconPath, "Reload", BuildUi);

        _progressBar = new ProgressBar
        {
            Visible = false,
            MinValue = 0,
            MaxValue = 1,
            Value = 0,
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        content.AddChild(_progressBar);

        _progressLabel = new Label
        {
            Visible = false,
            Text = string.Empty,
            AutowrapMode = TextServer.AutowrapMode.WordSmart
        };
        content.AddChild(_progressLabel);

        RefreshVisibleOptions();
        SetStatus($"Ready to export {_cards.Count} card(s) and {_decks.Count} deck(s).");
    }

    private void AddOutputPathPicker(VBoxContainer content, string defaultOutputPath)
    {
        content.AddChild(new Label { Text = "Output Path" });

        var row = new HBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        row.AddThemeConstantOverride("separation", 8);
        content.AddChild(row);

        _outputPath = new LineEdit
        {
            Text = defaultOutputPath,
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        row.AddChild(_outputPath);

        AddIconButton(row, BrowseIconPath, "Browse", () => OpenOutputPathDialog(false));

        _outputPathDialog = new FileDialog
        {
            Access = FileDialog.AccessEnum.Filesystem,
            FileMode = FileDialog.FileModeEnum.OpenDir,
            Title = "Choose Export Folder",
            Filters = ["*.png ; PNG"]
        };
        _outputPathDialog.DirSelected += OnOutputDirectorySelected;
        _outputPathDialog.FileSelected += OnOutputFileSelected;
        _outputPathDialog.Canceled += OnOutputPathDialogCanceled;
        AddChild(_outputPathDialog);
    }

    private void OpenOutputPathDialog(bool startExportAfterSelection)
    {
        _startExportAfterPathSelection = startExportAfterSelection;
        var mode = GetOutputMode();
        _outputPathDialog.FileMode = mode == ExportOutputMode.SaveFile
            ? FileDialog.FileModeEnum.SaveFile
            : FileDialog.FileModeEnum.OpenDir;
        _outputPathDialog.Title = mode == ExportOutputMode.SaveFile ? "Choose Export File" : "Choose Export Folder";

        var outputPath = string.IsNullOrWhiteSpace(_outputPath.Text) ? "output" : _outputPath.Text;
        var globalOutputPath = ProjectPaths.ToGlobalPath(outputPath);

        if (mode == ExportOutputMode.SaveFile)
        {
            var currentDirectory = Path.GetDirectoryName(globalOutputPath);
            if (string.IsNullOrWhiteSpace(currentDirectory))
            {
                currentDirectory = ProjectPaths.ToGlobalPath("output");
            }

            Directory.CreateDirectory(currentDirectory);
            _outputPathDialog.CurrentDir = currentDirectory;
            _outputPathDialog.CurrentFile = Path.GetExtension(globalOutputPath).Equals(".png", StringComparison.OrdinalIgnoreCase)
                ? Path.GetFileName(globalOutputPath)
                : GetDefaultOutputFileName();
        }
        else
        {
            Directory.CreateDirectory(globalOutputPath);
            _outputPathDialog.CurrentDir = globalOutputPath;
            _outputPathDialog.CurrentFile = string.Empty;
        }

        _outputPathDialog.PopupCenteredRatio(0.72f);
    }

    private void OnOutputDirectorySelected(string directory)
    {
        _outputPath.Text = directory;
        StartExportAfterPathSelection();
    }

    private void OnOutputFileSelected(string file)
    {
        _outputPath.Text = file;
        StartExportAfterPathSelection();
    }

    private void OnOutputPathDialogCanceled()
    {
        if (!_startExportAfterPathSelection)
        {
            return;
        }

        _startExportAfterPathSelection = false;
        ResetProgress();
        SetStatus("Export cancelled.");
    }

    private static VBoxContainer AddOptionGroup(VBoxContainer parent, string? title = null)
    {
        var group = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        group.AddThemeConstantOverride("separation", 8);

        if (!string.IsNullOrWhiteSpace(title))
        {
            group.AddChild(new Label { Text = title });
        }

        parent.AddChild(group);
        return group;
    }

    private void RefreshVisibleOptions()
    {
        var isCardExport = GetSelectedText(_targetType) == "Card";
        _cardLabel.Visible = isCardExport;
        _card.Visible = isCardExport;
        _deckLabel.Visible = !isCardExport;
        _deck.Visible = !isCardExport;
        _exportTypeLabel.Visible = !isCardExport;
        _exportType.Visible = !isCardExport;
        _deckImageOptions.Visible = !isCardExport;
        _printSheetOptions.Visible = false;

        if (isCardExport)
        {
            UpdateOutputTooltip();
            return;
        }

        var isPrintSheet = GetSelectedText(_exportType) == "Print Sheet";
        _deckImageOptions.Visible = !isPrintSheet;
        _printSheetOptions.Visible = isPrintSheet;

        if (isPrintSheet)
        {
            UpdateOutputTooltip();
            return;
        }

        var layout = GetSelectedText(_layout);
        _gridColumnRow.Visible = layout == "grid";
        _spacingRow.Visible = layout is "grid" or "strip";
        UpdateOutputTooltip();
    }

    private OptionButton AddDeckOption(VBoxContainer content, string defaultDeckId)
    {
        _deckLabel = new Label { Text = "Deck" };
        content.AddChild(_deckLabel);
        var option = new OptionButton
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };

        foreach (var deck in _decks)
        {
            option.AddItem(deck.Id);
        }

        var selectedIndex = 0;
        for (var index = 0; index < _decks.Count; index++)
        {
            if (_decks[index].Id == defaultDeckId)
            {
                selectedIndex = index;
                break;
            }
        }

        option.Select(selectedIndex);
        content.AddChild(option);
        return option;
    }

    private OptionButton AddCardOption(VBoxContainer content, string defaultCardId)
    {
        _cardLabel = new Label { Text = "Card" };
        content.AddChild(_cardLabel);
        var option = new OptionButton
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };

        foreach (var card in _cards)
        {
            option.AddItem(card.Id);
        }

        var selectedIndex = 0;
        for (var index = 0; index < _cards.Count; index++)
        {
            if (_cards[index].Id == defaultCardId)
            {
                selectedIndex = index;
                break;
            }
        }

        option.Select(selectedIndex);
        content.AddChild(option);
        return option;
    }

    private void ExportSelected()
    {
        ResetProgress();
        SetStatus("Choose where to save the export.");
        OpenOutputPathDialog(true);
    }

    private void StartExportAfterPathSelection()
    {
        if (!_startExportAfterPathSelection)
        {
            return;
        }

        _startExportAfterPathSelection = false;
        StartExport();
    }

    private async void StartExport()
    {
        ResetProgress();
        var exportOperation = CreateExportOperation();
        if (exportOperation is null)
        {
            return;
        }

        SetExportControlsDisabled(true);
        _progressBar.Visible = true;
        _progressLabel.Visible = true;
        SetStatus("Exporting. Large decks or high-DPI sheets can take a while...");

        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

        var result = await Task.Run(exportOperation);

        SetExportControlsDisabled(false);
        ApplyExportProgress(1, 1, result.Message);
        SetStatus(result.Message, !result.Success);
    }

    private Func<ToolResult>? CreateExportOperation()
    {
        var outputPath = _outputPath.Text;
        Action<ExportProgress> progress = ReportExportProgress;
        if (GetSelectedText(_targetType) == "Card")
        {
            if (_card.Selected < 0 || _card.Selected >= _cards.Count)
            {
                SetStatus("Select a card before exporting.", true);
                return null;
            }

            var selectedCard = _cards[_card.Selected];
            return () => CardToolService.RenderCard(selectedCard, ResolveSingleOutputPath(outputPath), progress);
        }

        if (_deck.Selected < 0 || _deck.Selected >= _decks.Count)
        {
            SetStatus("Select a deck before exporting.", true);
            return null;
        }

        var selectedDeck = _decks[_deck.Selected];
        if (GetSelectedText(_exportType) == "Print Sheet")
        {
            var paper = GetSelectedText(_paper);
            var dpi = int.Parse(GetSelectedText(_dpi));
            var sheetOutputPath = ResolveMultiOutputPath(outputPath, $"{selectedDeck.Id}_{paper}_{dpi}dpi_sheets");
            return () => CardToolService.ExportSheet(selectedDeck, sheetOutputPath, paper, dpi, progress);
        }

        var layout = GetSelectedText(_layout);
        var columns = (int)_columns.Value;
        var spacing = (int)_spacing.Value;
        var deckOutputPath = layout == "individual"
            ? ResolveMultiOutputPath(outputPath, $"{selectedDeck.Id}_individual")
            : ResolveSingleOutputPath(outputPath);
        return () => CardToolService.ExportDeck(selectedDeck, deckOutputPath, "png", layout, columns, spacing, progress);
    }

    private void ReportExportProgress(ExportProgress progress)
    {
        if (!IsInstanceValid(this))
        {
            return;
        }

        CallDeferred(nameof(ApplyExportProgress), progress.Current, progress.Total, progress.Message);
    }

    private void ApplyExportProgress(int current, int total, string message)
    {
        _progressBar.MaxValue = Math.Max(1, total);
        _progressBar.Value = Math.Clamp(current, 0, Math.Max(1, total));
        _progressLabel.Text = total > 0 ? $"{message} ({Math.Clamp(current, 0, total)} / {total})" : message;
    }

    private void ResetProgress()
    {
        _progressBar.Visible = false;
        _progressLabel.Visible = false;
        _progressBar.Value = 0;
        _progressLabel.Text = string.Empty;
    }

    private ExportOutputMode GetOutputMode()
    {
        if (GetSelectedText(_targetType) == "Card")
        {
            return ExportOutputMode.SaveFile;
        }

        if (GetSelectedText(_exportType) == "Print Sheet")
        {
            return ExportOutputMode.Folder;
        }

        return GetSelectedText(_layout) == "individual" ? ExportOutputMode.Folder : ExportOutputMode.SaveFile;
    }

    private string GetDefaultOutputFileName()
    {
        if (GetSelectedText(_targetType) == "Card" && _card.Selected >= 0 && _card.Selected < _cards.Count)
        {
            return $"{SanitizeFileName(_cards[_card.Selected].Id)}.png";
        }

        if (_deck.Selected >= 0 && _deck.Selected < _decks.Count)
        {
            return $"{SanitizeFileName(_decks[_deck.Selected].Id)}_{GetSelectedText(_layout)}.png";
        }

        return "export.png";
    }

    private string ResolveSingleOutputPath(string outputPath)
    {
        if (Path.GetExtension(outputPath).Equals(".png", StringComparison.OrdinalIgnoreCase))
        {
            return outputPath;
        }

        return Path.Combine(outputPath, GetDefaultOutputFileName());
    }

    private static string ResolveMultiOutputPath(string parentFolder, string subfolderName)
    {
        return Path.Combine(parentFolder, SanitizeFileName(subfolderName));
    }

    private void UpdateOutputTooltip()
    {
        _outputPath.TooltipText = GetOutputMode() == ExportOutputMode.SaveFile
            ? "Choose the exact PNG file to write."
            : "Choose a parent folder. Export creates a named subfolder there.";
    }

    private static string SanitizeFileName(string fileName)
    {
        foreach (var invalidChar in Path.GetInvalidFileNameChars())
        {
            fileName = fileName.Replace(invalidChar, '_');
        }

        return string.IsNullOrWhiteSpace(fileName) ? "export" : fileName;
    }

    private void SetExportControlsDisabled(bool disabled)
    {
        _exportButton.Disabled = disabled;
        _reloadButton.Disabled = disabled;
        _targetType.Disabled = disabled;
        _card.Disabled = disabled;
        _deck.Disabled = disabled;
        _exportType.Disabled = disabled;
        _layout.Disabled = disabled;
        _paper.Disabled = disabled;
        _dpi.Disabled = disabled;
        _columns.Editable = !disabled;
        _spacing.Editable = !disabled;
        _outputPath.Editable = !disabled;
    }

    private enum ExportOutputMode
    {
        SaveFile,
        Folder
    }
}
