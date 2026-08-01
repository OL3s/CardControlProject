using Godot;

namespace CardGeneration.Resources;

[GlobalClass]
public partial class ResourceAmount : Resource
{
    [Export] public ElementResource? Element { get; set; }
    [Export(PropertyHint.Range, "0,20,1")] public int Amount { get; set; } = 1;
}
