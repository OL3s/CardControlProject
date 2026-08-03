using System.Collections.Generic;
using Godot;
using CardGeneration.Resources.Enums;

namespace CardGeneration.Resources;

[GlobalClass]
public partial class CardDeckResource : Resource
{
    [Export] public string Id { get; set; } = string.Empty;
    [Export] public Texture2D? MonsterBackImageTexture { get; set; }
    [Export] public Texture2D? TerrainBackImageTexture { get; set; }
    [Export] public Texture2D? NeutralElementIconTexture { get; set; }
    [Export] public Texture2D? GrassElementIconTexture { get; set; }
    [Export] public Texture2D? FlameElementIconTexture { get; set; }
    [Export] public Texture2D? WaterElementIconTexture { get; set; }
    [Export] public Texture2D? AnyElementIconTexture { get; set; }
    [Export] public Texture2D? PowerIconTexture { get; set; }
    [Export] public CardDeckEntryResource[] Entries { get; set; } = [];

    public Texture2D? GetBackImageTexture(CardType cardType)
    {
        return cardType switch
        {
            CardType.Monster => MonsterBackImageTexture,
            CardType.Terrain => TerrainBackImageTexture,
            _ => null
        };
    }

    public void SetBackImageTexture(CardType cardType, Texture2D? texture)
    {
        switch (cardType)
        {
            case CardType.Monster:
                MonsterBackImageTexture = texture;
                break;
            case CardType.Terrain:
                TerrainBackImageTexture = texture;
                break;
        }
    }

    public Texture2D? GetElementIconTexture(ElementType elementType)
    {
        return elementType switch
        {
            ElementType.Neutral => NeutralElementIconTexture,
            ElementType.Grass => GrassElementIconTexture,
            ElementType.Flame => FlameElementIconTexture,
            ElementType.Water => WaterElementIconTexture,
            ElementType.Any => AnyElementIconTexture,
            _ => null
        };
    }

    public void SetElementIconTexture(ElementType elementType, Texture2D? texture)
    {
        switch (elementType)
        {
            case ElementType.Neutral:
                NeutralElementIconTexture = texture;
                break;
            case ElementType.Grass:
                GrassElementIconTexture = texture;
                break;
            case ElementType.Flame:
                FlameElementIconTexture = texture;
                break;
            case ElementType.Water:
                WaterElementIconTexture = texture;
                break;
            case ElementType.Any:
                AnyElementIconTexture = texture;
                break;
        }
    }

    public void SetPowerIconTexture(Texture2D? texture)
    {
        PowerIconTexture = texture;
    }

    public IReadOnlyDictionary<ElementType, Texture2D> GetElementIconOverrides()
    {
        var overrides = new Dictionary<ElementType, Texture2D>();
        AddOverride(overrides, ElementType.Neutral, NeutralElementIconTexture);
        AddOverride(overrides, ElementType.Grass, GrassElementIconTexture);
        AddOverride(overrides, ElementType.Flame, FlameElementIconTexture);
        AddOverride(overrides, ElementType.Water, WaterElementIconTexture);
        AddOverride(overrides, ElementType.Any, AnyElementIconTexture);
        return overrides;
    }

    private static void AddOverride(IDictionary<ElementType, Texture2D> overrides, ElementType elementType, Texture2D? texture)
    {
        if (texture is not null)
        {
            overrides[elementType] = texture;
        }
    }
}
