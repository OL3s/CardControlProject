using Godot;
using CardGeneration.Resources.Enums;

namespace CardGeneration.Resources;

[GlobalClass]
public partial class CardDeckResource : Resource
{
    [Export] public string Id { get; set; } = string.Empty;
    [Export] public Texture2D? MonsterBackImageTexture { get; set; }
    [Export] public Texture2D? TerrainBackImageTexture { get; set; }
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
}
