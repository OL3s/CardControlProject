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
                ProducedResources = [Amount(element, 1)]
            },
            CardType.King => new KingCardResource
            {
                ElementFocus = element,
                Health = 6,
                QuestText = "Control 6 terrain."
            },
            _ => new MonsterCardResource
            {
                Requirements = [Amount(element, 1)],
                BasePower = 1
            }
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
