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
    [Export] public string MonsterBackImageSourcePath { get; set; } = string.Empty;
    [Export] public string TerrainBackImageSourcePath { get; set; } = string.Empty;
    [Export] public CardImageScaleMode MonsterBackImageScaleMode { get; set; } = CardImageScaleMode.Stretch;
    [Export] public CardImageScaleMode TerrainBackImageScaleMode { get; set; } = CardImageScaleMode.Stretch;
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

    public string GetBackImageSourcePath(CardType cardType)
    {
        return cardType switch
        {
            CardType.Monster => MonsterBackImageSourcePath,
            CardType.Terrain => TerrainBackImageSourcePath,
            _ => string.Empty
        };
    }

    public void SetBackImageSourcePath(CardType cardType, string sourcePath)
    {
        switch (cardType)
        {
            case CardType.Monster:
                MonsterBackImageSourcePath = sourcePath;
                break;
            case CardType.Terrain:
                TerrainBackImageSourcePath = sourcePath;
                break;
        }
    }

    public CardImageScaleMode GetBackImageScaleMode(CardType cardType)
    {
        if (GetBackImageTexture(cardType) is null && string.IsNullOrWhiteSpace(GetBackImageSourcePath(cardType)))
        {
            return CardImageScaleMode.Cover;
        }

        return cardType switch
        {
            CardType.Monster => MonsterBackImageScaleMode,
            CardType.Terrain => TerrainBackImageScaleMode,
            _ => CardImageScaleMode.Cover
        };
    }

    public void SetBackImageScaleMode(CardType cardType, CardImageScaleMode scaleMode)
    {
        switch (cardType)
        {
            case CardType.Monster:
                MonsterBackImageScaleMode = scaleMode;
                break;
            case CardType.Terrain:
                TerrainBackImageScaleMode = scaleMode;
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
