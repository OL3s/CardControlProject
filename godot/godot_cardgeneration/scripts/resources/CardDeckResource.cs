using Godot;

namespace CardGeneration.Resources;

[GlobalClass]
public partial class CardDeckResource : Resource
{
    [Export] public string Id { get; set; } = string.Empty;
    [Export] public string DisplayName { get; set; } = string.Empty;
    [Export(PropertyHint.MultilineText)] public string Description { get; set; } = string.Empty;
    [Export] public CardDeckEntryResource[] Entries { get; set; } = [];
}
