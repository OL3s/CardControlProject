using CardGeneration.App;
using CardGeneration.Resources;
using CardGeneration.Resources.Enums;

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

            if (deck.DeckCardType != CardType.Unknown && entry.Card.CardType != deck.DeckCardType)
            {
                return ToolResult.Fail($"Deck '{deck.Id}' has card '{entry.Card.Id}' with type '{entry.Card.CardType}', but deck type is '{deck.DeckCardType}'.");
            }
        }

        return ToolResult.Ok($"Validated deck '{deck.Id}'.");
    }
}
