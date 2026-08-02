using System.Collections.Generic;
using System.IO;
using System.Linq;
using CardGeneration.App;
using CardGeneration.Resources;
using CardGeneration.Resources.Enums;
using Godot;

namespace CardGeneration.Services;

public sealed class CardRepository
{
    public const string CardsRootPath = "res://resources/cards";
    public const string UserCardsRootPath = "user://resources/cards";
    public const string UserDefaultCardsRootPath = UserCardsRootPath + "/default";

    public IReadOnlyList<CardResource> LoadAllCards()
    {
        return LoadDefaultCards()
            .Concat(LoadGeneratedDefaultCards())
            .Concat(LoadSavedUserCards())
            .GroupBy(card => card.Id)
            .Select(group => group.Last())
            .OrderBy(card => card.Id)
            .ToArray();
    }

    public IReadOnlyList<CardResource> LoadDefaultCards()
    {
        return ResourceRepository.LoadAll<CardResource>(CardsRootPath, warnIfMissing: false)
            .OrderBy(card => card.Id)
            .ToArray();
    }

    public IReadOnlyList<CardResource> LoadUserCards()
    {
        return LoadGeneratedDefaultCards()
            .Concat(LoadSavedUserCards())
            .GroupBy(card => card.Id)
            .Select(group => group.Last())
            .OrderBy(card => card.Id)
            .ToArray();
    }

    private static IReadOnlyList<CardResource> LoadGeneratedDefaultCards()
    {
        return ResourceRepository.LoadAll<CardResource>(UserDefaultCardsRootPath, warnIfMissing: false)
            .OrderBy(card => card.Id)
            .ToArray();
    }

    private static IReadOnlyList<CardResource> LoadSavedUserCards()
    {
        return ResourceRepository.LoadAll<CardResource>(UserCardsRootPath, warnIfMissing: false)
            .Where(card => !IsUnderRoot(card.ResourcePath, UserDefaultCardsRootPath))
            .OrderBy(card => card.Id)
            .ToArray();
    }

    public CardResource? LoadCardById(string cardId)
    {
        return LoadAllCards().FirstOrDefault(card => card.Id == cardId);
    }

    public ToolResult SaveCard(CardResource card)
    {
        if (IsDefaultOnlyCardId(card.Id))
        {
            return ToolResult.Fail($"Card '{card.Id}' is a read-only default resource. Use Save as new or duplicate it first.");
        }

        return SaveCardToRoot(card, UserCardsRootPath, "card");
    }

    public ToolResult SaveDefaultCard(CardResource card)
    {
        return SaveCardToRoot(card, UserDefaultCardsRootPath, "default card");
    }

    private static ToolResult SaveCardToRoot(CardResource card, string rootPath, string resourceType)
    {
        if (string.IsNullOrWhiteSpace(card.Id))
        {
            return ToolResult.Fail("Card must have an id before it can be saved.");
        }

        var directoryPath = GetCardTypeDirectory(card, rootPath);
        DirAccess.MakeDirRecursiveAbsolute(ProjectSettings.GlobalizePath(directoryPath));
        var path = $"{directoryPath}/{card.Id}.tres";
        var error = ResourceSaver.Save(card, path);
        return error == Error.Ok
            ? ToolResult.Ok($"Saved {resourceType} '{card.Id}' to {path}.")
            : ToolResult.Fail($"Failed to save {resourceType} '{card.Id}' to {path}: {error}.");
    }

    public int DeleteAllSavedCards()
    {
        return DeleteResourceFiles(UserCardsRootPath);
    }

    public ToolResult DeleteCard(string cardId)
    {
        if (string.IsNullOrWhiteSpace(cardId))
        {
            return ToolResult.Fail("Missing card id. Use --card <card_id>.");
        }

        var card = LoadSavedUserCards().FirstOrDefault(savedCard => savedCard.Id == cardId);
        if (card is null)
        {
            return LoadAllCards().Any(existingCard => existingCard.Id == cardId)
                ? ToolResult.Fail($"Card '{cardId}' is a read-only default resource. Duplicate it or save as new before deleting.")
                : ToolResult.Fail($"Card '{cardId}' was not found.");
        }

        return DeleteResourceFile(card.ResourcePath, "card", cardId);
    }

    private static int DeleteResourceFiles(string rootPath)
    {
        var globalRoot = ProjectSettings.GlobalizePath(rootPath);
        if (!Directory.Exists(globalRoot))
        {
            return 0;
        }

        var deleted = 0;
        foreach (var filePath in Directory.EnumerateFiles(globalRoot, "*.*", SearchOption.AllDirectories))
        {
            if (!filePath.EndsWith(".tres") && !filePath.EndsWith(".res"))
            {
                continue;
            }

            File.Delete(filePath);
            deleted++;
        }

        return deleted;
    }

    private static ToolResult DeleteResourceFile(string resourcePath, string resourceType, string id)
    {
        if (string.IsNullOrWhiteSpace(resourcePath) || IsDefaultResourcePath(resourcePath))
        {
            return ToolResult.Fail($"{resourceType} '{id}' is a read-only default resource.");
        }

        var globalPath = ProjectSettings.GlobalizePath(resourcePath);
        if (!File.Exists(globalPath))
        {
            return ToolResult.Fail($"{resourceType} resource file was not found: {resourcePath}.");
        }

        File.Delete(globalPath);
        return ToolResult.Ok($"Deleted {resourceType} '{id}' from {resourcePath}.");
    }

    private static string GetCardTypeDirectory(CardResource card, string rootPath)
    {
        return card.CardType switch
        {
            CardType.Monster => $"{rootPath}/monsters",
            CardType.Terrain => $"{rootPath}/terrain",
            CardType.King => $"{rootPath}/kings",
            _ => rootPath
        };
    }

    private static bool IsUnderRoot(string resourcePath, string rootPath)
    {
        return !string.IsNullOrWhiteSpace(resourcePath)
            && resourcePath.StartsWith(rootPath + "/", System.StringComparison.OrdinalIgnoreCase);
    }

    private bool IsDefaultOnlyCardId(string cardId)
    {
        if (string.IsNullOrWhiteSpace(cardId) || LoadSavedUserCards().Any(card => card.Id == cardId))
        {
            return false;
        }

        return LoadDefaultCards().Concat(LoadGeneratedDefaultCards()).Any(card => card.Id == cardId);
    }

    private static bool IsDefaultResourcePath(string resourcePath)
    {
        return resourcePath.StartsWith("res://", System.StringComparison.OrdinalIgnoreCase)
            || IsUnderRoot(resourcePath, UserDefaultCardsRootPath);
    }
}
