using System.Collections.Generic;
using System.Linq;
using CardGeneration.App;
using CardGeneration.Resources;
using CardGeneration.Resources.Enums;
using Godot;

namespace CardGeneration.Services;

public sealed class CardRepository
{
    public const string CardsRootPath = "res://resources/cards";

    public IReadOnlyList<CardResource> LoadAllCards()
    {
        return ResourceRepository.LoadAll<CardResource>(CardsRootPath)
            .OrderBy(card => card.Id)
            .ToArray();
    }

    public CardResource? LoadCardById(string cardId)
    {
        return LoadAllCards().FirstOrDefault(card => card.Id == cardId);
    }

    public ToolResult SaveCard(CardResource card)
    {
        if (string.IsNullOrWhiteSpace(card.Id))
        {
            return ToolResult.Fail("Card must have an id before it can be saved.");
        }

        var directoryPath = GetCardTypeDirectory(card);
        DirAccess.MakeDirRecursiveAbsolute(ProjectSettings.GlobalizePath(directoryPath));
        var path = $"{directoryPath}/{card.Id}.tres";
        var error = ResourceSaver.Save(card, path);
        return error == Error.Ok
            ? ToolResult.Ok($"Saved card '{card.Id}' to {path}.")
            : ToolResult.Fail($"Failed to save card '{card.Id}' to {path}: {error}.");
    }

    private static string GetCardTypeDirectory(CardResource card)
    {
        return card.CardType switch
        {
            CardType.Monster => $"{CardsRootPath}/monsters",
            CardType.Terrain => $"{CardsRootPath}/terrain",
            CardType.King => $"{CardsRootPath}/kings",
            _ => CardsRootPath
        };
    }
}
