using CardGeneration.Resources.Enums;
using Godot;

namespace CardGeneration.Resources;

[GlobalClass]
public partial class CardDeckResource : Resource
{
    [Export] public string Id { get; set; } = string.Empty;
    [Export] public string DisplayName { get; set; } = string.Empty;
    [Export] public CardType DeckCardType { get; set; } = CardType.Unknown;
    [Export] public Texture2D? BackImageTexture { get; set; }
    [Export(PropertyHint.MultilineText)] public string Description { get; set; } = string.Empty;
    [Export] public CardDeckEntryResource[] Entries { get; set; } = [];
}
