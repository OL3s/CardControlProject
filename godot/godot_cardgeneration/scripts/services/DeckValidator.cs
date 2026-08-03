using System;
using System.Linq;
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

            if (entry.Card is not MonsterCardResource && entry.Card is not TerrainCardResource)
            {
                return ToolResult.Fail($"Deck '{deck.Id}' contains a card that is not a monster or terrain card.");
            }

            if (entry.Card is MonsterCardResource && entry.Card.CardType != CardType.Monster
                || entry.Card is TerrainCardResource && entry.Card.CardType != CardType.Terrain)
            {
                return ToolResult.Fail($"Deck '{deck.Id}' contains a card whose type does not match its resource type.");
            }

            if (entry.Card.Element is null || !Enum.IsDefined(entry.Card.Element.ElementType))
            {
                return ToolResult.Fail($"Deck '{deck.Id}' contains card '{entry.Card.Id}' without an explicit valid element.");
            }

            if (entry.Card is MonsterCardResource monster && monster.Tier is < 1 or > 3)
            {
                return ToolResult.Fail($"Deck '{deck.Id}' contains monster '{monster.Id}' without an explicit valid tier.");
            }
        }

        if (deck.Id == DefaultDeckFactory.Default52CardDeckId)
        {
            var defaultResult = ValidateDefaultDeck(deck);
            if (!defaultResult.Success)
            {
                return defaultResult;
            }
        }

        return ToolResult.Ok($"Validated deck '{deck.Id}'.");
    }

    private static ToolResult ValidateDefaultDeck(CardDeckResource deck)
    {
        var cards = deck.Entries
            .SelectMany(entry => Enumerable.Repeat(entry.Card!, entry.Count))
            .ToArray();

        if (cards.Length != 52)
        {
            return ToolResult.Fail($"Default deck must contain 52 cards, but contains {cards.Length}.");
        }

        if (cards.Select(card => card.Id).Distinct(StringComparer.Ordinal).Count() != cards.Length)
        {
            return ToolResult.Fail("Every card in the default deck must have a unique id.");
        }

        var terrains = cards.OfType<TerrainCardResource>().ToArray();
        var monsters = cards.OfType<MonsterCardResource>().ToArray();
        if (terrains.Length != 20 || monsters.Length != 32)
        {
            return ToolResult.Fail($"Default deck must contain 20 terrain and 32 monsters; found {terrains.Length} and {monsters.Length}.");
        }

        foreach (var elementType in Enum.GetValues<ElementType>())
        {
            if (elementType == ElementType.Any)
            {
                continue;
            }

            var elementName = elementType.ToString().ToLowerInvariant();
            var elementMonsters = monsters.Where(monster => monster.Id.StartsWith($"default_monster_{elementName}_", StringComparison.Ordinal)).ToArray();
            var tier1Count = elementMonsters.Count(monster => monster.Id.StartsWith($"default_monster_{elementName}_1_", StringComparison.Ordinal));
            var tier2Count = elementMonsters.Count(monster => monster.Id.StartsWith($"default_monster_{elementName}_2_", StringComparison.Ordinal));
            var tier3Count = elementMonsters.Count(monster => monster.Id.StartsWith($"default_monster_{elementName}_3_", StringComparison.Ordinal));
            if (elementMonsters.Length != 8 || tier1Count != 4 || tier2Count != 3 || tier3Count != 1)
            {
                return ToolResult.Fail($"Default deck monsters for {elementName} must use a 4/3/1 tier distribution.");
            }

            if (elementMonsters.Any(monster =>
                    monster.Id.StartsWith($"default_monster_{elementName}_1_", StringComparison.Ordinal) && monster.Tier != 1
                    || monster.Id.StartsWith($"default_monster_{elementName}_2_", StringComparison.Ordinal) && monster.Tier != 2
                    || monster.Id.StartsWith($"default_monster_{elementName}_3_", StringComparison.Ordinal) && monster.Tier != 3))
            {
                return ToolResult.Fail($"Default deck monster tier metadata for {elementName} must match its source list.");
            }

            if (elementMonsters.Any(monster => monster.Element?.ElementType != elementType))
            {
                return ToolResult.Fail($"Every default monster in the {elementName} group must use the {elementName} element.");
            }

            var elementTerrains = terrains.Where(terrain => terrain.Id.StartsWith($"default_terrain_{elementName}_", StringComparison.Ordinal)).ToArray();
            var expectedTerrainCount = elementType == ElementType.Neutral ? 8 : 4;
            if (elementTerrains.Length != expectedTerrainCount
                || elementTerrains.Any(terrain => terrain.Element?.ElementType != elementType))
            {
                return ToolResult.Fail($"Default deck terrain for {elementName} must contain {expectedTerrainCount} cards using the {elementName} element.");
            }
        }

        return ToolResult.Ok("Validated default deck composition.");
    }
}
