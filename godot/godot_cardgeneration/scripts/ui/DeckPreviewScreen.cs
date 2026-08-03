using System;
using System.Collections.Generic;
using System.Linq;
using CardGeneration.Resources;
using Godot;

namespace CardGeneration.Ui;

public partial class DeckPreviewScreen : CardToolScreen
{
    public static readonly Vector2I CardPreviewRenderSize = new(126, 176);
    public static readonly Vector2I BackPreviewRenderSize = new(90, 126);

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
        var rowScroll = new ScrollContainer
        {
            HorizontalScrollMode = ScrollContainer.ScrollMode.Auto,
            VerticalScrollMode = ScrollContainer.ScrollMode.Disabled,
            CustomMinimumSize = new Vector2(0, 150)
        };
        content.AddChild(rowScroll);

        var backs = new HBoxContainer
        {
            Alignment = BoxContainer.AlignmentMode.Center,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            CustomMinimumSize = new Vector2(420, 0)
        };
        backs.AddThemeConstantOverride("separation", 18);
        rowScroll.AddChild(backs);

        AddBackPreview(backs, new MonsterCardResource { Id = "monster_back_preview" });
        AddBackPreview(backs, new TerrainCardResource { Id = "terrain_back_preview" });
        AddDeckGlyphPreview(backs);
    }

    private void AddDeckGlyphPreview(HBoxContainer parent)
    {
        var separator = new VSeparator
        {
            CustomMinimumSize = new Vector2(12, 120)
        };
        parent.AddChild(separator);

        var glyphs = new HBoxContainer
        {
            Alignment = BoxContainer.AlignmentMode.Center
        };
        glyphs.AddThemeConstantOverride("separation", 10);
        parent.AddChild(glyphs);

        AddDeckGlyph(glyphs, "Neutral", _deck?.NeutralElementIconTexture);
        AddDeckGlyph(glyphs, "Grass", _deck?.GrassElementIconTexture);
        AddDeckGlyph(glyphs, "Flame", _deck?.FlameElementIconTexture);
        AddDeckGlyph(glyphs, "Water", _deck?.WaterElementIconTexture);
        AddDeckGlyph(glyphs, "Any", _deck?.AnyElementIconTexture);
        AddDeckGlyph(glyphs, "Power", _deck?.PowerIconTexture);
    }

    private static void AddDeckGlyph(HBoxContainer parent, string title, Texture2D? texture)
    {
        var column = new VBoxContainer
        {
            CustomMinimumSize = new Vector2(58, 0),
            Alignment = BoxContainer.AlignmentMode.Center
        };
        column.AddThemeConstantOverride("separation", 5);
        parent.AddChild(column);
        column.AddChild(new TextureRect
        {
            Texture = texture,
            CustomMinimumSize = new Vector2(52, 52),
            ExpandMode = TextureRect.ExpandModeEnum.FitWidthProportional,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            TooltipText = $"Deck {title} glyph"
        });
        column.AddChild(new Label
        {
            Text = title,
            HorizontalAlignment = HorizontalAlignment.Center
        });
    }

    private void AddBackPreview(HBoxContainer parent, CardResource card)
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
            minimumSize: BackPreviewRenderSize,
            renderSize: BackPreviewRenderSize));
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

    private PanelContainer CreateCardTile(CardResource card)
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
            minimumSize: CardPreviewRenderSize,
            renderSize: CardPreviewRenderSize,
            deferRender: true,
            elementIconOverrides: _deck?.GetElementIconOverrides(),
            powerIconOverride: _deck?.PowerIconTexture));
        stack.AddChild(new Label
        {
            Text = card.Id,
            HorizontalAlignment = HorizontalAlignment.Center,
            AutowrapMode = TextServer.AutowrapMode.WordSmart
        });

        return panel;
    }
}
