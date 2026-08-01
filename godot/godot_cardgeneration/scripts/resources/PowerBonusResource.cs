using Godot;

namespace CardGeneration.Resources;

[GlobalClass]
public partial class PowerBonusResource : Resource
{
    [Export] public ResourceAmount[] Requirements { get; set; } = [];
    [Export] public int PowerGain { get; set; } = 1;
}
