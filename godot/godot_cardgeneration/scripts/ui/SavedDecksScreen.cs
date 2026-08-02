using System;
using System.Collections.Generic;
using System.Linq;
using CardGeneration.Resources;
using Godot;

namespace CardGeneration.Ui;

public partial class SavedDecksScreen : CardToolScreen
{
    private IReadOnlyList<CardDeckResource> _decks = Array.Empty<CardDeckResource>();
    private PopupMenu _createDeckMenu = null!;
    private CardPreviewControl _frontPreview = null!;
    private CardPreviewControl _backPreview = null!;
    private Label _details = null!;

    public event Action<CardDeckResource?>? EditDeckRequested;
    public event Action<CardDeckResource?>? NewDeckRequested;

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
        AddButton(toolbar, "+", ShowCreateDeckMenu, 44).TooltipText = "Create deck";
        AddButton(toolbar, "Refresh", BuildUi);

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

        var previewColumn = new VBoxContainer
        {
            CustomMinimumSize = new Vector2(300, 0),
            SizeFlagsHorizontal = SizeFlags.ShrinkEnd,
            SizeFlagsVertical = SizeFlags.ExpandFill
        };
        previewColumn.AddThemeConstantOverride("separation", 8);
        body.AddChild(previewColumn);

        previewColumn.AddChild(new Label { Text = "First Card Front" });
        _frontPreview = CardPreviewControl.Create(minimumSize: new Vector2(220, 308), renderSize: new Vector2I(220, 308));
        previewColumn.AddChild(_frontPreview);

        previewColumn.AddChild(new Label { Text = "First Card Back" });
        _backPreview = CardPreviewControl.Create(showBack: true, minimumSize: new Vector2(220, 308), renderSize: new Vector2I(220, 308));
        previewColumn.AddChild(_backPreview);
        _details = new Label
        {
            Text = "Select a deck to preview its first card.",
            AutowrapMode = TextServer.AutowrapMode.WordSmart
        };
        previewColumn.AddChild(_details);

        if (_decks.Count == 0)
        {
            list.AddChild(new Label
            {
                Text = "No saved decks found in res://resources/decks.",
                AutowrapMode = TextServer.AutowrapMode.WordSmart
            });
            return;
        }

        foreach (var deck in _decks)
        {
            AddDeckRow(list, deck);
        }

        ShowDeck(_decks[0]);
        SetStatus($"Loaded {_decks.Count} saved deck(s).");
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

        var row = new VBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        row.AddThemeConstantOverride("separation", 6);
        rowMargin.AddChild(row);

        row.AddChild(new Label
        {
            Text = $"{deck.Id} | {GetCardCount(deck)} cards | {GetCardComposition(deck)}",
            AutowrapMode = TextServer.AutowrapMode.WordSmart
        });

        var buttons = new HBoxContainer();
        buttons.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        buttons.AddThemeConstantOverride("separation", 8);
        row.AddChild(buttons);
        AddButton(buttons, "Preview", () => ShowDeck(deck), 86);
        AddButton(buttons, "Edit", () => EditDeckRequested?.Invoke(deck), 76);
    }

    private void ShowDeck(CardDeckResource deck)
    {
        var firstCard = (deck.Entries ?? Array.Empty<CardDeckEntryResource>()).FirstOrDefault(entry => entry.Card is not null)?.Card;
        _frontPreview.SetCard(firstCard);
        _backPreview.SetCard(firstCard, showBack: true);
        _details.Text = $"ID: {deck.Id}\nCards: {GetCardCount(deck)}\nEntries: {(deck.Entries ?? Array.Empty<CardDeckEntryResource>()).Length}\n{GetCardComposition(deck)}";
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
}
