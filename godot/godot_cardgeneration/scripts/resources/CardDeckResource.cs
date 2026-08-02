using Godot;
using CardGeneration.Resources.Enums;

namespace CardGeneration.Resources;

[GlobalClass]
public partial class CardDeckResource : Resource
{
    [Export] public string Id { get; set; } = string.Empty;
    [Export] public Texture2D? MonsterBackImageTexture { get; set; }
    [Export] public Texture2D? TerrainBackImageTexture { get; set; }
    [Export] public Texture2D? KingBackImageTexture { get; set; }
    [Export] public CardDeckEntryResource[] Entries { get; set; } = [];

    public Texture2D? GetBackImageTexture(CardType cardType)
    {
        return cardType switch
        {
            CardType.Terrain => TerrainBackImageTexture,
            CardType.King => KingBackImageTexture,
            _ => MonsterBackImageTexture
        };
    }

    public void SetBackImageTexture(CardType cardType, Texture2D? texture)
    {
        switch (cardType)
        {
            case CardType.Terrain:
                TerrainBackImageTexture = texture;
                break;
            case CardType.King:
                KingBackImageTexture = texture;
                break;
            default:
                MonsterBackImageTexture = texture;
                break;
        }
    }
}
