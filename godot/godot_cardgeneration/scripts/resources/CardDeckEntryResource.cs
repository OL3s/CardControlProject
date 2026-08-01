using Godot;

namespace CardGeneration.Resources;

[GlobalClass]
public partial class CardDeckEntryResource : Resource
{
    [Export] public CardResource? Card { get; set; }
    [Export(PropertyHint.Range, "1,99,1")] public int Count { get; set; } = 1;
}
