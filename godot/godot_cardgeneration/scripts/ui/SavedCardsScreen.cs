using System;
using System.Collections.Generic;
using CardGeneration.Resources;
using Godot;

namespace CardGeneration.Ui;

public partial class SavedCardsScreen : CardToolScreen
{
    private IReadOnlyList<CardResource> _cards = Array.Empty<CardResource>();
    private CardPreviewControl _frontPreview = null!;
    private CardPreviewControl _backPreview = null!;
    private Label _details = null!;

    public event Action<CardResource?>? EditCardRequested;
    public event Action? NewCardRequested;

    public override void _Ready()
    {
        BuildUi();
    }

    private void BuildUi()
    {
        _cards = CardToolService.LoadAllCards();

        var content = BuildScreen("Cards", "Browse card resources, preview them, edit them, or export a single card.");

        var toolbar = new HBoxContainer
        {
            Alignment = BoxContainer.AlignmentMode.Begin,
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        toolbar.AddThemeConstantOverride("separation", 10);
        content.AddChild(toolbar);
        AddButton(toolbar, "+", () => NewCardRequested?.Invoke(), 44).TooltipText = "Create card";
        AddButton(toolbar, "Refresh", BuildUi);

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

        previewColumn.AddChild(new Label { Text = "Front" });
        _frontPreview = CardPreviewControl.Create(minimumSize: new Vector2(220, 308), renderSize: new Vector2I(220, 308));
        previewColumn.AddChild(_frontPreview);

        previewColumn.AddChild(new Label { Text = "Back" });
        _backPreview = CardPreviewControl.Create(showBack: true, minimumSize: new Vector2(220, 308), renderSize: new Vector2I(220, 308));
        previewColumn.AddChild(_backPreview);

        _details = new Label
        {
            Text = "Select a card to preview it.",
            AutowrapMode = TextServer.AutowrapMode.WordSmart
        };
        previewColumn.AddChild(_details);

        if (_cards.Count == 0)
        {
            list.AddChild(new Label
            {
                Text = "No saved cards found in res://resources/cards.",
                AutowrapMode = TextServer.AutowrapMode.WordSmart
            });
            return;
        }

        foreach (var card in _cards)
        {
            AddCardRow(list, card);
        }

        ShowCard(_cards[0]);
        SetStatus($"Loaded {_cards.Count} saved card(s).");
    }

    private void AddCardRow(VBoxContainer list, CardResource card)
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
            Text = $"{card.Id} | {card.CardType}",
            AutowrapMode = TextServer.AutowrapMode.WordSmart
        });

        var buttons = new HBoxContainer();
        buttons.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        buttons.AddThemeConstantOverride("separation", 8);
        row.AddChild(buttons);

        AddButton(buttons, "Preview", () => ShowCard(card), 86);
        AddButton(buttons, "Edit", () => EditCardRequested?.Invoke(card), 76);
    }

    private void ShowCard(CardResource card)
    {
        _frontPreview.SetCard(card);
        _backPreview.SetCard(card, showBack: true);
        _details.Text = card switch
        {
            MonsterCardResource => $"ID: {card.Id}\nType: {card.CardType}\nTier: {card.InternalTier}\nDerived Element: {CardElementResolver.GetCardElementType(card)}",
            KingCardResource king => $"ID: {card.Id}\nType: {card.CardType}\nTier: {card.InternalTier}\nElement Focus: {king.ElementFocus?.DisplayName ?? CardElementResolver.GetCardElementType(card).ToString()}",
            _ => $"ID: {card.Id}\nType: {card.CardType}\nTier: {card.InternalTier}"
        };
    }

}
