using System.Collections.Generic;
using System.IO;
using System.Linq;
using CardGeneration.App;
using CardGeneration.Resources;
using Godot;

namespace CardGeneration.Services;

public sealed class DeckRepository
{
    public const string DecksRootPath = "res://resources/decks";
    public const string UserDecksRootPath = "user://resources/decks";
    public const string UserDefaultDecksRootPath = UserDecksRootPath + "/default";

    public IReadOnlyList<CardDeckResource> LoadAllDecks()
    {
        return LoadDefaultDecks()
            .Concat(LoadGeneratedDefaultDecks())
            .Concat(LoadSavedUserDecks())
            .GroupBy(deck => deck.Id)
            .Select(group => group.Last())
            .OrderBy(deck => deck.Id)
            .ToArray();
    }

    public IReadOnlyList<CardDeckResource> LoadDefaultDecks()
    {
        return ResourceRepository.LoadAll<CardDeckResource>(DecksRootPath, warnIfMissing: false)
            .OrderBy(deck => deck.Id)
            .ToArray();
    }

    public IReadOnlyList<CardDeckResource> LoadUserDecks()
    {
        return LoadGeneratedDefaultDecks()
            .Concat(LoadSavedUserDecks())
            .GroupBy(deck => deck.Id)
            .Select(group => group.Last())
            .OrderBy(deck => deck.Id)
            .ToArray();
    }

    private static IReadOnlyList<CardDeckResource> LoadGeneratedDefaultDecks()
    {
        return ResourceRepository.LoadAll<CardDeckResource>(UserDefaultDecksRootPath, warnIfMissing: false)
            .OrderBy(deck => deck.Id)
            .ToArray();
    }

    private static IReadOnlyList<CardDeckResource> LoadSavedUserDecks()
    {
        return ResourceRepository.LoadAll<CardDeckResource>(UserDecksRootPath, warnIfMissing: false)
            .Where(deck => !IsUnderRoot(deck.ResourcePath, UserDefaultDecksRootPath))
            .OrderBy(deck => deck.Id)
            .ToArray();
    }

    public CardDeckResource? LoadDeckById(string deckId)
    {
        return LoadAllDecks().FirstOrDefault(deck => deck.Id == deckId);
    }

    public ToolResult SaveDeck(CardDeckResource deck)
    {
        if (IsDefaultOnlyDeckId(deck.Id))
        {
            return ToolResult.Fail($"Deck '{deck.Id}' is a read-only default resource. Use Save as new or duplicate it first.");
        }

        return SaveDeckToRoot(deck, UserDecksRootPath, "deck");
    }

    public ToolResult SaveDefaultDeck(CardDeckResource deck)
    {
        return SaveDeckToRoot(deck, UserDefaultDecksRootPath, "default deck");
    }

    private static ToolResult SaveDeckToRoot(CardDeckResource deck, string rootPath, string resourceType)
    {
        if (string.IsNullOrWhiteSpace(deck.Id))
        {
            return ToolResult.Fail("Deck must have an id before it can be saved.");
        }

        DirAccess.MakeDirRecursiveAbsolute(ProjectSettings.GlobalizePath(rootPath));
        var path = $"{rootPath}/{deck.Id}.tres";
        var error = ResourceSaver.Save(deck, path);
        return error == Error.Ok
            ? ToolResult.Ok($"Saved {resourceType} '{deck.Id}' to {path}.")
            : ToolResult.Fail($"Failed to save {resourceType} '{deck.Id}' to {path}: {error}.");
    }

    public ToolResult SaveDeckToExistingResource(CardDeckResource deck, string resourcePath)
    {
        if (string.IsNullOrWhiteSpace(resourcePath))
        {
            return ToolResult.Fail("Deck has no existing resource file. Use Save as new.");
        }

        if (!resourcePath.EndsWith(".tres", System.StringComparison.OrdinalIgnoreCase)
            && !resourcePath.EndsWith(".res", System.StringComparison.OrdinalIgnoreCase))
        {
            return ToolResult.Fail($"Deck resource path must be .tres or .res: {resourcePath}.");
        }

        if (!ResourceFileExists(resourcePath))
        {
            return ToolResult.Fail($"Deck resource file no longer exists, refusing to overwrite: {resourcePath}. Use Save as new.");
        }

        if (IsDefaultResourcePath(resourcePath))
        {
            return ToolResult.Fail($"Deck '{deck.Id}' is a read-only default resource. Use Save as new or duplicate it first.");
        }

        var error = ResourceSaver.Save(deck, resourcePath);
        if (error != Error.Ok && Path.IsPathRooted(resourcePath))
        {
            error = SaveResourceViaTemporaryPath(deck, resourcePath);
        }

        return error == Error.Ok
            ? ToolResult.Ok($"Saved deck '{deck.Id}' to {resourcePath}.")
            : ToolResult.Fail($"Failed to save deck '{deck.Id}' to {resourcePath}: {error}.");
    }

    public int DeleteAllSavedDecks()
    {
        return DeleteResourceFiles(UserDecksRootPath);
    }

    public int DeleteGeneratedDefaultDecks()
    {
        return DeleteResourceFiles(UserDefaultDecksRootPath);
    }

    public ToolResult DeleteDeck(string deckId)
    {
        if (string.IsNullOrWhiteSpace(deckId))
        {
            return ToolResult.Fail("Missing deck id. Use --deck <deck_id>.");
        }

        var deck = LoadSavedUserDecks()
            .Concat(LoadGeneratedDefaultDecks())
            .FirstOrDefault(savedDeck => savedDeck.Id == deckId);
        if (deck is null)
        {
            return LoadDefaultDecks().Any(existingDeck => existingDeck.Id == deckId)
                ? ToolResult.Fail($"Deck '{deckId}' is a packaged default resource and cannot be deleted.")
                : ToolResult.Fail($"Deck '{deckId}' was not found.");
        }

        return DeleteResourceFile(deck.ResourcePath, "deck", deckId);
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
        if (string.IsNullOrWhiteSpace(resourcePath) || IsPackagedResourcePath(resourcePath))
        {
            return ToolResult.Fail($"{resourceType} '{id}' is a packaged default resource and cannot be deleted.");
        }

        var globalPath = ProjectSettings.GlobalizePath(resourcePath);
        if (!File.Exists(globalPath))
        {
            return ToolResult.Fail($"{resourceType} resource file was not found: {resourcePath}.");
        }

        File.Delete(globalPath);
        return ToolResult.Ok($"Deleted {resourceType} '{id}' from {resourcePath}.");
    }

    private static bool ResourceFileExists(string resourcePath)
    {
        if (Godot.FileAccess.FileExists(resourcePath))
        {
            return true;
        }

        var globalPath = Path.IsPathRooted(resourcePath)
            ? resourcePath
            : ProjectSettings.GlobalizePath(resourcePath);
        return File.Exists(globalPath);
    }

    private static Error SaveResourceViaTemporaryPath(Resource resource, string outputPath)
    {
        var temporaryPath = $"user://deck_save_{System.Guid.NewGuid():N}.tres";
        var error = ResourceSaver.Save(resource, temporaryPath);
        if (error != Error.Ok)
        {
            return error;
        }

        var temporaryGlobalPath = ProjectSettings.GlobalizePath(temporaryPath);
        try
        {
            File.Copy(temporaryGlobalPath, outputPath, overwrite: true);
            File.Delete(temporaryGlobalPath);
            return Error.Ok;
        }
        catch
        {
            return Error.FileCantWrite;
        }
    }

    private static bool IsUnderRoot(string resourcePath, string rootPath)
    {
        return !string.IsNullOrWhiteSpace(resourcePath)
            && resourcePath.StartsWith(rootPath + "/", System.StringComparison.OrdinalIgnoreCase);
    }

    private bool IsDefaultOnlyDeckId(string deckId)
    {
        if (string.IsNullOrWhiteSpace(deckId) || LoadSavedUserDecks().Any(deck => deck.Id == deckId))
        {
            return false;
        }

        return LoadDefaultDecks().Concat(LoadGeneratedDefaultDecks()).Any(deck => deck.Id == deckId);
    }

    private static bool IsDefaultResourcePath(string resourcePath)
    {
        return resourcePath.StartsWith("res://", System.StringComparison.OrdinalIgnoreCase)
            || IsUnderRoot(resourcePath, UserDefaultDecksRootPath);
    }

    private static bool IsPackagedResourcePath(string resourcePath)
    {
        return resourcePath.StartsWith("res://", System.StringComparison.OrdinalIgnoreCase);
    }
}
