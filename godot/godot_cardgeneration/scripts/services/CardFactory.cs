using System;
using System.Collections.Generic;
using System.Linq;
using CardGeneration.Resources;
using CardGeneration.Resources.Enums;

namespace CardGeneration.Services;

public static class CardFactory
{
    public static CardResource CreateCard(CardType cardType, IReadOnlyList<ElementResource> elements)
    {
        var element = FindElement(elements, ElementType.Neutral);
        return cardType switch
        {
            CardType.Terrain => new TerrainCardResource
            {
                Element = element,
                ProducedResources = [Amount(element, 1)]
            },
            CardType.Monster => new MonsterCardResource
            {
                Element = element,
                Tier = 1,
                Requirements = [Amount(element, 1)],
                BasePower = 1
            },
            _ => throw new ArgumentOutOfRangeException(nameof(cardType), cardType, "Only monster and terrain cards are supported.")
        };
    }

    private static ElementResource? FindElement(IReadOnlyList<ElementResource> elements, ElementType elementType)
    {
        return elements.FirstOrDefault(element => element.ElementType == elementType);
    }

    private static ResourceAmount Amount(ElementResource? element, int amount)
    {
        return new ResourceAmount
        {
            Element = element,
            Amount = amount
        };
    }
}
