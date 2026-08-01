using CardGeneration.Resources.Enums;
using Godot;

namespace CardGeneration.Resources;

[GlobalClass]
public partial class ElementResource : Resource
{
    [Export] public ElementType ElementType { get; set; } = ElementType.Neutral;
    [Export] public string DisplayName { get; set; } = string.Empty;
    [Export] public Texture2D? IconTexture { get; set; }

    public bool IsNeutral()
    {
        return ElementType == ElementType.Neutral;
    }

    public bool IsStrongAgainst(ElementResource? other)
    {
        return GetMatchupAgainst(other) == ElementMatchup.Strong;
    }

    public bool IsWeakAgainst(ElementResource? other)
    {
        return GetMatchupAgainst(other) == ElementMatchup.Weak;
    }

    public ElementMatchup GetMatchupAgainst(ElementResource? other)
    {
        if (other is null)
        {
            return ElementMatchup.Neutral;
        }

        if (ElementType == other.ElementType)
        {
            return ElementMatchup.Same;
        }

        if (ElementType == ElementType.Neutral || other.ElementType == ElementType.Neutral)
        {
            return ElementMatchup.Neutral;
        }

        return Beats(ElementType, other.ElementType) ? ElementMatchup.Strong : ElementMatchup.Weak;
    }

    private static bool Beats(ElementType source, ElementType target)
    {
        return source == ElementType.Water && target == ElementType.Flame
            || source == ElementType.Flame && target == ElementType.Grass
            || source == ElementType.Grass && target == ElementType.Water;
    }
}
