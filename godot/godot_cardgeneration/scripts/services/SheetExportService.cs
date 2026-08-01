using CardGeneration.App;
using CardGeneration.Resources;

namespace CardGeneration.Services;

public sealed class SheetExportService
{
    public ToolResult ExportSheet(CardDeckResource deck, string outputPath, string paper)
    {
        return ToolResult.Ok($"ExportSheet is not implemented yet for '{deck.Id}' on {paper} -> {outputPath}.");
    }
}
