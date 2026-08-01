using CardGeneration.App;
using CardGeneration.Resources;

namespace CardGeneration.Services;

public sealed class DeckExportService
{
    public ToolResult ExportDeck(CardDeckResource deck, string outputPath, string format)
    {
        return ToolResult.Ok($"ExportDeck is not implemented yet for '{deck.Id}' as {format} -> {outputPath}.");
    }

    public ToolResult ExportShowcase(CardDeckResource deck, string outputPath, string format)
    {
        return ToolResult.Ok($"ExportShowcase is not implemented yet for '{deck.Id}' as {format} -> {outputPath}.");
    }
}
