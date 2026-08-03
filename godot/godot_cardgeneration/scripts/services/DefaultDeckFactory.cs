using System;
using System.Collections.Generic;
using System.Linq;
using CardGeneration.Resources;
using CardGeneration.Resources.Enums;
using Godot;

namespace CardGeneration.Services;

public static class DefaultDeckFactory
{
    public const string Default52CardDeckId = "default_deck";
    private const string MonsterArtworkDirectory = "res://assets/artwork/monsters";
    private const string TerrainArtworkDirectory = "res://assets/artwork/terrain";
    private const string ElementIconDirectory = "res://assets/icons/elements";
    private const string PowerIconPath = "res://assets/icons/symbols/power.svg";
    private const string AnyElementIconPath = "res://assets/icons/elements/any.svg";

    public static CardDeckResource CreateEmptyDeck()
    {
        return new CardDeckResource
        {
            Id = string.Empty,
            MonsterBackImageScaleMode = CardImageScaleMode.Cover,
            TerrainBackImageScaleMode = CardImageScaleMode.Cover
        };
    }

    public static CardDeckResource CreateDefault52CardDeck(IReadOnlyList<ElementResource> elements, IReadOnlyDictionary<string, CardResource>? existingCards = null)
    {
        var elementMap = BuildElementMap(elements);
        var cards = CreateDefaultCards(elementMap, existingCards);

        return new CardDeckResource
        {
            Id = Default52CardDeckId,
            MonsterBackImageScaleMode = CardImageScaleMode.Cover,
            TerrainBackImageScaleMode = CardImageScaleMode.Cover,
            NeutralElementIconTexture = LoadRequiredTexture($"{ElementIconDirectory}/neutral.svg"),
            GrassElementIconTexture = LoadRequiredTexture($"{ElementIconDirectory}/grass.svg"),
            FlameElementIconTexture = LoadRequiredTexture($"{ElementIconDirectory}/flame.svg"),
            WaterElementIconTexture = LoadRequiredTexture($"{ElementIconDirectory}/water.svg"),
            AnyElementIconTexture = LoadRequiredTexture(AnyElementIconPath),
            PowerIconTexture = LoadRequiredTexture(PowerIconPath),
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

        AddTerrain(cards, elements, existingCards);
        AddMonsters(cards, elements, existingCards);

        return cards;
    }

    private static void AddTerrain(List<CardResource> cards, IReadOnlyDictionary<ElementType, ElementResource> elements, IReadOnlyDictionary<string, CardResource>? existingCards)
    {
        cards.Add(UseExisting(existingCards, "terrain_neutral_1_a", () => Terrain(elements, "terrain_neutral_1_a", ElementType.Neutral, 1, 0, 0, 0)));
        cards.Add(UseExisting(existingCards, "terrain_neutral_1_b", () => Terrain(elements, "terrain_neutral_1_b", ElementType.Neutral, 1, 1, 0, 0)));
        cards.Add(UseExisting(existingCards, "terrain_neutral_1_c", () => Terrain(elements, "terrain_neutral_1_c", ElementType.Neutral, 1, 0, 1, 0)));
        cards.Add(UseExisting(existingCards, "terrain_neutral_1_d", () => Terrain(elements, "terrain_neutral_1_d", ElementType.Neutral, 1, 0, 0, 1)));
        cards.Add(UseExisting(existingCards, "terrain_neutral_1_e", () => Terrain(elements, "terrain_neutral_1_e", ElementType.Neutral, 2, 1, 0, 0)));
        cards.Add(UseExisting(existingCards, "terrain_neutral_2_a", () => Terrain(elements, "terrain_neutral_2_a", ElementType.Neutral, 2, 0, 1, 0)));
        cards.Add(UseExisting(existingCards, "terrain_neutral_2_b", () => Terrain(elements, "terrain_neutral_2_b", ElementType.Neutral, 2, 0, 0, 1)));
        cards.Add(UseExisting(existingCards, "terrain_neutral_2_c", () => Terrain(elements, "terrain_neutral_2_c", ElementType.Neutral, 3, 1, 1, 1)));
        cards.Add(UseExisting(existingCards, "terrain_grass_1_a", () => Terrain(elements, "terrain_grass_1_a", ElementType.Grass, 0, 1, 0, 0)));
        cards.Add(UseExisting(existingCards, "terrain_grass_1_b", () => Terrain(elements, "terrain_grass_1_b", ElementType.Grass, 1, 1, 0, 0)));
        cards.Add(UseExisting(existingCards, "terrain_grass_1_c", () => Terrain(elements, "terrain_grass_1_c", ElementType.Grass, 1, 2, 0, 0)));
        cards.Add(UseExisting(existingCards, "terrain_grass_2_a", () => Terrain(elements, "terrain_grass_2_a", ElementType.Grass, 2, 2, 0, 1)));
        cards.Add(UseExisting(existingCards, "terrain_flame_1_a", () => Terrain(elements, "terrain_flame_1_a", ElementType.Flame, 0, 0, 1, 0)));
        cards.Add(UseExisting(existingCards, "terrain_flame_1_b", () => Terrain(elements, "terrain_flame_1_b", ElementType.Flame, 1, 0, 1, 0)));
        cards.Add(UseExisting(existingCards, "terrain_flame_1_c", () => Terrain(elements, "terrain_flame_1_c", ElementType.Flame, 1, 0, 2, 0)));
        cards.Add(UseExisting(existingCards, "terrain_flame_2_a", () => Terrain(elements, "terrain_flame_2_a", ElementType.Flame, 2, 1, 2, 0)));
        cards.Add(UseExisting(existingCards, "terrain_water_1_a", () => Terrain(elements, "terrain_water_1_a", ElementType.Water, 0, 0, 0, 1)));
        cards.Add(UseExisting(existingCards, "terrain_water_1_b", () => Terrain(elements, "terrain_water_1_b", ElementType.Water, 1, 0, 0, 1)));
        cards.Add(UseExisting(existingCards, "terrain_water_1_c", () => Terrain(elements, "terrain_water_1_c", ElementType.Water, 1, 0, 0, 2)));
        cards.Add(UseExisting(existingCards, "terrain_water_2_a", () => Terrain(elements, "terrain_water_2_a", ElementType.Water, 2, 0, 1, 2)));
    }

    private static void AddMonsters(List<CardResource> cards, IReadOnlyDictionary<ElementType, ElementResource> elements, IReadOnlyDictionary<string, CardResource>? existingCards)
    {
        cards.Add(UseExisting(existingCards, "monster_neutral_1_a", () => Monster(elements, "monster_neutral_1_a", ElementType.Neutral, 1, [(ElementType.Neutral, 1)], 1, [])));
        cards.Add(UseExisting(existingCards, "monster_neutral_1_b", () => Monster(elements, "monster_neutral_1_b", ElementType.Neutral, 1, [(ElementType.Neutral, 2)], 1, [(ElementType.Neutral, 1)])));
        cards.Add(UseExisting(existingCards, "monster_neutral_1_c", () => Monster(elements, "monster_neutral_1_c", ElementType.Neutral, 1, [(ElementType.Neutral, 1)], 1, [(ElementType.Neutral, 1)])));
        cards.Add(UseExisting(existingCards, "monster_neutral_1_d", () => Monster(elements, "monster_neutral_1_d", ElementType.Neutral, 1, [(ElementType.Neutral, 2)], 2, [])));
        cards.Add(UseExisting(existingCards, "monster_neutral_2_a", () => Monster(elements, "monster_neutral_2_a", ElementType.Neutral, 2, [(ElementType.Neutral, 2)], 2, [(ElementType.Neutral, 1)])));
        cards.Add(UseExisting(existingCards, "monster_neutral_2_b", () => Monster(elements, "monster_neutral_2_b", ElementType.Neutral, 2, [(ElementType.Neutral, 2)], 1, [(ElementType.Neutral, 1), (ElementType.Neutral, 2)])));
        cards.Add(UseExisting(existingCards, "monster_neutral_2_c", () => Monster(elements, "monster_neutral_2_c", ElementType.Neutral, 2, [(ElementType.Neutral, 3)], 2, [(ElementType.Neutral, 1)])));
        cards.Add(UseExisting(existingCards, "monster_neutral_3_a", () => Monster(elements, "monster_neutral_3_a", ElementType.Neutral, 3, [(ElementType.Neutral, 3)], 2, [(ElementType.Neutral, 1), (ElementType.Neutral, 2)], "reduce_bond_loss_1", "Reduce received pawn loss by 1.")));
        cards.Add(UseExisting(existingCards, "monster_grass_1_a", () => Monster(elements, "monster_grass_1_a", ElementType.Grass, 1, [(ElementType.Grass, 1)], 1, [])));
        cards.Add(UseExisting(existingCards, "monster_grass_1_b", () => Monster(elements, "monster_grass_1_b", ElementType.Grass, 1, [(ElementType.Neutral, 1), (ElementType.Grass, 1)], 1, [(ElementType.Grass, 1)])));
        cards.Add(UseExisting(existingCards, "monster_grass_1_c", () => Monster(elements, "monster_grass_1_c", ElementType.Grass, 1, [(ElementType.Grass, 2)], 1, [(ElementType.Grass, 1)])));
        cards.Add(UseExisting(existingCards, "monster_grass_1_d", () => Monster(elements, "monster_grass_1_d", ElementType.Grass, 1, [(ElementType.Neutral, 1), (ElementType.Grass, 1)], 2, [])));
        cards.Add(UseExisting(existingCards, "monster_grass_2_a", () => Monster(elements, "monster_grass_2_a", ElementType.Grass, 2, [(ElementType.Neutral, 1), (ElementType.Grass, 2)], 1, [(ElementType.Grass, 1)])));
        cards.Add(UseExisting(existingCards, "monster_grass_2_b", () => Monster(elements, "monster_grass_2_b", ElementType.Grass, 2, [(ElementType.Neutral, 2), (ElementType.Grass, 1)], 2, [(ElementType.Grass, 2)])));
        cards.Add(UseExisting(existingCards, "monster_grass_2_c", () => Monster(elements, "monster_grass_2_c", ElementType.Grass, 2, [(ElementType.Neutral, 2), (ElementType.Grass, 2)], 2, [(ElementType.Grass, 1)])));
        cards.Add(UseExisting(existingCards, "monster_grass_3_a", () => Monster(elements, "monster_grass_3_a", ElementType.Grass, 3, [(ElementType.Neutral, 3), (ElementType.Grass, 2)], 2, [(ElementType.Grass, 1), (ElementType.Grass, 2)], "reduce_bond_loss_1", "Reduce received pawn loss by 1.")));
        cards.Add(UseExisting(existingCards, "monster_flame_1_a", () => Monster(elements, "monster_flame_1_a", ElementType.Flame, 1, [(ElementType.Flame, 1)], 1, [])));
        cards.Add(UseExisting(existingCards, "monster_flame_1_b", () => Monster(elements, "monster_flame_1_b", ElementType.Flame, 1, [(ElementType.Neutral, 1), (ElementType.Flame, 1)], 1, [(ElementType.Flame, 1)])));
        cards.Add(UseExisting(existingCards, "monster_flame_1_c", () => Monster(elements, "monster_flame_1_c", ElementType.Flame, 1, [(ElementType.Flame, 1), (ElementType.Any, 1)], 1, [(ElementType.Flame, 1)])));
        cards.Add(UseExisting(existingCards, "monster_flame_1_d", () => Monster(elements, "monster_flame_1_d", ElementType.Flame, 1, [(ElementType.Neutral, 1), (ElementType.Flame, 1)], 2, [])));
        cards.Add(UseExisting(existingCards, "monster_flame_2_a", () => Monster(elements, "monster_flame_2_a", ElementType.Flame, 2, [(ElementType.Neutral, 1), (ElementType.Flame, 2)], 1, [(ElementType.Flame, 1)])));
        cards.Add(UseExisting(existingCards, "monster_flame_2_b", () => Monster(elements, "monster_flame_2_b", ElementType.Flame, 2, [(ElementType.Neutral, 2), (ElementType.Flame, 1)], 2, [(ElementType.Flame, 2)])));
        cards.Add(UseExisting(existingCards, "monster_flame_2_c", () => Monster(elements, "monster_flame_2_c", ElementType.Flame, 2, [(ElementType.Neutral, 2), (ElementType.Flame, 2)], 2, [(ElementType.Flame, 1)])));
        cards.Add(UseExisting(existingCards, "monster_flame_3_a", () => Monster(elements, "monster_flame_3_a", ElementType.Flame, 3, [(ElementType.Neutral, 3), (ElementType.Flame, 2)], 2, [(ElementType.Flame, 1), (ElementType.Flame, 2)], "reroll_attack_die", "Reroll one attack die.")));
        cards.Add(UseExisting(existingCards, "monster_water_1_a", () => Monster(elements, "monster_water_1_a", ElementType.Water, 1, [(ElementType.Water, 1)], 1, [])));
        cards.Add(UseExisting(existingCards, "monster_water_1_b", () => Monster(elements, "monster_water_1_b", ElementType.Water, 1, [(ElementType.Neutral, 1), (ElementType.Water, 1)], 1, [(ElementType.Water, 1)])));
        cards.Add(UseExisting(existingCards, "monster_water_1_c", () => Monster(elements, "monster_water_1_c", ElementType.Water, 1, [(ElementType.Water, 1), (ElementType.Any, 1)], 1, [(ElementType.Water, 2)])));
        cards.Add(UseExisting(existingCards, "monster_water_1_d", () => Monster(elements, "monster_water_1_d", ElementType.Water, 1, [(ElementType.Neutral, 1), (ElementType.Water, 1)], 2, [])));
        cards.Add(UseExisting(existingCards, "monster_water_2_a", () => Monster(elements, "monster_water_2_a", ElementType.Water, 2, [(ElementType.Neutral, 1), (ElementType.Water, 2)], 1, [(ElementType.Water, 1)])));
        cards.Add(UseExisting(existingCards, "monster_water_2_b", () => Monster(elements, "monster_water_2_b", ElementType.Water, 2, [(ElementType.Neutral, 2), (ElementType.Water, 1)], 2, [(ElementType.Water, 2)])));
        cards.Add(UseExisting(existingCards, "monster_water_2_c", () => Monster(elements, "monster_water_2_c", ElementType.Water, 2, [(ElementType.Neutral, 2), (ElementType.Water, 2)], 2, [(ElementType.Water, 1)])));
        cards.Add(UseExisting(existingCards, "monster_water_3_a", () => Monster(elements, "monster_water_3_a", ElementType.Water, 3, [(ElementType.Neutral, 3), (ElementType.Water, 2)], 2, [(ElementType.Water, 1), (ElementType.Water, 2)], "reduce_king_damage_1", "Reduce received damage against king health by 1.")));
    }

    private static CardResource UseExisting(IReadOnlyDictionary<string, CardResource>? existingCards, string id, Func<CardResource> createCard)
    {
        var defaultId = CreateDefaultCardId(id);
        if (existingCards is not null && existingCards.TryGetValue(defaultId, out var existingCard))
        {
            return existingCard;
        }

        var card = createCard();
        card.Id = defaultId;
        return card;
    }

    private static string CreateDefaultCardId(string id)
    {
        return id.StartsWith("default_", StringComparison.OrdinalIgnoreCase)
            ? id
            : $"default_{id}";
    }

    private static TerrainCardResource Terrain(IReadOnlyDictionary<ElementType, ElementResource> elements, string id, ElementType elementType, int neutral, int grass, int flame, int water)
    {
        return new TerrainCardResource
        {
            Id = id,
            Element = Element(elements, elementType),
            CardImageSourcePath = $"{TerrainArtworkDirectory}/{id}.png",
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
            Element = Element(elements, elementType),
            CardImageSourcePath = $"{MonsterArtworkDirectory}/{id}.png",
            Tier = tier,
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
            : throw new InvalidOperationException($"Required element resource is missing for {elementType}.");
    }

    private static Texture2D LoadRequiredTexture(string path)
    {
        return ResourceLoader.Load<Texture2D>(path)
            ?? throw new InvalidOperationException($"Required deck icon is missing: {path}");
    }

}
