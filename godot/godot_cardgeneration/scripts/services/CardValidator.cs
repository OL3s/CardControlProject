using System;
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

            if (card is not MonsterCardResource && card is not TerrainCardResource)
            {
                return ToolResult.Fail($"Card '{card.Id}' must be a monster or terrain card.");
            }

            if (card is MonsterCardResource && card.CardType != CardType.Monster
                || card is TerrainCardResource && card.CardType != CardType.Terrain)
            {
                return ToolResult.Fail($"Card '{card.Id}' has a card type that does not match its resource type.");
            }

            if (card.Element is null || !Enum.IsDefined(card.Element.ElementType))
            {
                return ToolResult.Fail($"Card '{card.Id}' must have an explicit valid element.");
            }

            if (card is MonsterCardResource monster)
            {
                if (monster.Tier is < 1 or > 3)
                {
                    return ToolResult.Fail($"Monster card '{monster.Id}' must use tier 1, 2 or 3.");
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

            if (card is TerrainCardResource terrain
                && (terrain.ProducedResources.Length == 0
                    || terrain.ProducedResources.Any(resource => resource.Element is null || resource.Amount < 1)))
            {
                return ToolResult.Fail($"Terrain card '{terrain.Id}' must produce at least one valid resource.");
            }

        }

        return ToolResult.Ok($"Validated {cards.Count} cards.");
    }
}
