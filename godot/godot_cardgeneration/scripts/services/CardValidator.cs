using System.Collections.Generic;
using System.Linq;
using CardGeneration.App;
using CardGeneration.Resources;
using CardGeneration.Resources.Enums;

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

            if (card.CardType == CardType.Unknown)
            {
                return ToolResult.Fail($"Card '{card.Id}' is missing a card type.");
            }

            if (card is MonsterCardResource monster)
            {
                if (monster.Element is null)
                {
                    return ToolResult.Fail($"Monster card '{monster.Id}' is missing an element.");
                }

                if (monster.Requirements.Length == 0)
                {
                    return ToolResult.Fail($"Monster card '{monster.Id}' must have at least one requirement.");
                }

                if (monster.Requirements.Any(requirement => requirement.Element is null || requirement.Amount < 1))
                {
                    return ToolResult.Fail($"Monster card '{monster.Id}' has an invalid requirement.");
                }
            }
        }

        return ToolResult.Ok($"Validated {cards.Count} cards.");
    }
}
