using System;
using System.Collections.Generic;
using System.Linq;
using CardGeneration.Resources;
using CardGeneration.Resources.Enums;

namespace CardGeneration.Services;

public static class DefaultDeckFactory
{
    public const string Default52CardDeckId = "default_52_card_deck";

    public static CardDeckResource CreateEmptyDeck()
    {
        return new CardDeckResource
        {
            Id = string.Empty
        };
    }

    public static CardDeckResource CreateDefault52CardDeck(IReadOnlyList<ElementResource> elements, IReadOnlyDictionary<string, CardResource>? existingCards = null)
    {
        var elementMap = BuildElementMap(elements);
        var cards = CreateDefaultCards(elementMap, existingCards);

        return new CardDeckResource
        {
            Id = Default52CardDeckId,
            Entries = cards
                .Select(card => new CardDeckEntryResource
                {
                    Card = card,
                    Count = 1
                })
                .ToArray()
        };
    }

    private static IReadOnlyList<CardResource> CreateDefaultCards(IReadOnlyDictionary<ElementType, ElementResource> elements, IReadOnlyDictionary<string, CardResource>? existingCards)
    {
        var cards = new List<CardResource>();

        AddKings(cards, elements, existingCards);
        AddTerrain(cards, elements, existingCards);
        AddMonsters(cards, elements, existingCards);

        return cards;
    }

    private static void AddKings(List<CardResource> cards, IReadOnlyDictionary<ElementType, ElementResource> elements, IReadOnlyDictionary<string, CardResource>? existingCards)
    {
        cards.Add(UseExisting(existingCards, "king_neutral_0_a", () => King(elements, "king_neutral_0_a", ElementType.Neutral, "Control 6 terrain, at least 4 neutral.")));
        cards.Add(UseExisting(existingCards, "king_neutral_0_b", () => King(elements, "king_neutral_0_b", ElementType.Neutral, "Control 6 terrain, at least 3 neutral.")));
        cards.Add(UseExisting(existingCards, "king_grass_0_a", () => King(elements, "king_grass_0_a", ElementType.Grass, "Control 6 terrain, at least 2 grass and 2 neutral.")));
        cards.Add(UseExisting(existingCards, "king_grass_0_b", () => King(elements, "king_grass_0_b", ElementType.Grass, "Control 6 terrain, at least 3 grass and 1 neutral.")));
        cards.Add(UseExisting(existingCards, "king_flame_0_a", () => King(elements, "king_flame_0_a", ElementType.Flame, "Control 6 terrain, at least 2 flame and 2 neutral.")));
        cards.Add(UseExisting(existingCards, "king_flame_0_b", () => King(elements, "king_flame_0_b", ElementType.Flame, "Control 6 terrain, at least 3 flame and 1 neutral.")));
        cards.Add(UseExisting(existingCards, "king_water_0_a", () => King(elements, "king_water_0_a", ElementType.Water, "Control 6 terrain, at least 2 water and 2 neutral.")));
        cards.Add(UseExisting(existingCards, "king_water_0_b", () => King(elements, "king_water_0_b", ElementType.Water, "Control 6 terrain, at least 3 water and 1 neutral.")));
    }

    private static void AddTerrain(List<CardResource> cards, IReadOnlyDictionary<ElementType, ElementResource> elements, IReadOnlyDictionary<string, CardResource>? existingCards)
    {
        cards.Add(UseExisting(existingCards, "terrain_neutral_1_a", () => Terrain(elements, "terrain_neutral_1_a", ElementType.Neutral, 1, 1, 0, 0, 0)));
        cards.Add(UseExisting(existingCards, "terrain_neutral_1_b", () => Terrain(elements, "terrain_neutral_1_b", ElementType.Neutral, 1, 1, 1, 0, 0)));
        cards.Add(UseExisting(existingCards, "terrain_neutral_1_c", () => Terrain(elements, "terrain_neutral_1_c", ElementType.Neutral, 1, 1, 0, 1, 0)));
        cards.Add(UseExisting(existingCards, "terrain_neutral_1_d", () => Terrain(elements, "terrain_neutral_1_d", ElementType.Neutral, 1, 1, 0, 0, 1)));
        cards.Add(UseExisting(existingCards, "terrain_neutral_1_e", () => Terrain(elements, "terrain_neutral_1_e", ElementType.Neutral, 1, 2, 1, 0, 0)));
        cards.Add(UseExisting(existingCards, "terrain_neutral_2_a", () => Terrain(elements, "terrain_neutral_2_a", ElementType.Neutral, 2, 2, 0, 1, 0)));
        cards.Add(UseExisting(existingCards, "terrain_neutral_2_b", () => Terrain(elements, "terrain_neutral_2_b", ElementType.Neutral, 2, 2, 0, 0, 1)));
        cards.Add(UseExisting(existingCards, "terrain_neutral_2_c", () => Terrain(elements, "terrain_neutral_2_c", ElementType.Neutral, 2, 3, 1, 1, 1)));
        cards.Add(UseExisting(existingCards, "terrain_grass_1_a", () => Terrain(elements, "terrain_grass_1_a", ElementType.Grass, 1, 0, 1, 0, 0)));
        cards.Add(UseExisting(existingCards, "terrain_grass_1_b", () => Terrain(elements, "terrain_grass_1_b", ElementType.Grass, 1, 1, 1, 0, 0)));
        cards.Add(UseExisting(existingCards, "terrain_grass_1_c", () => Terrain(elements, "terrain_grass_1_c", ElementType.Grass, 1, 1, 2, 0, 0)));
        cards.Add(UseExisting(existingCards, "terrain_grass_2_a", () => Terrain(elements, "terrain_grass_2_a", ElementType.Grass, 2, 2, 2, 0, 1)));
        cards.Add(UseExisting(existingCards, "terrain_flame_1_a", () => Terrain(elements, "terrain_flame_1_a", ElementType.Flame, 1, 0, 0, 1, 0)));
        cards.Add(UseExisting(existingCards, "terrain_flame_1_b", () => Terrain(elements, "terrain_flame_1_b", ElementType.Flame, 1, 1, 0, 1, 0)));
        cards.Add(UseExisting(existingCards, "terrain_flame_1_c", () => Terrain(elements, "terrain_flame_1_c", ElementType.Flame, 1, 1, 0, 2, 0)));
        cards.Add(UseExisting(existingCards, "terrain_flame_2_a", () => Terrain(elements, "terrain_flame_2_a", ElementType.Flame, 2, 2, 1, 2, 0)));
        cards.Add(UseExisting(existingCards, "terrain_water_1_a", () => Terrain(elements, "terrain_water_1_a", ElementType.Water, 1, 0, 0, 0, 1)));
        cards.Add(UseExisting(existingCards, "terrain_water_1_b", () => Terrain(elements, "terrain_water_1_b", ElementType.Water, 1, 1, 0, 0, 1)));
        cards.Add(UseExisting(existingCards, "terrain_water_1_c", () => Terrain(elements, "terrain_water_1_c", ElementType.Water, 1, 1, 0, 0, 2)));
        cards.Add(UseExisting(existingCards, "terrain_water_2_a", () => Terrain(elements, "terrain_water_2_a", ElementType.Water, 2, 2, 0, 1, 2)));
    }

    private static void AddMonsters(List<CardResource> cards, IReadOnlyDictionary<ElementType, ElementResource> elements, IReadOnlyDictionary<string, CardResource>? existingCards)
    {
        cards.Add(UseExisting(existingCards, "monster_neutral_1_a", () => Monster(elements, "monster_neutral_1_a", ElementType.Neutral, 1, [(ElementType.Neutral, 1)], 1, [])));
        cards.Add(UseExisting(existingCards, "monster_neutral_1_b", () => Monster(elements, "monster_neutral_1_b", ElementType.Neutral, 1, [(ElementType.Neutral, 2)], 1, [(ElementType.Neutral, 3)])));
        cards.Add(UseExisting(existingCards, "monster_neutral_1_c", () => Monster(elements, "monster_neutral_1_c", ElementType.Neutral, 1, [(ElementType.Neutral, 1), (ElementType.Grass, 1)], 1, [(ElementType.Neutral, 2)])));
        cards.Add(UseExisting(existingCards, "monster_neutral_2_a", () => Monster(elements, "monster_neutral_2_a", ElementType.Neutral, 2, [(ElementType.Neutral, 2)], 2, [(ElementType.Neutral, 3)])));
        cards.Add(UseExisting(existingCards, "monster_neutral_2_b", () => Monster(elements, "monster_neutral_2_b", ElementType.Neutral, 2, [(ElementType.Neutral, 2)], 1, [(ElementType.Neutral, 3), (ElementType.Neutral, 4)])));
        cards.Add(UseExisting(existingCards, "monster_neutral_3_a", () => Monster(elements, "monster_neutral_3_a", ElementType.Neutral, 3, [(ElementType.Neutral, 3)], 2, [(ElementType.Neutral, 4), (ElementType.Neutral, 5)], "reduce_bond_loss_1", "Reduce received pawn loss by 1.")));
        cards.Add(UseExisting(existingCards, "monster_grass_1_a", () => Monster(elements, "monster_grass_1_a", ElementType.Grass, 1, [(ElementType.Grass, 1)], 1, [])));
        cards.Add(UseExisting(existingCards, "monster_grass_1_b", () => Monster(elements, "monster_grass_1_b", ElementType.Grass, 1, [(ElementType.Neutral, 1), (ElementType.Grass, 1)], 1, [(ElementType.Grass, 2)])));
        cards.Add(UseExisting(existingCards, "monster_grass_1_c", () => Monster(elements, "monster_grass_1_c", ElementType.Grass, 1, [(ElementType.Grass, 2)], 1, [(ElementType.Grass, 3)])));
        cards.Add(UseExisting(existingCards, "monster_grass_2_a", () => Monster(elements, "monster_grass_2_a", ElementType.Grass, 2, [(ElementType.Neutral, 1), (ElementType.Grass, 2)], 1, [(ElementType.Grass, 2), (ElementType.Grass, 3)])));
        cards.Add(UseExisting(existingCards, "monster_grass_2_b", () => Monster(elements, "monster_grass_2_b", ElementType.Grass, 2, [(ElementType.Neutral, 2), (ElementType.Grass, 1)], 2, [(ElementType.Grass, 3)])));
        cards.Add(UseExisting(existingCards, "monster_grass_3_a", () => Monster(elements, "monster_grass_3_a", ElementType.Grass, 3, [(ElementType.Neutral, 3), (ElementType.Grass, 2)], 2, [(ElementType.Grass, 3), (ElementType.Grass, 4)], "reduce_bond_loss_1", "Reduce received pawn loss by 1.")));
        cards.Add(UseExisting(existingCards, "monster_flame_1_a", () => Monster(elements, "monster_flame_1_a", ElementType.Flame, 1, [(ElementType.Flame, 1)], 1, [])));
        cards.Add(UseExisting(existingCards, "monster_flame_1_b", () => Monster(elements, "monster_flame_1_b", ElementType.Flame, 1, [(ElementType.Neutral, 1), (ElementType.Flame, 1)], 1, [(ElementType.Flame, 2)])));
        cards.Add(UseExisting(existingCards, "monster_flame_1_c", () => Monster(elements, "monster_flame_1_c", ElementType.Flame, 1, [(ElementType.Flame, 2)], 1, [(ElementType.Flame, 3)])));
        cards.Add(UseExisting(existingCards, "monster_flame_2_a", () => Monster(elements, "monster_flame_2_a", ElementType.Flame, 2, [(ElementType.Neutral, 1), (ElementType.Flame, 2)], 1, [(ElementType.Flame, 2), (ElementType.Flame, 3)])));
        cards.Add(UseExisting(existingCards, "monster_flame_2_b", () => Monster(elements, "monster_flame_2_b", ElementType.Flame, 2, [(ElementType.Neutral, 2), (ElementType.Flame, 1)], 2, [(ElementType.Flame, 3)])));
        cards.Add(UseExisting(existingCards, "monster_flame_3_a", () => Monster(elements, "monster_flame_3_a", ElementType.Flame, 3, [(ElementType.Neutral, 3), (ElementType.Flame, 2)], 2, [(ElementType.Flame, 3), (ElementType.Flame, 4)], "reroll_attack_die", "Reroll one attack die.")));
        cards.Add(UseExisting(existingCards, "monster_water_1_a", () => Monster(elements, "monster_water_1_a", ElementType.Water, 1, [(ElementType.Water, 1)], 1, [])));
        cards.Add(UseExisting(existingCards, "monster_water_1_b", () => Monster(elements, "monster_water_1_b", ElementType.Water, 1, [(ElementType.Neutral, 1), (ElementType.Water, 1)], 1, [(ElementType.Water, 2)])));
        cards.Add(UseExisting(existingCards, "monster_water_1_c", () => Monster(elements, "monster_water_1_c", ElementType.Water, 1, [(ElementType.Water, 2)], 1, [(ElementType.Water, 3)])));
        cards.Add(UseExisting(existingCards, "monster_water_2_a", () => Monster(elements, "monster_water_2_a", ElementType.Water, 2, [(ElementType.Neutral, 1), (ElementType.Water, 2)], 1, [(ElementType.Water, 2), (ElementType.Water, 3)])));
        cards.Add(UseExisting(existingCards, "monster_water_2_b", () => Monster(elements, "monster_water_2_b", ElementType.Water, 2, [(ElementType.Neutral, 2), (ElementType.Water, 1)], 2, [(ElementType.Water, 3)])));
        cards.Add(UseExisting(existingCards, "monster_water_3_a", () => Monster(elements, "monster_water_3_a", ElementType.Water, 3, [(ElementType.Neutral, 3), (ElementType.Water, 2)], 2, [(ElementType.Water, 3), (ElementType.Water, 4)], "reduce_king_damage_1", "Reduce received damage against king health by 1.")));
    }

    private static CardResource UseExisting(IReadOnlyDictionary<string, CardResource>? existingCards, string id, Func<CardResource> createCard)
    {
        return existingCards is not null && existingCards.TryGetValue(id, out var existingCard)
            ? existingCard
            : createCard();
    }

    private static KingCardResource King(IReadOnlyDictionary<ElementType, ElementResource> elements, string id, ElementType elementType, string questText)
    {
        return new KingCardResource
        {
            Id = id,
            ElementFocus = Element(elements, elementType),
            InternalTier = 0,
            Health = 6,
            QuestText = questText
        };
    }

    private static TerrainCardResource Terrain(IReadOnlyDictionary<ElementType, ElementResource> elements, string id, ElementType elementType, int tier, int neutral, int grass, int flame, int water)
    {
        return new TerrainCardResource
        {
            Id = id,
            InternalTier = tier,
            ProducedResources = Amounts(elements, (ElementType.Neutral, neutral), (ElementType.Grass, grass), (ElementType.Flame, flame), (ElementType.Water, water))
        };
    }

    private static MonsterCardResource Monster(
        IReadOnlyDictionary<ElementType, ElementResource> elements,
        string id,
        ElementType elementType,
        int tier,
        (ElementType ElementType, int Amount)[] requirements,
        int basePower,
        (ElementType ElementType, int Amount)[] bonuses,
        string effectId = "",
        string effectText = "")
    {
        return new MonsterCardResource
        {
            Id = id,
            InternalTier = tier,
            Requirements = Amounts(elements, requirements),
            BasePower = basePower,
            PowerBonuses = Bonuses(elements, bonuses),
            Effect = string.IsNullOrWhiteSpace(effectText) ? null : new CardEffectResource { EffectId = effectId, RulesText = effectText }
        };
    }

    private static ResourceAmount[] Amounts(IReadOnlyDictionary<ElementType, ElementResource> elements, params (ElementType ElementType, int Amount)[] specs)
    {
        return specs
            .Where(spec => spec.Amount > 0)
            .Select(spec => new ResourceAmount
            {
                Element = Element(elements, spec.ElementType),
                Amount = spec.Amount
            })
            .ToArray();
    }

    private static PowerBonusResource[] Bonuses(IReadOnlyDictionary<ElementType, ElementResource> elements, params (ElementType ElementType, int Amount)[] specs)
    {
        return specs
            .Where(spec => spec.Amount > 0)
            .Select(spec => new PowerBonusResource
            {
                Requirements = Amounts(elements, (spec.ElementType, spec.Amount)),
                PowerGain = 1
            })
            .ToArray();
    }

    private static IReadOnlyDictionary<ElementType, ElementResource> BuildElementMap(IReadOnlyList<ElementResource> elements)
    {
        return elements
            .GroupBy(element => element.ElementType)
            .ToDictionary(group => group.Key, group => group.First());
    }

    private static ElementResource Element(IReadOnlyDictionary<ElementType, ElementResource> elements, ElementType elementType)
    {
        return elements.TryGetValue(elementType, out var element)
            ? element
            : new ElementResource
            {
                ElementType = elementType,
                DisplayName = elementType.ToString()
            };
    }

}
