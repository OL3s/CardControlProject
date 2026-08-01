using Godot;

namespace CardGeneration.Resources;

[GlobalClass]
public partial class CardEffectResource : Resource
{
    [Export] public string EffectId { get; set; } = string.Empty;
    [Export] public Texture2D? IconTexture { get; set; }
    [Export(PropertyHint.MultilineText)] public string RulesText { get; set; } = string.Empty;
}
