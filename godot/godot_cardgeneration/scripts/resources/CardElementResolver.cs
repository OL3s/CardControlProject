using System.Collections.Generic;
using System.Linq;
using CardGeneration.Resources.Enums;

namespace CardGeneration.Resources;

public static class CardElementResolver
{
    public static ElementType GetCardElementType(CardResource card)
    {
        return card switch
        {
            MonsterCardResource monster => GetSingleNonNeutralElementType(monster.Requirements) ?? ElementType.Neutral,
            KingCardResource king => king.ElementFocus?.ElementType ?? ElementType.Neutral,
            _ => ElementType.Neutral
        };
    }

    public static ElementType? GetSingleNonNeutralElementType(IEnumerable<ResourceAmount>? amounts)
    {
        var elementTypes = GetPositiveElementTypes(amounts)
            .Where(elementType => elementType != ElementType.Neutral)
            .Distinct()
            .Take(2)
            .ToArray();

        return elementTypes.Length == 1 ? elementTypes[0] : null;
    }

    public static bool HasMultipleNonNeutralElementTypes(IEnumerable<ResourceAmount>? amounts)
    {
        return GetPositiveElementTypes(amounts)
            .Where(elementType => elementType != ElementType.Neutral)
            .Distinct()
            .Take(2)
            .Count() > 1;
    }

    private static IEnumerable<ElementType> GetPositiveElementTypes(IEnumerable<ResourceAmount>? amounts)
    {
        return amounts is null
            ? []
            : amounts
                .Where(amount => amount.Amount > 0 && amount.Element is not null)
                .Select(amount => amount.Element!.ElementType);
    }
}
