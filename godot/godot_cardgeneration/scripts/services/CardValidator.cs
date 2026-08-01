using System.Collections.Generic;
using CardGeneration.App;
using CardGeneration.Resources;

namespace CardGeneration.Services;

public sealed class CardValidator
{
    public ToolResult Validate(IReadOnlyList<CardResource> cards)
    {
        foreach (var card in cards)
        {
            if (string.IsNullOrWhiteSpace(card.Id))
            {
                return ToolResult.Fail("A card is missing an id.");
            }
        }

        return ToolResult.Ok($"Validated {cards.Count} cards.");
    }
}
