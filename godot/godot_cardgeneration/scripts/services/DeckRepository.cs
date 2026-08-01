using System;
using System.Collections.Generic;
using CardGeneration.App;
using CardGeneration.Resources;

namespace CardGeneration.Services;

public sealed class DeckRepository
{
    public const string DecksRootPath = "res://resources/decks";

    public IReadOnlyList<CardDeckResource> LoadAllDecks()
    {
        return Array.Empty<CardDeckResource>();
    }

    public CardDeckResource? LoadDeckById(string deckId)
    {
        _ = deckId;
        return null;
    }

    public ToolResult SaveDeck(CardDeckResource deck)
    {
        if (string.IsNullOrWhiteSpace(deck.Id))
        {
            return ToolResult.Fail("Deck must have an id before it can be saved.");
        }

        return ToolResult.Ok($"SaveDeck is not implemented yet for '{deck.Id}'.");
    }
}
