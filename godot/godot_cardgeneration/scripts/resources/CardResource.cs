using CardGeneration.Resources.Enums;
using Godot;

namespace CardGeneration.Resources;

[GlobalClass]
public partial class CardResource : Resource
{
    [Export] public string Id { get; set; } = string.Empty;
    [Export] public string DisplayName { get; set; } = string.Empty;
    [Export] public CardType CardType { get; set; } = CardType.Unknown;
    [Export] public ElementResource? Element { get; set; }
    [Export] public int InternalTier { get; set; }
    [Export] public Texture2D? CardImageTexture { get; set; }
    [Export] public Texture2D? BackImageTexture { get; set; }
    [Export(PropertyHint.MultilineText)] public string Notes { get; set; } = string.Empty;
}
