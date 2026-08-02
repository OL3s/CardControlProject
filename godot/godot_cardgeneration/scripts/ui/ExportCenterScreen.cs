using System;
using System.Collections.Generic;
using System.IO;
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
    private VBoxContainer _deckImageOptions = null!;
    private VBoxContainer _gridColumnRow = null!;
    private VBoxContainer _spacingRow = null!;
    private VBoxContainer _printSheetOptions = null!;
    private FileDialog _outputPathDialog = null!;

    public override void _Ready()
    {
        BuildUi();
    }

    private void BuildUi()
    {
        _decks = CardToolService.LoadAllDecks();
        _cards = CardToolService.LoadAllCards();
        var config = CardToolService.LoadConfig();
        var content = BuildScreen("Export", "Export a saved card or deck. Decks can be exported as images or numbered print sheets.");

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
        AddButton(buttons, "Export", ExportSelected, 110);
        AddButton(buttons, "Reload", BuildUi, 100);

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

        AddButton(row, "Browse", OpenOutputPathDialog, 100);

        _outputPathDialog = new FileDialog
        {
            Access = FileDialog.AccessEnum.Filesystem,
            FileMode = FileDialog.FileModeEnum.OpenDir,
            Title = "Choose Export Folder"
        };
        _outputPathDialog.DirSelected += OnOutputDirectorySelected;
        AddChild(_outputPathDialog);
    }

    private void OpenOutputPathDialog()
    {
        var outputDirectory = string.IsNullOrWhiteSpace(_outputPath.Text) ? "output" : _outputPath.Text;
        var globalOutputDirectory = CardGeneration.App.ProjectPaths.ToGlobalPath(outputDirectory);
        Directory.CreateDirectory(globalOutputDirectory);

        _outputPathDialog.CurrentDir = globalOutputDirectory;
        _outputPathDialog.PopupCenteredRatio(0.72f);
    }

    private void OnOutputDirectorySelected(string directory)
    {
        _outputPath.Text = directory;
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
            return;
        }

        var isPrintSheet = GetSelectedText(_exportType) == "Print Sheet";
        _deckImageOptions.Visible = !isPrintSheet;
        _printSheetOptions.Visible = isPrintSheet;

        if (isPrintSheet)
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
        if (GetSelectedText(_targetType) == "Card")
        {
            ExportSelectedCard();
            return;
        }

        ExportSelectedDeck();
    }

    private void ExportSelectedCard()
    {
        if (_card.Selected < 0 || _card.Selected >= _cards.Count)
        {
            SetStatus("Select a card before exporting.", true);
            return;
        }

        var result = CardToolService.RenderCard(_cards[_card.Selected], _outputPath.Text);
        SetStatus(result.Message, !result.Success);
    }

    private void ExportSelectedDeck()
    {
        if (_deck.Selected < 0 || _deck.Selected >= _decks.Count)
        {
            SetStatus("Select a deck before exporting.", true);
            return;
        }

        var selectedDeck = _decks[_deck.Selected];
        var outputPath = _outputPath.Text;
        var result = GetSelectedText(_exportType) == "Print Sheet"
            ? CardToolService.ExportSheet(selectedDeck, outputPath, GetSelectedText(_paper), int.Parse(GetSelectedText(_dpi)))
            : CardToolService.ExportDeck(selectedDeck, outputPath, "png", GetSelectedText(_layout), (int)_columns.Value, (int)_spacing.Value);
        SetStatus(result.Message, !result.Success);
    }
}
