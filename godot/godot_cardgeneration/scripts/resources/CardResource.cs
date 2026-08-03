using CardGeneration.Resources.Enums;
using Godot;

namespace CardGeneration.Resources;

[GlobalClass]
public partial class CardResource : Resource
{
    [Export] public string Id { get; set; } = string.Empty;
    [Export] public CardType CardType { get; set; } = CardType.Unknown;
    [Export] public ElementResource? Element { get; set; }
    [Export] public Texture2D? CardImageTexture { get; set; }
    [Export] public string CardImageSourcePath { get; set; } = string.Empty;
    [Export] public Texture2D? BackImageTexture { get; set; }
}
