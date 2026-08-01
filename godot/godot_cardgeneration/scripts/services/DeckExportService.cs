using CardGeneration.App;
using CardGeneration.Resources;

namespace CardGeneration.Services;

public sealed class DeckExportService
{
    private readonly CardRenderService _cardRenderService;

    public DeckExportService()
        : this(new CardRenderService())
    {
    }

    public DeckExportService(CardRenderService cardRenderService)
    {
        _cardRenderService = cardRenderService;
    }

    public ToolResult ExportDeck(CardDeckResource deck, string outputPath, string format)
    {
        if (format != "png")
        {
            return ToolResult.Fail($"Deck export format '{format}' is not supported yet. Use png.");
        }

        var renderedCount = 0;
        foreach (var entry in deck.Entries)
        {
            if (entry.Card is null)
            {
                return ToolResult.Fail($"Deck '{deck.Id}' has an entry without a card.");
            }

            for (var copyIndex = 0; copyIndex < entry.Count; copyIndex++)
            {
                renderedCount++;
                var fileNameStem = $"{deck.Id}_{renderedCount:000}_{entry.Card.Id}";
                var result = _cardRenderService.RenderCard(entry.Card, outputPath, fileNameStem);
                if (!result.Success)
                {
                    return result;
                }
            }
        }

        return ToolResult.Ok($"Exported {renderedCount} cards from deck '{deck.Id}' to {outputPath}.");
    }

    public ToolResult ExportShowcase(CardDeckResource deck, string outputPath, string format)
    {
        return ToolResult.Ok($"ExportShowcase is not implemented yet for '{deck.Id}' as {format} -> {outputPath}.");
    }
}
