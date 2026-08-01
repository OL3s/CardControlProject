using CardGeneration.Resources.Enums;
using Godot;

namespace CardGeneration.Resources;

[GlobalClass]
public partial class MonsterCardResource : CardResource
{
    [Export] public ResourceAmount[] Requirements { get; set; } = [];
    [Export] public int BasePower { get; set; } = 1;
    [Export] public PowerBonusResource[] PowerBonuses { get; set; } = [];
    [Export] public CardEffectResource? Effect { get; set; }

    public MonsterCardResource()
    {
        CardType = CardType.Monster;
    }
}
