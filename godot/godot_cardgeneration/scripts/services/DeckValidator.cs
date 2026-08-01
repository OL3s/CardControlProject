using CardGeneration.App;
using CardGeneration.Resources;

namespace CardGeneration.Services;

public sealed class DeckValidator
{
    public ToolResult Validate(CardDeckResource deck)
    {
        if (string.IsNullOrWhiteSpace(deck.Id))
        {
            return ToolResult.Fail("Deck is missing an id.");
        }

        foreach (var entry in deck.Entries)
        {
            if (entry.Card is null)
            {
                return ToolResult.Fail($"Deck '{deck.Id}' has an entry without a card.");
            }

            if (entry.Count < 1)
            {
                return ToolResult.Fail($"Deck '{deck.Id}' has an entry with count below 1.");
            }
        }

        return ToolResult.Ok($"Validated deck '{deck.Id}'.");
    }
}
