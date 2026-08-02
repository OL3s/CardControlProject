using CardGeneration.Resources.Enums;
using Godot;

namespace CardGeneration.Resources;

[GlobalClass]
public partial class KingCardResource : CardResource
{
    [Export] public ElementResource? ElementFocus { get; set; }
    [Export] public int Health { get; set; } = 6;
    [Export(PropertyHint.MultilineText)] public string QuestText { get; set; } = string.Empty;
    [Export] public ResourceAmount[] QuestRequirements { get; set; } = [];

    public KingCardResource()
    {
        CardType = CardType.King;
    }
}
