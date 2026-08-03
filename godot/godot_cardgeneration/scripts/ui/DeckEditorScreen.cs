using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CardGeneration.App;
using CardGeneration.Resources;
using CardGeneration.Resources.Enums;
using Godot;

namespace CardGeneration.Ui;

public partial class DeckEditorScreen : CardToolScreen
{
    public static readonly Vector2I CardThumbnailRenderSize = new(130, 182);

    private CardDeckResource _editingDeck = new();
    private readonly List<CardDeckEntryResource> _entries = [];
    private readonly HashSet<string> _selectedAvailableCardIds = [];
    private readonly HashSet<string> _selectedDeckCardIds = [];
    private IReadOnlyList<CardResource> _availableCards = Array.Empty<CardResource>();
    private string _originalResourcePath = string.Empty;
    private CardType _backImageTargetType = CardType.Monster;
    private ElementType _elementIconTargetType = ElementType.Neutral;
    private LineEdit _id = null!;
    private HBoxContainer _backPreviewRow = null!;
    private HBoxContainer _elementIconPreviewRow = null!;
    private HBoxContainer _powerIconPreviewRow = null!;
    private FileDialog _backImageDialog = null!;
    private FileDialog _elementIconDialog = null!;
    private FileDialog _powerIconDialog = null!;
    private FileDialog _saveAsDialog = null!;
    private FileDialog _exportDialog = null!;
    private VBoxContainer _entriesPanel = null!;
    private VBoxContainer _availableCardsPanel = null!;

    public void SetDeck(CardDeckResource? deck)
    {
        _originalResourcePath = deck?.ResourcePath ?? string.Empty;
        _editingDeck = deck is null ? new CardDeckResource() : CloneDeck(deck);
        _entries.Clear();
        _selectedAvailableCardIds.Clear();
        _selectedDeckCardIds.Clear();
        _entries.AddRange((_editingDeck.Entries ?? Array.Empty<CardDeckEntryResource>())
            .Where(entry => entry.Card is not null)
            .Select(entry => new CardDeckEntryResource
            {
                Card = entry.Card,
                Count = Math.Max(1, entry.Count)
            }));
    }

    public override void _Ready()
    {
        BuildUi();
    }

    private void BuildUi()
    {
        _availableCards = CardToolService.LoadAllCards();
        var isNewDeck = string.IsNullOrWhiteSpace(_editingDeck.Id);
        var content = BuildScreen(isNewDeck ? "New Deck" : "Edit Deck", "Build a deck from saved cards using compact card buttons and multi-select actions.");

        var body = new HBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ShrinkBegin
        };
        body.AddThemeConstantOverride("separation", 18);
        content.AddChild(body);

        var form = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ShrinkBegin
        };
        form.AddThemeConstantOverride("separation", 8);
        body.AddChild(form);

        _id = AddLineEdit(form, "Deck ID", _editingDeck.Id);
        AddSectionHeader(form, "Deck Assets");
        var assetEditors = new HBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        assetEditors.AddThemeConstantOverride("separation", 12);
        form.AddChild(assetEditors);
        AddDeckBackEditor(assetEditors);
        AddElementIconEditor(assetEditors);
        AddPowerIconEditor(assetEditors);

        AddSaveAsDialog();

        AddSectionHeader(content, "Card Order");

        var lists = new HBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill
        };
        lists.AddThemeConstantOverride("separation", 18);
        content.AddChild(lists);

        _availableCardsPanel = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill
        };
        _availableCardsPanel.SizeFlagsStretchRatio = 1;
        _availableCardsPanel.AddThemeConstantOverride("separation", 8);
        lists.AddChild(_availableCardsPanel);

        _entriesPanel = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill
        };
        _entriesPanel.SizeFlagsStretchRatio = 1;
        _entriesPanel.AddThemeConstantOverride("separation", 8);
        lists.AddChild(_entriesPanel);

        RenderAvailableCards();
        RenderEntries();
        RefreshBackPreview();
        RefreshElementIconPreview();
        RefreshPowerIconPreview();
        AddSectionHeader(content, "Save Options");
        AddEditorActions(content, isNewDeck);
        SetStatus($"Loaded {_availableCards.Count} available card(s). Deck has {GetEntryCardCount()} card(s).");
    }

    private static void AddSectionHeader(VBoxContainer parent, string text)
    {
        AddSeparator(parent);
        parent.AddChild(new Label
        {
            Text = text,
            AutowrapMode = TextServer.AutowrapMode.WordSmart
        });
    }

    private void AddSaveAsDialog()
    {
        _saveAsDialog = new FileDialog
        {
            Access = FileDialog.AccessEnum.Filesystem,
            FileMode = FileDialog.FileModeEnum.SaveFile,
            Title = "Save Deck As New Resource",
            Filters = ["*.tres ; Godot Resource"]
        };
        _saveAsDialog.FileSelected += path => RunGuiAction("Selected deck save-as file", () => OnSaveAsFileSelected(path), $"path={path}");
        AddChild(_saveAsDialog);

        _exportDialog = new FileDialog
        {
            Access = FileDialog.AccessEnum.Filesystem,
            FileMode = FileDialog.FileModeEnum.SaveFile,
            Title = "Export Deck Resource",
            Filters = ["*.tres ; Godot Resource"]
        };
        _exportDialog.FileSelected += path => RunGuiAction("Selected deck export file", () => OnExportFileSelected(path), $"path={path}");
        AddChild(_exportDialog);
    }

    private void AddEditorActions(VBoxContainer content, bool isNewDeck)
    {
        var buttons = new HBoxContainer
        {
            Alignment = BoxContainer.AlignmentMode.Begin
        };
        buttons.AddThemeConstantOverride("separation", 8);
        content.AddChild(buttons);

        if (!isNewDeck)
        {
            AddIconButton(buttons, SaveIconPath, "Save", SaveDeck);
        }

        AddIconButton(buttons, SaveAddIconPath, "Save as new", SaveDeckAsNew);
        AddIconButton(buttons, ExportIconPath, "Export .tres resource", OpenExportDialog);
        AddIconButton(buttons, RefreshIconPath, "Refresh", RefreshEditor);
    }

    private void AddDeckBackEditor(Container parent)
    {
        var panel = new PanelContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        parent.AddChild(panel);

        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_left", 8);
        margin.AddThemeConstantOverride("margin_right", 8);
        margin.AddThemeConstantOverride("margin_top", 8);
        margin.AddThemeConstantOverride("margin_bottom", 8);
        panel.AddChild(margin);

        var layout = new VBoxContainer();
        layout.AddThemeConstantOverride("separation", 8);
        margin.AddChild(layout);

        _backPreviewRow = new HBoxContainer
        {
            Alignment = BoxContainer.AlignmentMode.Center
        };
        _backPreviewRow.AddThemeConstantOverride("separation", 10);
        layout.AddChild(_backPreviewRow);

        _backImageDialog = new FileDialog
        {
            Access = FileDialog.AccessEnum.Filesystem,
            FileMode = FileDialog.FileModeEnum.OpenFile,
            Title = "Select Deck Back Image",
            Filters = [
                "*.png, *.jpg, *.jpeg, *.webp, *.svg ; Supported Images",
                "*.png ; PNG",
                "*.jpg, *.jpeg ; JPEG",
                "*.webp ; WebP",
                "*.svg ; SVG"
            ]
        };
        _backImageDialog.FileSelected += path => RunGuiAction("Selected deck back image file", () => OnBackImageSelected(path), $"path={path}");
        AddChild(_backImageDialog);
    }

    private void AddElementIconEditor(Container parent)
    {
        var panel = new PanelContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        parent.AddChild(panel);

        var margin = new MarginContainer();
        foreach (var side in new[] { "left", "right", "top", "bottom" })
        {
            margin.AddThemeConstantOverride($"margin_{side}", 8);
        }

        panel.AddChild(margin);
        var layout = new VBoxContainer();
        layout.AddThemeConstantOverride("separation", 8);
        margin.AddChild(layout);

        _elementIconPreviewRow = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
        _elementIconPreviewRow.AddThemeConstantOverride("separation", 10);
        layout.AddChild(_elementIconPreviewRow);

        var actions = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
        actions.AddThemeConstantOverride("separation", 8);
        layout.AddChild(actions);
        AddIconButton(actions, ClearIconPath, "Use default element glyphs", ClearElementIcons);

        _elementIconDialog = new FileDialog
        {
            Access = FileDialog.AccessEnum.Filesystem,
            FileMode = FileDialog.FileModeEnum.OpenFile,
            Title = "Select Element Glyph",
            Filters = [
                "*.png, *.jpg, *.jpeg, *.webp, *.svg ; Supported Images",
                "*.png ; PNG",
                "*.jpg, *.jpeg ; JPEG",
                "*.webp ; WebP",
                "*.svg ; SVG"
            ]
        };
        _elementIconDialog.FileSelected += path => RunGuiAction("Selected element glyph", () => OnElementIconSelected(path), $"path={path}");
        AddChild(_elementIconDialog);
    }

    private void AddPowerIconEditor(Container parent)
    {
        var panel = new PanelContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        parent.AddChild(panel);
        var margin = new MarginContainer();
        foreach (var side in new[] { "left", "right", "top", "bottom" })
        {
            margin.AddThemeConstantOverride($"margin_{side}", 8);
        }

        panel.AddChild(margin);
        var layout = new VBoxContainer();
        layout.AddThemeConstantOverride("separation", 8);
        margin.AddChild(layout);
        _powerIconPreviewRow = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
        layout.AddChild(_powerIconPreviewRow);

        _powerIconDialog = new FileDialog
        {
            Access = FileDialog.AccessEnum.Filesystem,
            FileMode = FileDialog.FileModeEnum.OpenFile,
            Title = "Select Power Glyph",
            Filters = [
                "*.png, *.jpg, *.jpeg, *.webp, *.svg ; Supported Images",
                "*.png ; PNG",
                "*.jpg, *.jpeg ; JPEG",
                "*.webp ; WebP",
                "*.svg ; SVG"
            ]
        };
        _powerIconDialog.FileSelected += path => RunGuiAction("Selected power glyph", () => OnPowerIconSelected(path), $"path={path}");
        AddChild(_powerIconDialog);
    }

    private void RenderAvailableCards()
    {
        ClearContainer(_availableCardsPanel);
        _availableCardsPanel.AddChild(new Label
        {
            Text = "Saved Cards",
            AutowrapMode = TextServer.AutowrapMode.WordSmart
        });

        if (_selectedAvailableCardIds.Count > 0)
        {
            var actions = new HBoxContainer();
            actions.AddThemeConstantOverride("separation", 8);
            _availableCardsPanel.AddChild(actions);
            AddIconButton(actions, AddIconPath, "Add selected", AddSelectedAvailableCards);
            AddIconButton(actions, ClearIconPath, "Clear selection", () =>
            {
                _selectedAvailableCardIds.Clear();
                RenderAvailableCards();
            });
        }

        if (_availableCards.Count == 0)
        {
            _availableCardsPanel.AddChild(new Label
            {
                Text = "No saved cards available yet.",
                AutowrapMode = TextServer.AutowrapMode.WordSmart,
                CustomMinimumSize = new Vector2(220, 0),
                SizeFlagsHorizontal = SizeFlags.ExpandFill
            });
            return;
        }

        var scroll = new ScrollContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
            CustomMinimumSize = new Vector2(0, 430),
            HorizontalScrollMode = ScrollContainer.ScrollMode.Auto,
            VerticalScrollMode = ScrollContainer.ScrollMode.Auto
        };
        var scrollPanel = CreateGridScrollPanel(scroll);
        _availableCardsPanel.AddChild(scrollPanel);

        var grid = new HFlowContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill
        };
        grid.AddThemeConstantOverride("h_separation", 12);
        grid.AddThemeConstantOverride("v_separation", 12);
        scroll.AddChild(grid);

        foreach (var card in _availableCards)
        {
            grid.AddChild(CreateCardTile(
                card,
                count: 0,
                isSelected: _selectedAvailableCardIds.Contains(card.Id),
                actions: [
                    new CardTileAction(AddIconPath, "Add one copy", () => AddCard(card), Toggle: false, Pressed: false),
                    new CardTileAction(CheckIconPath, "Select for bulk add", () => ToggleAvailableSelection(card.Id), Toggle: true, Pressed: _selectedAvailableCardIds.Contains(card.Id))
                ]));
        }
    }

    private void RenderEntries()
    {
        ClearContainer(_entriesPanel);
        _entriesPanel.AddChild(new Label
        {
            Text = $"Deck Contents ({GetEntryCardCount()} cards)",
            AutowrapMode = TextServer.AutowrapMode.WordSmart
        });
        if (_selectedDeckCardIds.Count > 0)
        {
            var actions = new HBoxContainer();
            actions.AddThemeConstantOverride("separation", 8);
            _entriesPanel.AddChild(actions);
            AddIconButton(actions, DeleteIconPath, "Remove selected", RemoveSelectedDeckCards);
            AddIconButton(actions, ClearIconPath, "Clear selection", () =>
            {
                _selectedDeckCardIds.Clear();
                RenderEntries();
            });
        }

        var scroll = new ScrollContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
            HorizontalScrollMode = ScrollContainer.ScrollMode.Auto,
            VerticalScrollMode = ScrollContainer.ScrollMode.Auto,
            CustomMinimumSize = new Vector2(0, 430)
        };
        _entriesPanel.AddChild(CreateGridScrollPanel(scroll));

        var flow = new HFlowContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill
        };
        flow.AddThemeConstantOverride("h_separation", 12);
        flow.AddThemeConstantOverride("v_separation", 12);
        scroll.AddChild(flow);

        if (_entries.Count == 0)
        {
            flow.AddChild(new Label
            {
                Text = "No cards in this deck yet.",
                AutowrapMode = TextServer.AutowrapMode.WordSmart
            });
            return;
        }

        foreach (var entry in _entries.ToArray())
        {
            if (entry.Card is null)
            {
                continue;
            }

            flow.AddChild(CreateCardTile(
                entry.Card,
                Math.Max(1, entry.Count),
                _selectedDeckCardIds.Contains(entry.Card.Id),
                [
                    new CardTileAction(string.Empty, "Move earlier", () => MoveEntry(entry, -1), Toggle: false, Pressed: false, Text: "<"),
                    new CardTileAction(string.Empty, "Move later", () => MoveEntry(entry, 1), Toggle: false, Pressed: false, Text: ">"),
                    new CardTileAction(DeleteIconPath, "Remove card type", () => RemoveEntry(entry), Toggle: false, Pressed: false),
                    new CardTileAction(CopyIconPath, "Duplicate one copy", () => AddCard(entry.Card), Toggle: false, Pressed: false),
                    new CardTileAction(CheckIconPath, "Select for bulk remove", () => ToggleDeckSelection(entry.Card.Id), Toggle: true, Pressed: _selectedDeckCardIds.Contains(entry.Card.Id))
                ]));
        }
    }

    private PanelContainer CreateCardTile(CardResource card, int count, bool isSelected, IReadOnlyList<CardTileAction> actions)
    {
        var panel = new PanelContainer
        {
            CustomMinimumSize = new Vector2(154, 0)
        };
        panel.AddThemeStyleboxOverride("panel", BuildTileStyle(isSelected));

        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_left", 8);
        margin.AddThemeConstantOverride("margin_right", 8);
        margin.AddThemeConstantOverride("margin_top", 8);
        margin.AddThemeConstantOverride("margin_bottom", 8);
        panel.AddChild(margin);

        var stack = new VBoxContainer
        {
            CustomMinimumSize = new Vector2(138, 0)
        };
        stack.AddThemeConstantOverride("separation", 6);
        margin.AddChild(stack);

        stack.AddChild(CardPreviewControl.Create(
            card,
            minimumSize: CardThumbnailRenderSize,
            renderSize: CardThumbnailRenderSize,
            deferRender: true,
            elementIconOverrides: _editingDeck.GetElementIconOverrides(),
            powerIconOverride: _editingDeck.PowerIconTexture));
        stack.AddChild(new Label
        {
            Text = card.Id,
            HorizontalAlignment = HorizontalAlignment.Center,
            AutowrapMode = TextServer.AutowrapMode.WordSmart
        });
        stack.AddChild(new Label
        {
            Text = count > 0 ? $"{card.CardType} x{count}" : card.CardType.ToString(),
            HorizontalAlignment = HorizontalAlignment.Center
        });

        var orderActions = actions.Where(action => action.Text is "<" or ">").ToArray();
        var otherActions = actions.Where(action => action.Text is not "<" and not ">").ToArray();
        AddTileActionRow(stack, otherActions);
        if (orderActions.Length > 0)
        {
            AddTileActionRow(stack, orderActions);
        }

        return panel;
    }

    private void AddTileActionRow(VBoxContainer parent, IReadOnlyList<CardTileAction> actions)
    {
        if (actions.Count == 0)
        {
            return;
        }

        var actionRow = new HBoxContainer
        {
            Alignment = BoxContainer.AlignmentMode.Center
        };
        actionRow.AddThemeConstantOverride("separation", 4);
        parent.AddChild(actionRow);

        foreach (var action in actions)
        {
            actionRow.AddChild(CreateTileIconButton(action, new Vector2(36, 34)));
        }
    }

    private Button CreateTileIconButton(CardTileAction action, Vector2 minimumSize)
    {
        var button = new Button
        {
            Text = action.Text,
            CustomMinimumSize = minimumSize,
            ToggleMode = action.Toggle,
            ButtonPressed = action.Pressed,
            TooltipText = action.Tooltip,
            Icon = string.IsNullOrWhiteSpace(action.IconPath) ? null : LoadIcon(action.IconPath),
            ExpandIcon = true
        };
        button.Pressed += LogGuiAction($"Deck tile: {action.Tooltip}", action.OnPressed);
        return button;
    }

    private void ToggleAvailableSelection(string cardId)
    {
        ToggleSelection(_selectedAvailableCardIds, cardId);
        RenderAvailableCards();
    }

    private void ToggleDeckSelection(string cardId)
    {
        ToggleSelection(_selectedDeckCardIds, cardId);
        RenderEntries();
    }

    private static void ToggleSelection(HashSet<string> selection, string cardId)
    {
        if (!selection.Add(cardId))
        {
            selection.Remove(cardId);
        }
    }

    private void AddSelectedAvailableCards()
    {
        var selectedCards = _availableCards.Where(card => _selectedAvailableCardIds.Contains(card.Id)).ToArray();
        foreach (var card in selectedCards)
        {
            AddCard(card, refresh: false);
        }

        _selectedAvailableCardIds.Clear();
        RenderAvailableCards();
        RenderEntries();
        SetStatus(selectedCards.Length == 0 ? "No saved cards selected." : $"Added {selectedCards.Length} selected card type(s).");
    }

    private void RemoveSelectedDeckCards()
    {
        var removeCount = _entries.RemoveAll(entry => entry.Card is not null && _selectedDeckCardIds.Contains(entry.Card.Id));
        _selectedDeckCardIds.Clear();
        RenderEntries();
        SetStatus(removeCount == 0 ? "No deck cards selected." : $"Removed {removeCount} selected card type(s).");
    }

    private void AddCard(CardResource card, bool refresh = true)
    {
        var existing = _entries.FirstOrDefault(entry => entry.Card?.Id == card.Id);
        if (existing is null)
        {
            _entries.Add(new CardDeckEntryResource
            {
                Card = card,
                Count = 1
            });
        }
        else
        {
            existing.Count += 1;
        }

        if (!refresh)
        {
            return;
        }

        RenderEntries();
        SetStatus($"Added '{card.Id}' to deck.");
    }

    private void RemoveEntry(CardDeckEntryResource entry)
    {
        if (entry.Card is not null)
        {
            _selectedDeckCardIds.Remove(entry.Card.Id);
        }

        _entries.Remove(entry);
        RenderEntries();
        SetStatus("Removed card type from deck.");
    }

    private void MoveEntry(CardDeckEntryResource entry, int direction)
    {
        var index = _entries.IndexOf(entry);
        var targetIndex = index + direction;
        if (index < 0 || targetIndex < 0 || targetIndex >= _entries.Count)
        {
            SetStatus("Card is already at the edge of the deck order.");
            return;
        }

        _entries.RemoveAt(index);
        _entries.Insert(targetIndex, entry);
        RenderEntries();
        SetStatus("Updated deck card order.");
    }

    private static StyleBoxFlat BuildTileStyle(bool selected)
    {
        return new StyleBoxFlat
        {
            BgColor = selected ? new Color(0.12f, 0.20f, 0.14f) : new Color(0.12f, 0.13f, 0.15f),
            BorderColor = selected ? new Color(0.48f, 0.90f, 0.48f) : new Color(0.34f, 0.40f, 0.48f),
            BorderWidthLeft = selected ? 2 : 1,
            BorderWidthRight = selected ? 2 : 1,
            BorderWidthTop = selected ? 2 : 1,
            BorderWidthBottom = selected ? 2 : 1,
            CornerRadiusTopLeft = 8,
            CornerRadiusTopRight = 8,
            CornerRadiusBottomLeft = 8,
            CornerRadiusBottomRight = 8
        };
    }

    private void SaveDeck()
    {
        EnsureId();
        ApplyFieldsToDeck();
        var result = CardToolService.SaveDeckToExistingResource(_editingDeck, _originalResourcePath);
        SetStatus(result.Message, !result.Success);
    }

    private void RefreshEditor()
    {
        ApplyFieldsToDeck();
        RenderAvailableCards();
        RenderEntries();
        RefreshBackPreview();
        RefreshElementIconPreview();
        RefreshPowerIconPreview();
        SetStatus($"Refreshed deck editor. Deck has {GetEntryCardCount()} card(s).");
    }

    private void SaveDeckAsNew()
    {
        EnsureId();
        var outputDirectory = ProjectSettings.GlobalizePath(CardGeneration.Services.DeckRepository.UserDecksRootPath);
        Directory.CreateDirectory(outputDirectory);
        _saveAsDialog.CurrentDir = outputDirectory;
        _saveAsDialog.CurrentFile = $"{SanitizeFileName(CreateCopyId(_id.Text))}.tres";
        _saveAsDialog.PopupCenteredRatio(0.72f);
    }

    private void OnSaveAsFileSelected(string filePath)
    {
        var fileId = MakeResourceId(Path.GetFileNameWithoutExtension(filePath), CreateCopyId(_id.Text));
        _id.Text = fileId;
        ApplyFieldsToDeck();
        var result = CardToolService.ExportDeckResource(_editingDeck, EnsureTresExtension(filePath));
        if (result.Success)
        {
            _originalResourcePath = EnsureTresExtension(filePath);
        }

        SetStatus(result.Message, !result.Success);
    }

    private void OpenExportDialog()
    {
        EnsureId();
        _exportDialog.CurrentFile = $"{SanitizeFileName(_id.Text)}.tres";
        _exportDialog.PopupCenteredRatio(0.72f);
    }

    private void OnExportFileSelected(string filePath)
    {
        EnsureId();
        ApplyFieldsToDeck();
        var result = CardToolService.ExportDeckResource(_editingDeck, EnsureTresExtension(filePath));
        SetStatus(result.Message, !result.Success);
    }

    private string CreateCopyId(string sourceId)
    {
        var baseId = string.IsNullOrWhiteSpace(sourceId) ? "new_deck" : sourceId.Trim();
        var existingIds = CardToolService.LoadAllDecks()
            .Select(deck => deck.Id)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        for (var index = 1; index < 1000; index++)
        {
            var candidate = $"{baseId}_copy_{index}";
            if (!existingIds.Contains(candidate))
            {
                return candidate;
            }
        }

        return $"{baseId}_copy_{DateTime.Now:yyyyMMddHHmmss}";
    }

    private void EnsureId()
    {
        if (!string.IsNullOrWhiteSpace(_id.Text))
        {
            return;
        }

        _id.Text = "new_deck";
    }

    private void ApplyFieldsToDeck()
    {
        _editingDeck.Id = _id.Text.Trim();
        _editingDeck.Entries = _entries
            .Where(entry => entry.Card is not null && entry.Count > 0)
            .ToArray();
    }

    private void OpenBackImageDialog(CardType cardType)
    {
        _backImageTargetType = cardType;
        _backImageDialog.PopupCenteredRatio(0.72f);
    }

    private void OnBackImageSelected(string filePath)
    {
        var image = Image.LoadFromFile(filePath);
        if (image is null)
        {
            SetStatus($"Could not load deck back image '{filePath}'.", true);
            return;
        }

        _editingDeck.SetBackImageTexture(_backImageTargetType, ImageTexture.CreateFromImage(image));
        image.Dispose();
        RefreshBackPreview();
        SetStatus($"Loaded {_backImageTargetType} back image '{filePath}'.");
    }

    private void ClearBackImage(CardType cardType)
    {
        _editingDeck.SetBackImageTexture(cardType, null);
        RefreshBackPreview();
        SetStatus($"Using the default {cardType} back image.");
    }

    private void OpenElementIconDialog(ElementType elementType)
    {
        _elementIconTargetType = elementType;
        _elementIconDialog.PopupCenteredRatio(0.72f);
    }

    private void OnElementIconSelected(string filePath)
    {
        var image = Image.LoadFromFile(filePath);
        if (image is null)
        {
            SetStatus($"Could not load {_elementIconTargetType} element glyph '{filePath}'.", true);
            return;
        }

        _editingDeck.SetElementIconTexture(_elementIconTargetType, ImageTexture.CreateFromImage(image));
        image.Dispose();
        RefreshElementIconPreview();
        SetStatus($"Loaded {_elementIconTargetType} element glyph '{filePath}'.");
    }

    private void ClearElementIcons()
    {
        foreach (var elementType in Enum.GetValues<ElementType>())
        {
            _editingDeck.SetElementIconTexture(elementType, null);
        }

        RefreshElementIconPreview();
        SetStatus("Using default element glyphs.");
    }

    private void ClearElementIcon(ElementType elementType)
    {
        _editingDeck.SetElementIconTexture(elementType, null);
        RefreshElementIconPreview();
        SetStatus($"Using the default {elementType} element glyph.");
    }

    private void OpenPowerIconDialog()
    {
        _powerIconDialog.PopupCenteredRatio(0.72f);
    }

    private void OnPowerIconSelected(string filePath)
    {
        var image = Image.LoadFromFile(filePath);
        if (image is null)
        {
            SetStatus($"Could not load power glyph '{filePath}'.", true);
            return;
        }

        _editingDeck.SetPowerIconTexture(ImageTexture.CreateFromImage(image));
        image.Dispose();
        RefreshPowerIconPreview();
        SetStatus($"Loaded power glyph '{filePath}'.");
    }

    private void ClearPowerIcon()
    {
        _editingDeck.SetPowerIconTexture(null);
        RefreshPowerIconPreview();
        SetStatus("Using the power placeholder.");
    }

    private void RefreshBackPreview()
    {
        if (_backPreviewRow is null)
        {
            return;
        }

        ClearContainer(_backPreviewRow);
        AddBackPreview(_backPreviewRow, "Monster", CardType.Monster);
        AddBackPreview(_backPreviewRow, "Terrain", CardType.Terrain);
    }

    private void RefreshElementIconPreview()
    {
        if (_elementIconPreviewRow is null)
        {
            return;
        }

        ClearContainer(_elementIconPreviewRow);
        foreach (var elementType in Enum.GetValues<ElementType>())
        {
            AddElementIconPreview(_elementIconPreviewRow, elementType);
        }
    }

    private void RefreshPowerIconPreview()
    {
        if (_powerIconPreviewRow is null)
        {
            return;
        }

        ClearContainer(_powerIconPreviewRow);
        var column = new VBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
        column.AddThemeConstantOverride("separation", 4);
        _powerIconPreviewRow.AddChild(column);

        column.AddChild(CreateGlyphTexture(_editingDeck.PowerIconTexture));
        column.AddChild(new Label { Text = "Power", HorizontalAlignment = HorizontalAlignment.Center });
        var actions = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
        column.AddChild(actions);
        AddIconButton(actions, BrowseIconPath, "Choose power glyph", OpenPowerIconDialog, new Vector2(36, 34));
        AddIconButton(actions, ClearIconPath, "Use power placeholder", ClearPowerIcon, new Vector2(36, 34));
    }

    private void AddElementIconPreview(HBoxContainer parent, ElementType elementType)
    {
        var column = new VBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
        column.AddThemeConstantOverride("separation", 4);
        parent.AddChild(column);

        var element = CardToolService.LoadAllElements().First(item => item.ElementType == elementType);
        var iconOverrides = _editingDeck.GetElementIconOverrides();
        column.AddChild(CreateGlyphTexture(iconOverrides.TryGetValue(elementType, out var overrideTexture)
            ? overrideTexture
            : element.IconTexture));
        column.AddChild(new Label
        {
            Text = elementType.ToString(),
            HorizontalAlignment = HorizontalAlignment.Center
        });
        AddIconButton(column, BrowseIconPath, $"Choose {elementType} element glyph", () => OpenElementIconDialog(elementType), new Vector2(36, 34));
        AddIconButton(column, ClearIconPath, $"Use default {elementType} element glyph", () => ClearElementIcon(elementType), new Vector2(36, 34));
    }

    private static TextureRect CreateGlyphTexture(Texture2D? texture)
    {
        return new TextureRect
        {
            Texture = texture,
            CustomMinimumSize = new Vector2(52, 52),
            ExpandMode = TextureRect.ExpandModeEnum.FitWidthProportional,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered
        };
    }

    private static PanelContainer CreateGridScrollPanel(ScrollContainer scroll)
    {
        var panel = new PanelContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill
        };
        panel.AddThemeStyleboxOverride("panel", BuildGridStyle());
        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_left", 8);
        margin.AddThemeConstantOverride("margin_right", 8);
        margin.AddThemeConstantOverride("margin_top", 8);
        margin.AddThemeConstantOverride("margin_bottom", 8);
        panel.AddChild(margin);
        margin.AddChild(scroll);
        return panel;
    }

    private static StyleBoxFlat BuildGridStyle()
    {
        return new StyleBoxFlat
        {
            BgColor = new Color(0.075f, 0.08f, 0.10f),
            BorderColor = new Color(0.30f, 0.36f, 0.44f),
            BorderWidthLeft = 1,
            BorderWidthRight = 1,
            BorderWidthTop = 1,
            BorderWidthBottom = 1,
            CornerRadiusTopLeft = 8,
            CornerRadiusTopRight = 8,
            CornerRadiusBottomLeft = 8,
            CornerRadiusBottomRight = 8
        };
    }

    private void AddBackPreview(HBoxContainer parent, string title, CardType cardType)
    {
        var column = new VBoxContainer
        {
            Alignment = BoxContainer.AlignmentMode.Center
        };
        column.AddThemeConstantOverride("separation", 4);
        parent.AddChild(column);

        column.AddChild(CardPreviewControl.Create(
            CreateBackPreviewCard(cardType),
            showBack: true,
            minimumSize: new Vector2(90, 126),
            renderSize: new Vector2I(90, 126),
            useCache: false));
        column.AddChild(new Label
        {
            Text = title,
            HorizontalAlignment = HorizontalAlignment.Center
        });
        AddIconButton(column, BrowseIconPath, $"Choose {title} back image", () => OpenBackImageDialog(cardType), new Vector2(36, 34));
        AddIconButton(column, ClearIconPath, $"Use default {title} back image", () => ClearBackImage(cardType), new Vector2(36, 34));
    }

    private CardResource CreateBackPreviewCard(CardType cardType)
    {
        CardResource card = cardType switch
        {
            CardType.Monster => new MonsterCardResource { Id = "monster_back_preview" },
            CardType.Terrain => new TerrainCardResource { Id = "terrain_back_preview" },
            _ => throw new ArgumentOutOfRangeException(nameof(cardType), cardType, "Only monster and terrain cards are supported.")
        };
        card.BackImageTexture = _editingDeck.GetBackImageTexture(cardType);
        return card;
    }

    private int GetEntryCardCount()
    {
        return _entries.Sum(entry => Math.Max(0, entry.Count));
    }

    private static CardDeckResource CloneDeck(CardDeckResource source)
    {
        return new CardDeckResource
        {
            Id = source.Id,
            MonsterBackImageTexture = source.MonsterBackImageTexture,
            TerrainBackImageTexture = source.TerrainBackImageTexture,
            NeutralElementIconTexture = source.NeutralElementIconTexture,
            GrassElementIconTexture = source.GrassElementIconTexture,
            FlameElementIconTexture = source.FlameElementIconTexture,
            WaterElementIconTexture = source.WaterElementIconTexture,
            AnyElementIconTexture = source.AnyElementIconTexture,
            PowerIconTexture = source.PowerIconTexture,
            Entries = (source.Entries ?? Array.Empty<CardDeckEntryResource>())
                .Select(entry => new CardDeckEntryResource
                {
                    Card = entry.Card,
                    Count = entry.Count
                })
                .ToArray()
        };
    }

    private static string EnsureTresExtension(string filePath)
    {
        return Path.GetExtension(filePath).Equals(".tres", StringComparison.OrdinalIgnoreCase)
            ? filePath
            : $"{filePath}.tres";
    }

    private static string SanitizeFileName(string fileName)
    {
        foreach (var invalidChar in Path.GetInvalidFileNameChars())
        {
            fileName = fileName.Replace(invalidChar, '_');
        }

        return string.IsNullOrWhiteSpace(fileName) ? "deck" : fileName;
    }

    private static void ClearContainer(Container container)
    {
        foreach (var child in container.GetChildren())
        {
            container.RemoveChild(child);
            child.QueueFree();
        }
    }

    private sealed record CardTileAction(string IconPath, string Tooltip, Action OnPressed, bool Toggle, bool Pressed, string Text = "");
}
