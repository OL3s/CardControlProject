using System.Collections.Generic;
using System.Linq;
using CardGeneration.App;
using CardGeneration.Resources;
using Godot;

namespace CardGeneration.Services;

public sealed class DeckRepository
{
    public const string DecksRootPath = "res://resources/decks";

    public IReadOnlyList<CardDeckResource> LoadAllDecks()
    {
        return ResourceRepository.LoadAll<CardDeckResource>(DecksRootPath)
            .OrderBy(deck => deck.Id)
            .ToArray();
    }

    public CardDeckResource? LoadDeckById(string deckId)
    {
        return LoadAllDecks().FirstOrDefault(deck => deck.Id == deckId);
    }

    public ToolResult SaveDeck(CardDeckResource deck)
    {
        if (string.IsNullOrWhiteSpace(deck.Id))
        {
            return ToolResult.Fail("Deck must have an id before it can be saved.");
        }

        var path = $"{DecksRootPath}/{deck.Id}.tres";
        var error = ResourceSaver.Save(deck, path);
        return error == Error.Ok
            ? ToolResult.Ok($"Saved deck '{deck.Id}' to {path}.")
            : ToolResult.Fail($"Failed to save deck '{deck.Id}' to {path}: {error}.");
    }
}
