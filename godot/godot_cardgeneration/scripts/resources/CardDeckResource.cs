using Godot;

namespace CardGeneration.Resources;

[GlobalClass]
public partial class CardDeckResource : Resource
{
    [Export] public string Id { get; set; } = string.Empty;
    [Export] public Texture2D? BackImageTexture { get; set; }
    [Export] public CardDeckEntryResource[] Entries { get; set; } = [];
}
