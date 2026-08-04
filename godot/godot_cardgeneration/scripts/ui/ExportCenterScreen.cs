using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using CardGeneration.App;
using CardGeneration.Resources;
using CardGeneration.Services;
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
    private OptionButton _printMode = null!;
    private OptionButton _backMirror = null!;
    private CheckBox _easyPrintBacks = null!;
    private CheckBox _measurementGuide = null!;
    private SpinBox _columns = null!;
    private SpinBox _spacing = null!;
    private HSlider _printCompensationSlider = null!;
    private SpinBox _printCompensationValue = null!;
    private Button _exportButton = null!;
    private Button _previewButton = null!;
    private Button _reloadButton = null!;
    private Button _calibrationExportButton = null!;
    private Button _calibrationPreviewButton = null!;
    private ProgressBar _progressBar = null!;
    private Label _progressLabel = null!;
    private VBoxContainer _imageBackOptions = null!;
    private OptionButton _imageBackMode = null!;
    private VBoxContainer _deckImageOptions = null!;
    private VBoxContainer _gridColumnRow = null!;
    private VBoxContainer _spacingRow = null!;
    private VBoxContainer _printOptions = null!;
    private FileDialog _outputPathDialog = null!;
    private bool _startExportAfterPathSelection;
    private bool _startCalibrationExportAfterPathSelection;
    private string _selectedOutputPath = string.Empty;

    public override void _Ready()
    {
        BuildUi();
    }

    private void BuildUi()
    {
        _decks = CardToolService.LoadAllDecks();
        _cards = CardToolService.LoadAllCards();
        var config = CardToolService.LoadConfig();
        var content = BuildScreen("Export", "Export a saved deck. Card rendering uses the deck's icons, power glyph and backs.");

        if (_decks.Count == 0)
        {
            content.AddChild(new Label
            {
                Text = "No saved decks found. Deck export is unavailable, but print calibration can still be previewed and exported.",
                AutowrapMode = TextServer.AutowrapMode.WordSmart
            });
        }

        _targetType = AddOptionButton(content, "Export Target", ["Deck"]);

        _targetType.ItemSelected += index => RunGuiAction("Change export target", RefreshVisibleOptions, $"index={index}; target={GetSelectedText(_targetType)}");
        _deck = AddDeckOption(content, config.DefaultDeckId);
        _card = AddCardOption(content, config.DefaultCardId);
        AddOutputPathDialog(config.DefaultOutputPath);
        _exportTypeLabel = new Label { Text = "Export Type" };
        content.AddChild(_exportTypeLabel);
        _exportType = new OptionButton { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        _exportType.AddItem("Images");
        _exportType.AddItem("Print");
        content.AddChild(_exportType);
        _exportType.ItemSelected += index => RunGuiAction("Change export type", RefreshVisibleOptions, $"index={index}; type={GetSelectedText(_exportType)}");

        _imageBackOptions = AddOptionGroup(content, "Image Back Options");
        _imageBackMode = AddOptionButton(_imageBackOptions, "Back Images", ["No backs", "Used card types", "All card types"]);
        SelectOption(_imageBackMode, "All card types");
        _imageBackMode.ItemSelected += index => RunGuiAction("Change image back mode", RefreshVisibleOptions, $"index={index}; mode={GetSelectedText(_imageBackMode)}");

        _deckImageOptions = AddOptionGroup(content, "Deck Image Options");
        _layout = AddOptionButton(_deckImageOptions, "Image Layout", ["individual", "grid", "strip"]);
        SelectOption(_layout, config.DefaultDeckLayout);
        _layout.ItemSelected += index => RunGuiAction("Change deck export layout", RefreshVisibleOptions, $"index={index}; layout={GetSelectedText(_layout)}");
        _gridColumnRow = AddOptionGroup(_deckImageOptions);
        _columns = AddSpinBox(_gridColumnRow, "Grid Columns", 0, 24, 1, config.DefaultGridColumns);
        _spacingRow = AddOptionGroup(_deckImageOptions);
        _spacing = AddSpinBox(_spacingRow, "Spacing", 0, 256, 1, config.DefaultSpacing);

        _printOptions = AddOptionGroup(content, "Print Options");
        _paper = AddOptionButton(_printOptions, "Paper", ["A4", "A3"]);
        SelectOption(_paper, config.DefaultPaper);
        _dpi = AddOptionButton(_printOptions, "Print DPI", ["150", "300", "600", "1200"]);
        SelectOption(_dpi, config.DefaultDpi.ToString());
        _printMode = AddOptionButton(_printOptions, "Print Mode", ["home", "production"]);
        SelectOption(_printMode, config.DefaultPrintMode);
        _printMode.TooltipText = "Home leaves the 3 mm work margin white for visible manual cutting. Production fills it as full bleed.";
        AddPrintCompensationControl(_printOptions, config.DefaultPrintCompensationPercent);
        _backMirror = AddOptionButton(_printOptions, "Back Mirror", ["none", "width", "height", "both"]);
        SelectOption(_backMirror, config.DefaultBackMirror);
        _easyPrintBacks = new CheckBox
        {
            Text = "Easy backs: group fronts by card type and fill every back sheet",
            ButtonPressed = false,
            TooltipText = "Uses more paper and ink, but every paired back sheet is completely filled with one card type's back so front/back slot alignment is unnecessary."
        };
        _easyPrintBacks.Toggled += enabled => RunGuiAction("Toggle easy print backs", UpdateBackMirrorAvailability, $"enabled={enabled}");
        _printOptions.AddChild(_easyPrintBacks);
        _measurementGuide = new CheckBox
        {
            Text = "10 cm measurement guide",
            ButtonPressed = false,
            TooltipText = "Draws a 10 cm ruler line with 1 cm ticks on print sheets so printed scale can be checked."
        };
        _printOptions.AddChild(_measurementGuide);

        var calibrationButtons = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Begin };
        calibrationButtons.AddThemeConstantOverride("separation", 8);
        _printOptions.AddChild(calibrationButtons);
        _calibrationPreviewButton = AddIconButton(calibrationButtons, PreviewIconPath, "Preview two-page print calibration test", ShowCalibrationPreview);
        _calibrationExportButton = AddIconButton(calibrationButtons, ExportIconPath, "Export two-page print calibration test", OpenCalibrationOutputDialog);

        var buttons = new HBoxContainer();
        buttons.AddThemeConstantOverride("separation", 8);
        content.AddChild(buttons);
        _exportButton = AddIconButton(buttons, ExportIconPath, "Export", ExportSelected);
        _previewButton = AddIconButton(buttons, PreviewIconPath, "Show preview", ShowPreview);
        _reloadButton = AddIconButton(buttons, RefreshIconPath, "Reload", RefreshDefaultsAndBuildUi);

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
        SetStatus($"Ready to export {_decks.Count} deck(s).");
    }

    private void RefreshDefaultsAndBuildUi()
    {
        var defaultResult = CardToolService.EnsureDefaultResources();
        BuildUi();
        SetStatus(defaultResult.Message, !defaultResult.Success);
    }

    private void AddOutputPathDialog(string defaultOutputPath)
    {
        _selectedOutputPath = string.IsNullOrWhiteSpace(defaultOutputPath) ? "output" : defaultOutputPath;

        _outputPathDialog = new FileDialog
        {
            Access = FileDialog.AccessEnum.Filesystem,
            FileMode = FileDialog.FileModeEnum.OpenDir,
            Title = "Choose Export Folder",
            Filters = ["*.png ; PNG"]
        };
        _outputPathDialog.DirSelected += path => RunGuiAction("Selected export output directory", () => OnOutputDirectorySelected(path), $"path={path}");
        _outputPathDialog.FileSelected += path => RunGuiAction("Selected export output file", () => OnOutputFileSelected(path), $"path={path}");
        _outputPathDialog.Canceled += LogGuiAction("Canceled export output path dialog", OnOutputPathDialogCanceled);
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

        var globalOutputPath = GetDialogStartPath(mode);

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
        _selectedOutputPath = directory;
        if (_startCalibrationExportAfterPathSelection)
        {
            _startCalibrationExportAfterPathSelection = false;
            ExportCalibrationTest();
            return;
        }

        StartExportAfterPathSelection();
    }

    private void OnOutputFileSelected(string file)
    {
        _selectedOutputPath = file;
        StartExportAfterPathSelection();
    }

    private void OnOutputPathDialogCanceled()
    {
        if (_startCalibrationExportAfterPathSelection)
        {
            _startCalibrationExportAfterPathSelection = false;
            ResetProgress();
            SetStatus("Print calibration export cancelled.");
            return;
        }

        if (!_startExportAfterPathSelection)
        {
            return;
        }

        _startExportAfterPathSelection = false;
        ResetProgress();
        SetStatus("Export cancelled.");
    }

    private void OpenCalibrationOutputDialog()
    {
        _startExportAfterPathSelection = false;
        _startCalibrationExportAfterPathSelection = true;
        _outputPathDialog.FileMode = FileDialog.FileModeEnum.OpenDir;
        _outputPathDialog.Title = "Choose Print Calibration Export Folder";
        var globalOutputPath = GetDialogStartPath(ExportOutputMode.Folder);
        Directory.CreateDirectory(globalOutputPath);
        _outputPathDialog.CurrentDir = globalOutputPath;
        _outputPathDialog.CurrentFile = string.Empty;
        _outputPathDialog.PopupCenteredRatio(0.72f);
    }

    private async void ExportCalibrationTest()
    {
        ResetProgress();
        SetExportControlsDisabled(true);
        SetStatus("Exporting two-page print calibration test...");
        try
        {
            var paper = GetSelectedText(_paper).ToLowerInvariant();
            var compensation = _printCompensationValue.Value;
            var outputPath = ResolveMultiOutputPath(_selectedOutputPath, $"print_test_{paper}_{compensation:0.#}pct");
            var result = await Task.Run(() => CardToolService.ExportPrintCalibration(outputPath, paper, compensation));
            if (!CanUpdateUi())
            {
                return;
            }

            SetStatus(result.Message, !result.Success);
        }
        catch (Exception exception)
        {
            AppLogger.GuiError("Print calibration export failed.", exception);
            if (CanUpdateUi())
            {
                SetStatus($"Print calibration export failed: {exception.Message}", true);
            }
        }
        finally
        {
            if (CanUpdateUi())
            {
                SetExportControlsDisabled(false);
            }
        }
    }

    private async void ShowCalibrationPreview()
    {
        ResetProgress();
        SetExportControlsDisabled(true);
        SetStatus("Rendering print calibration preview...");
        try
        {
            var paper = GetSelectedText(_paper).ToLowerInvariant();
            var compensation = _printCompensationValue.Value;
            var result = await Task.Run(() =>
            {
                var previews = CardToolService.RenderPrintCalibrationPreviews(paper, compensation, out var error);
                return (Previews: previews, Error: error);
            });
            if (!CanUpdateUi())
            {
                if (result.Previews is not null)
                {
                    foreach (var preview in result.Previews)
                    {
                        preview.Dispose();
                    }
                }

                return;
            }

            if (result.Previews is null)
            {
                SetStatus(result.Error, true);
                return;
            }

            ShowImagePreviewPopup(result.Previews, "Print calibration preview", sheetSized: true);
            SetStatus($"Showing two-page {paper.ToUpperInvariant()} print calibration preview at {compensation:0.#}%.");
        }
        catch (Exception exception)
        {
            AppLogger.GuiError("Print calibration preview failed.", exception);
            if (CanUpdateUi())
            {
                SetStatus($"Print calibration preview failed: {exception.Message}", true);
            }
        }
        finally
        {
            if (CanUpdateUi())
            {
                SetExportControlsDisabled(false);
            }
        }
    }

    private string GetDialogStartPath(ExportOutputMode mode)
    {
        var outputPath = string.IsNullOrWhiteSpace(_selectedOutputPath) ? "output" : _selectedOutputPath;
        var globalOutputPath = ProjectPaths.ToGlobalPath(outputPath);

        if (mode == ExportOutputMode.Folder && Path.GetExtension(globalOutputPath).Equals(".png", StringComparison.OrdinalIgnoreCase))
        {
            return Path.GetDirectoryName(globalOutputPath) ?? ProjectPaths.ToGlobalPath("output");
        }

        return globalOutputPath;
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
        _cardLabel.Visible = false;
        _card.Visible = false;
        _deckLabel.Visible = true;
        _deck.Visible = true;
        _exportTypeLabel.Visible = true;
        _exportType.Visible = true;

        var isPrint = GetSelectedText(_exportType) == "Print";
        _imageBackOptions.Visible = !isPrint;
        _deckImageOptions.Visible = !isPrint;
        _printOptions.Visible = isPrint;
        _previewButton.Visible = true;

        if (isPrint)
        {
            return;
        }

        var layout = GetSelectedText(_layout);
        _gridColumnRow.Visible = layout == "grid";
        _spacingRow.Visible = layout is "grid" or "strip";
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

        if (option.ItemCount == 0)
        {
            option.AddItem("No decks available");
            option.Disabled = true;
            content.AddChild(option);
            return option;
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

        if (option.ItemCount == 0)
        {
            option.AddItem("No cards available");
            option.Disabled = true;
            content.AddChild(option);
            return option;
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

        try
        {
            var result = await Task.Run(exportOperation);
            ApplyExportProgress(1, 1, result.Message);
            SetStatus(result.Message, !result.Success);
        }
        catch (Exception exception)
        {
            AppLogger.GuiError("Export failed with an unexpected exception.", exception);
            SetStatus($"Export failed: {exception.Message}", true);
        }
        finally
        {
            SetExportControlsDisabled(false);
        }
    }

    private Func<ToolResult>? CreateExportOperation()
    {
        var outputPath = _selectedOutputPath;
        Action<ExportProgress> progress = ReportExportProgress;
        CardDeckResource? selectedDeck = null;
        if (_deck.Selected < 0 || _deck.Selected >= _decks.Count)
        {
            SetStatus("Select a deck before exporting.", true);
            return null;
        }
        selectedDeck = _decks[_deck.Selected];

        var exportType = GetSelectedText(_exportType);
        if (exportType == "Print")
        {
            var paper = GetSelectedText(_paper).ToLowerInvariant();
            var dpi = int.Parse(GetSelectedText(_dpi));
            var backMirror = GetSelectedText(_backMirror);
            var includeMeasurementGuide = _measurementGuide.ButtonPressed;
            var easyPrintBacks = _easyPrintBacks.ButtonPressed;
            var printMode = GetSelectedText(_printMode);
            var modeSuffix = easyPrintBacks ? "_easy_backs" : string.Empty;
            var sheetOutputPath = ResolveMultiOutputPath(outputPath, $"{selectedDeck.Id}_{paper}_{dpi}dpi_{printMode}{modeSuffix}_sheets");
            var printCompensationPercent = _printCompensationValue.Value;
            return () => CardToolService.ExportSheet(selectedDeck, sheetOutputPath, paper, dpi, backMirror, includeMeasurementGuide, progress, easyPrintBacks, printCompensationPercent, printMode);
        }

        var layout = GetSelectedText(_layout);
        var columns = (int)_columns.Value;
        var spacing = (int)_spacing.Value;
        var deckOutputPath = layout == "individual"
            ? ResolveMultiOutputPath(outputPath, $"{selectedDeck.Id}_individual")
            : ResolveSingleOutputPath(outputPath);
        var backMode = GetImageBackMode();
        return () => CardToolService.ExportDeck(selectedDeck, deckOutputPath, "png", layout, columns, spacing, progress, backMode);
    }

    private async void ShowPreview()
    {
        ResetProgress();
        SetExportControlsDisabled(true);
        _progressBar.Visible = true;
        _progressLabel.Visible = true;
        SetStatus("Creating preview...");

        try
        {
            await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

            if (GetSelectedText(_exportType) == "Print")
            {
                await ShowPrintPreview();
            }
            else
            {
                await ShowImagePreview();
            }
        }
        catch (Exception exception)
        {
            AppLogger.GuiError("Preview generation failed with an unexpected exception.", exception);
            SetStatus($"Preview generation failed: {exception.Message}", true);
        }
        finally
        {
            SetExportControlsDisabled(false);
        }
    }

    private async Task ShowImagePreview()
    {
        IReadOnlyList<ImagePreviewItem>? previews;

        {
            if (_deck.Selected < 0 || _deck.Selected >= _decks.Count)
            {
                SetStatus("Select a deck before previewing image output.", true);
                return;
            }

            var deck = _decks[_deck.Selected];
            var layout = GetSelectedText(_layout);
            var columns = (int)_columns.Value;
            var spacing = (int)_spacing.Value;
            var backMode = GetImageBackMode();
            var result = await Task.Run(() =>
            {
                var items = CardToolService.RenderDeckImagePreviews(deck, layout, columns, spacing, out var error, ReportExportProgress, backMode);
                return (Items: items, Error: error);
            });
            previews = result.Items;
            if (previews is null)
            {
                SetStatus(result.Error, true);
                return;
            }
        }

        ShowImagePreviewPopup(previews);
        SetStatus($"Showing image export preview with {previews.Count} image(s).");
    }

    private void ShowImagePreviewPopup(IReadOnlyList<ImagePreviewItem> previews, string title = "Image export preview", bool sheetSized = false)
    {
        var popup = new PopupPanel { Title = title };
        AddChild(popup);

        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_left", 14);
        margin.AddThemeConstantOverride("margin_right", 14);
        margin.AddThemeConstantOverride("margin_top", 14);
        margin.AddThemeConstantOverride("margin_bottom", 14);
        popup.AddChild(margin);

        var scroll = new ScrollContainer
        {
            CustomMinimumSize = new Vector2(980, 680),
            HorizontalScrollMode = sheetSized ? ScrollContainer.ScrollMode.Disabled : ScrollContainer.ScrollMode.Auto,
            VerticalScrollMode = ScrollContainer.ScrollMode.Auto
        };
        margin.AddChild(scroll);

        Container previewList = sheetSized
            ? new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center, SizeFlagsHorizontal = SizeFlags.ExpandFill }
            : previews.Count > 1
                ? new GridContainer { Columns = 3, SizeFlagsHorizontal = SizeFlags.ExpandFill }
            : new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        previewList.AddThemeConstantOverride("separation", sheetSized ? 16 : 8);
        scroll.AddChild(previewList);

        try
        {
            foreach (var preview in previews)
            {
                var column = new VBoxContainer();
                column.AddThemeConstantOverride("separation", 8);
                previewList.AddChild(column);
                column.AddChild(new Label { Text = preview.Label, HorizontalAlignment = HorizontalAlignment.Center });

                var texture = ImageTexture.CreateFromImage(preview.Image);
                var imageSize = preview.Image.GetSize();
                var displayWidth = sheetSized ? 460 : previews.Count > 1 ? 280 : Math.Min(900, Math.Max(300, imageSize.X));
                var displayHeight = Math.Max(1, (int)Math.Round(displayWidth * imageSize.Y / (double)imageSize.X));
                column.AddChild(new TextureRect
                {
                    Texture = texture,
                    CustomMinimumSize = new Vector2(displayWidth, displayHeight),
                    ExpandMode = TextureRect.ExpandModeEnum.FitWidthProportional,
                    StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered
                });
            }
        }
        finally
        {
            foreach (var preview in previews)
            {
                preview.Dispose();
            }
        }

        popup.PopupCentered(new Vector2I(1060, 760));
        popup.PopupHide += popup.QueueFree;
    }

    private async Task ShowPrintPreview()
    {
        var previewDeck = GetSelectedPreviewDeck();
        if (previewDeck is null)
        {
            return;
        }

        var paper = GetSelectedText(_paper).ToLowerInvariant();
        var dpi = int.Parse(GetSelectedText(_dpi));
        var backMirror = GetSelectedText(_backMirror);
        var includeMeasurementGuide = _measurementGuide.ButtonPressed;
        var easyPrintBacks = _easyPrintBacks.ButtonPressed;
        var printMode = GetSelectedText(_printMode);
        var printCompensationPercent = _printCompensationValue.Value;
        var result = await Task.Run(() =>
        {
            var previewPages = CardToolService.RenderSheetPreviews(previewDeck, paper, dpi, backMirror, includeMeasurementGuide, easyPrintBacks, out var error, ReportExportProgress, printCompensationPercent, printMode);
            return (Pages: previewPages, Error: error);
        });
        var pages = result.Pages;
        if (pages is null)
        {
            SetStatus(result.Error, true);
            return;
        }

        ShowPrintPreviewPopup(previewDeck.Id, paper, dpi, pages);
        SetStatus($"Showing all {pages.Count} {paper.ToUpperInvariant()} print page pair(s) for '{previewDeck.Id}'.");
    }

    private CardDeckResource? GetSelectedPreviewDeck()
    {
        if (_deck.Selected < 0 || _deck.Selected >= _decks.Count)
        {
            SetStatus("Select a deck before previewing print output.", true);
            return null;
        }

        return _decks[_deck.Selected];
    }

    private void UpdateBackMirrorAvailability()
    {
        _backMirror.Disabled = _easyPrintBacks.ButtonPressed;
    }

    private ImageBackMode GetImageBackMode()
    {
        return GetSelectedText(_imageBackMode) switch
        {
            "Used card types" => ImageBackMode.Used,
            "All card types" => ImageBackMode.All,
            _ => ImageBackMode.None
        };
    }

    private void ShowPrintPreviewPopup(string targetId, string paper, int dpi, IReadOnlyList<SheetPreviewPage> pages)
    {
        var popup = new PopupPanel
        {
            Title = $"Print preview: {targetId} ({paper.ToUpperInvariant()}, {dpi} DPI layout)"
        };
        AddChild(popup);

        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_left", 14);
        margin.AddThemeConstantOverride("margin_right", 14);
        margin.AddThemeConstantOverride("margin_top", 14);
        margin.AddThemeConstantOverride("margin_bottom", 14);
        popup.AddChild(margin);

        var scroll = new ScrollContainer
        {
            CustomMinimumSize = new Vector2(980, 680),
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
            VerticalScrollMode = ScrollContainer.ScrollMode.Auto
        };
        margin.AddChild(scroll);

        var pageList = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        pageList.AddThemeConstantOverride("separation", 20);
        scroll.AddChild(pageList);

        var nextPageToDispose = 0;
        try
        {
            for (var pageIndex = 0; pageIndex < pages.Count; pageIndex++)
            {
                var page = pages[pageIndex];
                var pageGroup = new VBoxContainer
                {
                    SizeFlagsHorizontal = SizeFlags.ExpandFill
                };
                pageGroup.AddThemeConstantOverride("separation", 8);
                pageList.AddChild(pageGroup);

                pageGroup.AddChild(new Label
                {
                    Text = $"Page {page.PageNumber} of {pages.Count}",
                    HorizontalAlignment = HorizontalAlignment.Center
                });

                var row = new HBoxContainer
                {
                    SizeFlagsHorizontal = SizeFlags.ExpandFill
                };
                row.AddThemeConstantOverride("separation", 16);
                pageGroup.AddChild(row);

                AddSheetPreview(row, $"Front page {page.PageNumber}", page.Front);
                AddSheetPreview(row, $"Back page {page.PageNumber}", page.Back);

                if (page.PageNumber < pages.Count)
                {
                    pageList.AddChild(new HSeparator());
                }

                page.Dispose();
                nextPageToDispose = pageIndex + 1;
            }
        }
        finally
        {
            for (var pageIndex = nextPageToDispose; pageIndex < pages.Count; pageIndex++)
            {
                pages[pageIndex].Dispose();
            }
        }

        popup.PopupCentered(new Vector2I(1060, 760));
        popup.PopupHide += popup.QueueFree;
    }

    private static void AddSheetPreview(Container parent, string title, Image image)
    {
        var column = new VBoxContainer();
        column.AddThemeConstantOverride("separation", 8);
        parent.AddChild(column);

        column.AddChild(new Label
        {
            Text = title,
            HorizontalAlignment = HorizontalAlignment.Center
        });

        var texture = ImageTexture.CreateFromImage(image);
        column.AddChild(new TextureRect
        {
            Texture = texture,
            CustomMinimumSize = new Vector2(460, 650),
            ExpandMode = TextureRect.ExpandModeEnum.FitWidthProportional,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered
        });
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
        if (GetSelectedText(_exportType) == "Print")
        {
            return ExportOutputMode.Folder;
        }

        return GetSelectedText(_layout) == "individual" ? ExportOutputMode.Folder : ExportOutputMode.SaveFile;
    }

    private string GetDefaultOutputFileName()
    {
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
        _previewButton.Disabled = disabled;
        _reloadButton.Disabled = disabled;
        _calibrationExportButton.Disabled = disabled;
        _calibrationPreviewButton.Disabled = disabled;
        _targetType.Disabled = disabled;
        _card.Disabled = disabled;
        _deck.Disabled = disabled;
        _exportType.Disabled = disabled;
        _imageBackMode.Disabled = disabled;
        _layout.Disabled = disabled;
        _paper.Disabled = disabled;
        _dpi.Disabled = disabled;
        _printMode.Disabled = disabled;
        _easyPrintBacks.Disabled = disabled;
        _backMirror.Disabled = disabled || _easyPrintBacks.ButtonPressed;
        _measurementGuide.Disabled = disabled;
        _printCompensationSlider.Editable = !disabled;
        _printCompensationValue.Editable = !disabled;
        _columns.Editable = !disabled;
        _spacing.Editable = !disabled;
    }

    private void AddPrintCompensationControl(VBoxContainer parent, double initialValue)
    {
        parent.AddChild(new Label
        {
            Text = "Print Compensation (%)",
            TooltipText = "Increase this when the printer shrinks the page. Card size, bleed, grid placement, and the 10 cm guide are scaled together."
        });
        var row = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        row.AddThemeConstantOverride("separation", 10);
        parent.AddChild(row);

        _printCompensationSlider = new HSlider
        {
            MinValue = PrintSheetLayout.MinCompensationPercent,
            MaxValue = PrintSheetLayout.MaxCompensationPercent,
            Step = 0.1,
            Value = initialValue,
            TickCount = 3,
            TicksOnBorders = true,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            TooltipText = "90% to 110%. Start at 100%, print a test, and adjust until the 10 cm line and 63 x 88 mm trim outline measure correctly."
        };
        row.AddChild(_printCompensationSlider);
        _printCompensationValue = new SpinBox
        {
            MinValue = PrintSheetLayout.MinCompensationPercent,
            MaxValue = PrintSheetLayout.MaxCompensationPercent,
            Step = 0.1,
            Value = initialValue,
            Suffix = "%",
            CustomMinimumSize = new Vector2(120, 0)
        };
        row.AddChild(_printCompensationValue);
        _printCompensationSlider.ValueChanged += value => _printCompensationValue.Value = value;
        _printCompensationValue.ValueChanged += value => _printCompensationSlider.Value = value;
    }

    private bool CanUpdateUi()
    {
        return GodotObject.IsInstanceValid(this) && IsInsideTree();
    }

    private enum ExportOutputMode
    {
        SaveFile,
        Folder
    }
}
