using System;
using System.Collections.Generic;
using System.Linq;
using CardGeneration.Resources;
using Godot;

namespace CardGeneration.Ui;

public partial class DeckPreviewScreen : CardToolScreen
{
    private CardDeckResource? _deck;

    public void SetDeck(CardDeckResource? deck)
    {
        _deck = deck;
    }

    public override void _Ready()
    {
        BuildUi();
    }

    private void BuildUi()
    {
        var deckId = string.IsNullOrWhiteSpace(_deck?.Id) ? "Deck" : _deck!.Id;
        var content = BuildScreen("Deck Preview", $"Full grid preview for {deckId}.");

        AddBackPreviewRow(content);
        AddSeparator(content);
        AddDeckGrid(content);
    }

    private void AddBackPreviewRow(VBoxContainer content)
    {
        content.AddChild(new Label
        {
            Text = "Card Backs",
            HorizontalAlignment = HorizontalAlignment.Center
        });

        var backs = new HBoxContainer
        {
            Alignment = BoxContainer.AlignmentMode.Center,
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        backs.AddThemeConstantOverride("separation", 18);
        content.AddChild(backs);

        AddBackPreview(backs, "Monster", new MonsterCardResource { Id = "monster_back_preview" });
        AddBackPreview(backs, "Terrain", new TerrainCardResource { Id = "terrain_back_preview" });
    }

    private void AddBackPreview(HBoxContainer parent, string title, CardResource card)
    {
        var column = new VBoxContainer
        {
            Alignment = BoxContainer.AlignmentMode.Center
        };
        column.AddThemeConstantOverride("separation", 6);
        parent.AddChild(column);

        card.BackImageTexture = _deck?.GetBackImageTexture(card.CardType);
        column.AddChild(CardPreviewControl.Create(
            card,
            showBack: true,
            minimumSize: new Vector2(90, 126),
            renderSize: new Vector2I(90, 126)));
        column.AddChild(new Label
        {
            Text = title,
            HorizontalAlignment = HorizontalAlignment.Center
        });
    }

    private void AddDeckGrid(VBoxContainer content)
    {
        var cards = ExpandDeckCards().ToArray();
        content.AddChild(new Label
        {
            Text = $"Cards ({cards.Length})",
            HorizontalAlignment = HorizontalAlignment.Center
        });

        if (cards.Length == 0)
        {
            content.AddChild(new Label
            {
                Text = "This deck has no cards to preview.",
                HorizontalAlignment = HorizontalAlignment.Center,
                AutowrapMode = TextServer.AutowrapMode.WordSmart
            });
            SetStatus("Deck preview is empty.");
            return;
        }

        var scroll = new ScrollContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
            VerticalScrollMode = ScrollContainer.ScrollMode.Auto,
            CustomMinimumSize = new Vector2(0, 520)
        };
        content.AddChild(scroll);

        var grid = new HFlowContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill
        };
        grid.AddThemeConstantOverride("h_separation", 12);
        grid.AddThemeConstantOverride("v_separation", 14);
        scroll.AddChild(grid);

        foreach (var card in cards)
        {
            grid.AddChild(CreateCardTile(card));
        }

        SetStatus($"Previewing {cards.Length} card(s) from '{_deck?.Id}'.");
    }

    private IEnumerable<CardResource> ExpandDeckCards()
    {
        foreach (var entry in _deck?.Entries ?? Array.Empty<CardDeckEntryResource>())
        {
            if (entry.Card is null)
            {
                continue;
            }

            for (var index = 0; index < Math.Max(0, entry.Count); index++)
            {
                yield return entry.Card;
            }
        }
    }

    private static PanelContainer CreateCardTile(CardResource card)
    {
        var panel = new PanelContainer
        {
            CustomMinimumSize = new Vector2(146, 0)
        };

        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_left", 7);
        margin.AddThemeConstantOverride("margin_right", 7);
        margin.AddThemeConstantOverride("margin_top", 7);
        margin.AddThemeConstantOverride("margin_bottom", 7);
        panel.AddChild(margin);

        var stack = new VBoxContainer
        {
            CustomMinimumSize = new Vector2(132, 0)
        };
        stack.AddThemeConstantOverride("separation", 5);
        margin.AddChild(stack);

        stack.AddChild(CardPreviewControl.Create(
            card,
            minimumSize: new Vector2(126, 176),
            renderSize: new Vector2I(126, 176),
            deferRender: true));
        stack.AddChild(new Label
        {
            Text = card.Id,
            HorizontalAlignment = HorizontalAlignment.Center,
            AutowrapMode = TextServer.AutowrapMode.WordSmart
        });

        return panel;
    }
}
