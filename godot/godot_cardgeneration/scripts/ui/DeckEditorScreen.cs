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
    private const string CopyIconPath = "res://assets/icons/actions/copy.svg";

    private CardDeckResource _editingDeck = new();
    private readonly List<CardDeckEntryResource> _entries = [];
    private readonly HashSet<string> _selectedAvailableCardIds = [];
    private readonly HashSet<string> _selectedDeckCardIds = [];
    private IReadOnlyList<CardResource> _availableCards = Array.Empty<CardResource>();
    private LineEdit _id = null!;
    private HBoxContainer _backPreviewRow = null!;
    private FileDialog _backImageDialog = null!;
    private FileDialog _saveAsDialog = null!;
    private VBoxContainer _entriesPanel = null!;
    private VBoxContainer _availableCardsPanel = null!;

    public void SetDeck(CardDeckResource? deck)
    {
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
        AddDeckBackEditor(form);

        AddSaveAsDialog();

        var lists = new HBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill
        };
        lists.AddThemeConstantOverride("separation", 18);
        content.AddChild(lists);

        _availableCardsPanel = new VBoxContainer
        {
            CustomMinimumSize = new Vector2(420, 0),
            SizeFlagsVertical = SizeFlags.ExpandFill
        };
        _availableCardsPanel.AddThemeConstantOverride("separation", 8);
        lists.AddChild(_availableCardsPanel);

        _entriesPanel = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill
        };
        _entriesPanel.AddThemeConstantOverride("separation", 8);
        lists.AddChild(_entriesPanel);

        RenderAvailableCards();
        RenderEntries();
        RefreshBackPreview();
        AddEditorActions(content, isNewDeck);
        SetStatus($"Loaded {_availableCards.Count} available card(s). Deck has {GetEntryCardCount()} card(s).");
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
        _saveAsDialog.FileSelected += OnSaveAsFileSelected;
        AddChild(_saveAsDialog);
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
        AddIconButton(buttons, RefreshIconPath, "Refresh", RefreshEditor);
    }

    private void AddDeckBackEditor(VBoxContainer form)
    {
        form.AddChild(new Label { Text = "Deck Back Image" });

        var panel = new PanelContainer
        {
            SizeFlagsHorizontal = SizeFlags.ShrinkBegin
        };
        form.AddChild(panel);

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

        var actions = new HBoxContainer
        {
            Alignment = BoxContainer.AlignmentMode.Center
        };
        actions.AddThemeConstantOverride("separation", 8);
        layout.AddChild(actions);
        AddIconButton(actions, BrowseIconPath, "Choose deck back image", OpenBackImageDialog);
        AddIconButton(actions, ClearIconPath, "Use default backs", ClearBackImage);

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
        _backImageDialog.FileSelected += OnBackImageSelected;
        AddChild(_backImageDialog);
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

        var scroll = new HorizontalWheelScrollContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            CustomMinimumSize = new Vector2(400, 260),
            HorizontalScrollMode = ScrollContainer.ScrollMode.Auto,
            VerticalScrollMode = ScrollContainer.ScrollMode.Disabled
        };
        _availableCardsPanel.AddChild(scroll);

        var row = new HBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        row.AddThemeConstantOverride("separation", 10);
        scroll.AddChild(row);

        if (_availableCards.Count == 0)
        {
            row.AddChild(new Label
            {
                Text = "No saved cards available yet.",
                AutowrapMode = TextServer.AutowrapMode.WordSmart
            });
            return;
        }

        foreach (var card in _availableCards)
        {
            row.AddChild(CreateCardTile(
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
            CustomMinimumSize = new Vector2(560, 430)
        };
        _entriesPanel.AddChild(scroll);

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
            minimumSize: new Vector2(130, 182),
            renderSize: new Vector2I(130, 182),
            deferRender: true));
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

        var actionRow = new HBoxContainer
        {
            Alignment = BoxContainer.AlignmentMode.Center
        };
        actionRow.AddThemeConstantOverride("separation", 4);
        stack.AddChild(actionRow);

        foreach (var action in actions)
        {
            actionRow.AddChild(CreateTileIconButton(action, new Vector2(36, 34)));
        }

        return panel;
    }

    private Button CreateTileIconButton(CardTileAction action, Vector2 minimumSize)
    {
        var button = new Button
        {
            CustomMinimumSize = minimumSize,
            ToggleMode = action.Toggle,
            ButtonPressed = action.Pressed,
            TooltipText = action.Tooltip,
            Icon = LoadIcon(action.IconPath),
            ExpandIcon = true
        };
        button.Pressed += action.OnPressed;
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
        var result = CardToolService.SaveDeck(_editingDeck);
        SetStatus(result.Message, !result.Success);
    }

    private void RefreshEditor()
    {
        ApplyFieldsToDeck();
        RenderAvailableCards();
        RenderEntries();
        RefreshBackPreview();
        SetStatus($"Refreshed deck editor. Deck has {GetEntryCardCount()} card(s).");
    }

    private void SaveDeckAsNew()
    {
        EnsureId();
        var outputDirectory = ProjectPaths.ToGlobalPath("resources/user/decks");
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

    private void OpenBackImageDialog()
    {
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

        _editingDeck.BackImageTexture = ImageTexture.CreateFromImage(image);
        image.Dispose();
        RefreshBackPreview();
        SetStatus($"Loaded deck back image '{filePath}'.");
    }

    private void ClearBackImage()
    {
        _editingDeck.BackImageTexture = null;
        RefreshBackPreview();
        SetStatus("Using default backs.");
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
        AddBackPreview(_backPreviewRow, "King", CardType.King);
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
            minimumSize: new Vector2(72, 101),
            renderSize: new Vector2I(72, 101),
            useCache: false));
        column.AddChild(new Label
        {
            Text = title,
            HorizontalAlignment = HorizontalAlignment.Center
        });
    }

    private CardResource CreateBackPreviewCard(CardType cardType)
    {
        CardResource card = cardType switch
        {
            CardType.Terrain => new TerrainCardResource { Id = "terrain_back_preview" },
            CardType.King => new KingCardResource { Id = "king_back_preview" },
            _ => new MonsterCardResource { Id = "monster_back_preview" }
        };
        card.BackImageTexture = _editingDeck.BackImageTexture;
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
            BackImageTexture = source.BackImageTexture,
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

    private sealed record CardTileAction(string IconPath, string Tooltip, Action OnPressed, bool Toggle, bool Pressed);
}
