using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CardGeneration.App;
using CardGeneration.Resources;
using CardGeneration.Resources.Enums;
using Godot;

namespace CardGeneration.Ui;

public partial class SavedDecksScreen : CardToolScreen
{
    private IReadOnlyList<CardDeckResource> _decks = Array.Empty<CardDeckResource>();
    private PopupMenu _createDeckMenu = null!;
    private Label _details = null!;
    private FileDialog _importDialog = null!;
    public event Action<CardDeckResource?>? EditDeckRequested;
    public event Action<CardDeckResource?>? NewDeckRequested;
    public event Action<CardDeckResource>? PreviewDeckRequested;

    public override void _Ready()
    {
        BuildUi();
    }

    private void BuildUi()
    {
        _decks = CardToolService.LoadAllDecks();
        var content = BuildScreen("Decks", "Browse decks, inspect their card count, edit them, or export with the saved defaults.");

        var toolbar = new HBoxContainer();
        toolbar.AddThemeConstantOverride("separation", 10);
        content.AddChild(toolbar);
        AddIconButton(toolbar, DeckIconPath, "Create deck", ShowCreateDeckMenu);
        AddIconButton(toolbar, ImportIconPath, "Import deck resource", OpenImportDialog);
        AddIconButton(toolbar, CheckIconPath, "Validate decks", ValidateDecks);
        AddIconButton(toolbar, RefreshIconPath, "Refresh", RefreshDefaultsAndBuildUi);
        AddResourceDialogs();

        _createDeckMenu = new PopupMenu();
        _createDeckMenu.AddItem("New Empty Deck", 0);
        _createDeckMenu.AddItem("Default 52-Card Preset", 1);
        _createDeckMenu.IdPressed += OnCreateDeckMenuPressed;
        AddChild(_createDeckMenu);

        var body = new HBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill
        };
        body.AddThemeConstantOverride("separation", 16);
        content.AddChild(body);

        var list = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill
        };
        list.AddThemeConstantOverride("separation", 10);
        body.AddChild(list);

        _details = new Label
        {
            Text = $"Deck Count: {_decks.Count}",
            AutowrapMode = TextServer.AutowrapMode.WordSmart
        };
        list.AddChild(_details);

        if (_decks.Count == 0)
        {
            list.AddChild(new Label
            {
                Text = "No decks found in default resources or user://resources/decks.",
                AutowrapMode = TextServer.AutowrapMode.WordSmart
            });
            return;
        }

        foreach (var deck in _decks)
        {
            AddDeckRow(list, deck);
        }

        SetStatus($"Loaded {_decks.Count} saved deck(s).");
    }

    private void RefreshDefaultsAndBuildUi()
    {
        var defaultResult = CardToolService.EnsureDefaultResources();
        BuildUi();
        SetStatus(defaultResult.Message, !defaultResult.Success);
    }

    private void AddDeckRow(VBoxContainer list, CardDeckResource deck)
    {
        var panel = new PanelContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ShrinkBegin
        };
        list.AddChild(panel);

        var rowMargin = new MarginContainer();
        rowMargin.AddThemeConstantOverride("margin_left", 10);
        rowMargin.AddThemeConstantOverride("margin_right", 10);
        rowMargin.AddThemeConstantOverride("margin_top", 8);
        rowMargin.AddThemeConstantOverride("margin_bottom", 8);
        panel.AddChild(rowMargin);

        var row = new HBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            Alignment = BoxContainer.AlignmentMode.Center
        };
        row.AddThemeConstantOverride("separation", 12);
        rowMargin.AddChild(row);

        var info = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        info.AddThemeConstantOverride("separation", 6);
        row.AddChild(info);

        info.AddChild(new Label
        {
            Text = deck.Id,
            AutowrapMode = TextServer.AutowrapMode.WordSmart
        });

        AddDeckStatsRow(info, deck);

        var buttons = new HBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ShrinkEnd,
            Alignment = BoxContainer.AlignmentMode.End
        };
        buttons.AddThemeConstantOverride("separation", 8);
        row.AddChild(buttons);
        AddIconButton(buttons, PreviewIconPath, "Preview full deck", () =>
        {
            PreviewDeckRequested?.Invoke(deck);
        });
        AddIconButton(buttons, EditIconPath, "Edit", () => EditDeckRequested?.Invoke(deck));
        AddIconButton(buttons, CopyIconPath, "Duplicate", () => DuplicateDeck(deck));
        AddIconButton(buttons, DeleteIconPath, "Delete", () => DeleteDeck(deck));
    }

    private void ShowDeck(CardDeckResource deck)
    {
        _details.Text = $"Deck Count: {_decks.Count}";
    }

    private void AddDeckStatsRow(VBoxContainer parent, CardDeckResource deck)
    {
        var stats = new HBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        stats.AddThemeConstantOverride("separation", 12);
        parent.AddChild(stats);

        AddIconCount(stats, CardCountIconPath, "Cards", GetCardCount(deck));
        AddIconCount(stats, MonsterTypeIconPath, "Monsters", GetCardTypeCount(deck, CardType.Monster));
        AddIconCount(stats, TerrainTypeIconPath, "Terrain", GetCardTypeCount(deck, CardType.Terrain));
        AddIconCount(stats, KingTypeIconPath, "Kings", GetCardTypeCount(deck, CardType.King));
    }

    private static void AddIconCount(HBoxContainer parent, string iconPath, string tooltip, int count)
    {
        if (count <= 0)
        {
            return;
        }

        var item = new HBoxContainer
        {
            Alignment = BoxContainer.AlignmentMode.Center,
            TooltipText = tooltip
        };
        item.AddThemeConstantOverride("separation", 4);
        parent.AddChild(item);

        item.AddChild(new TextureRect
        {
            Texture = LoadIcon(iconPath),
            CustomMinimumSize = new Vector2(24, 24),
            ExpandMode = TextureRect.ExpandModeEnum.FitWidthProportional,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            TooltipText = tooltip
        });
        item.AddChild(new Label
        {
            Text = $"x {count}",
            VerticalAlignment = VerticalAlignment.Center,
            TooltipText = tooltip
        });
    }

    private void ShowCreateDeckMenu()
    {
        _createDeckMenu.PopupCentered(new Vector2I(260, 96));
    }

    private void OnCreateDeckMenuPressed(long id)
    {
        var deck = id == 1
            ? CardToolService.CreateDefault52CardDeck()
            : CardToolService.CreateEmptyDeck();
        NewDeckRequested?.Invoke(deck);
    }

    private static int GetCardCount(CardDeckResource deck)
    {
        return (deck.Entries ?? Array.Empty<CardDeckEntryResource>()).Sum(entry => Math.Max(0, entry.Count));
    }

    private static int GetCardTypeCount(CardDeckResource deck, CardType cardType)
    {
        return (deck.Entries ?? Array.Empty<CardDeckEntryResource>())
            .Where(entry => entry.Card?.CardType == cardType)
            .Sum(entry => Math.Max(0, entry.Count));
    }

    private static string GetCardComposition(CardDeckResource deck)
    {
        var groups = (deck.Entries ?? Array.Empty<CardDeckEntryResource>())
            .Where(entry => entry.Card is not null)
            .GroupBy(entry => entry.Card!.CardType)
            .OrderBy(group => group.Key)
            .Select(group => $"{group.Sum(entry => Math.Max(0, entry.Count))} {group.Key}")
            .ToArray();
        return groups.Length == 0 ? "empty" : string.Join(", ", groups);
    }

    private void AddResourceDialogs()
    {
        _importDialog = CreateResourceDialog("Import Deck Resource", FileDialog.FileModeEnum.OpenFile);
        _importDialog.FileSelected += OnImportFileSelected;
        AddChild(_importDialog);

    }

    private static FileDialog CreateResourceDialog(string title, FileDialog.FileModeEnum fileMode)
    {
        return new FileDialog
        {
            Access = FileDialog.AccessEnum.Filesystem,
            FileMode = fileMode,
            Title = title,
            Filters = ["*.tres ; Godot Resource"]
        };
    }

    private void OpenImportDialog()
    {
        var outputDirectory = ProjectSettings.GlobalizePath(CardGeneration.Services.DeckRepository.UserDecksRootPath);
        Directory.CreateDirectory(outputDirectory);
        _importDialog.CurrentDir = outputDirectory;
        _importDialog.PopupCenteredRatio(0.72f);
    }

    private void OnImportFileSelected(string filePath)
    {
        var result = CardToolService.ImportDeckResource(filePath);
        BuildUi();
        SetStatus(result.Message, !result.Success);
    }

    private void ValidateDecks()
    {
        var result = CardToolService.ValidateDecks();
        SetStatus(result.Message, !result.Success);
    }

    private void DuplicateDeck(CardDeckResource deck)
    {
        var result = CardToolService.DuplicateDeck(deck.Id);
        BuildUi();
        SetStatus(result.Message, !result.Success);
    }

    private void DeleteDeck(CardDeckResource deck)
    {
        var result = CardToolService.DeleteDeck(deck.Id);
        BuildUi();
        SetStatus(result.Message, !result.Success);
    }

}
