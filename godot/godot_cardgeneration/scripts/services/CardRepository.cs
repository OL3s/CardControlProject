using System;
using System.Collections.Generic;
using CardGeneration.App;
using CardGeneration.Resources;

namespace CardGeneration.Services;

public sealed class CardRepository
{
    public const string CardsRootPath = "res://resources/cards";

    public IReadOnlyList<CardResource> LoadAllCards()
    {
        return Array.Empty<CardResource>();
    }

    public CardResource? LoadCardById(string cardId)
    {
        _ = cardId;
        return null;
    }

    public ToolResult SaveCard(CardResource card)
    {
        if (string.IsNullOrWhiteSpace(card.Id))
        {
            return ToolResult.Fail("Card must have an id before it can be saved.");
        }

        return ToolResult.Ok($"SaveCard is not implemented yet for '{card.Id}'.");
    }
}
