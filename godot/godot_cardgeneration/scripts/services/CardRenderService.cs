using CardGeneration.App;
using CardGeneration.Resources;

namespace CardGeneration.Services;

public sealed class CardRenderService
{
    public ToolResult RenderCard(CardResource card, string outputPath)
    {
        return ToolResult.Ok($"RenderCard is not implemented yet for '{card.Id}' -> {outputPath}.");
    }
}
