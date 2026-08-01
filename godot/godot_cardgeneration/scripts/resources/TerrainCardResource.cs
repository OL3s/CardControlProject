using CardGeneration.Resources.Enums;
using Godot;

namespace CardGeneration.Resources;

[GlobalClass]
public partial class TerrainCardResource : CardResource
{
    [Export] public ResourceAmount[] ProducedResources { get; set; } = [];

    public TerrainCardResource()
    {
        CardType = CardType.Terrain;
    }
}
