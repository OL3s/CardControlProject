using CardGeneration.App;
using CardGeneration.Resources;

namespace CardGeneration.Services;

public sealed class DiyExportService
{
    public ToolResult ExportDiy(CardDeckResource deck, string outputPath, string paper)
    {
        return ToolResult.Ok($"ExportDiy is not implemented yet for '{deck.Id}' on {paper} -> {outputPath}.");
    }
}
