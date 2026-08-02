using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using CardGeneration.App;
using CardGeneration.Resources;
using CardGeneration.Resources.Enums;

namespace CardGeneration.Services;

public sealed class CardToolService
{
    private const string DefaultContentVersion = "terrain_monster_v1";
    private const string DefaultContentVersionPath = "user://resources/default_content_version.txt";

    private readonly CardRepository _cardRepository;
    private readonly DeckRepository _deckRepository;
    private readonly CardValidator _cardValidator;
    private readonly DeckValidator _deckValidator;
    private readonly CardRenderService _cardRenderService;
    private readonly DeckExportService _deckExportService;
    private readonly SheetExportService _sheetExportService;
    private readonly DiyExportService _diyExportService;
    private readonly ConfigRepository _configRepository;

    public CardToolService()
        : this(CreateDefaultServices())
    {
    }

    private CardToolService(DefaultServices services)
        : this(
            services.CardRepository,
            services.DeckRepository,
            services.CardValidator,
            services.DeckValidator,
            services.CardRenderService,
            services.DeckExportService,
            services.SheetExportService,
            services.DiyExportService,
            services.ConfigRepository)
    {
    }

    public CardToolService(
        CardRepository cardRepository,
        DeckRepository deckRepository,
        CardValidator cardValidator,
        DeckValidator deckValidator,
        CardRenderService cardRenderService,
        DeckExportService deckExportService,
        SheetExportService sheetExportService,
        DiyExportService diyExportService,
        ConfigRepository configRepository)
    {
        _cardRepository = cardRepository;
        _deckRepository = deckRepository;
        _cardValidator = cardValidator;
        _deckValidator = deckValidator;
        _cardRenderService = cardRenderService;
        _deckExportService = deckExportService;
        _sheetExportService = sheetExportService;
        _diyExportService = diyExportService;
        _configRepository = configRepository;
    }

    public CardToolConfigResource LoadConfig()
    {
        return _configRepository.LoadConfig();
    }

    public ToolResult ShowConfig()
    {
        var config = _configRepository.LoadConfig();
        var message = new StringBuilder();
        message.AppendLine("Card tool config:");
        message.AppendLine($"- default_card: {config.DefaultCardId}");
        message.AppendLine($"- default_deck: {config.DefaultDeckId}");
        message.AppendLine($"- output: {config.DefaultOutputPath}");
        message.AppendLine($"- format: {config.DefaultFormat}");
        message.AppendLine($"- paper: {config.DefaultPaper}");
        message.AppendLine($"- dpi: {config.DefaultDpi}");
        message.AppendLine($"- back_mirror: {config.DefaultBackMirror}");
        message.AppendLine($"- deck_layout: {config.DefaultDeckLayout}");
        message.AppendLine($"- grid_columns: {config.DefaultGridColumns}");
        message.AppendLine($"- spacing: {config.DefaultSpacing}");
        return ToolResult.Ok(message.ToString().TrimEnd());
    }

    public ToolResult SetConfig(CardToolConfigUpdate update)
    {
        var config = _configRepository.LoadConfig();

        if (!string.IsNullOrWhiteSpace(update.DefaultCardId))
        {
            config.DefaultCardId = update.DefaultCardId;
        }

        if (!string.IsNullOrWhiteSpace(update.DefaultDeckId))
        {
            config.DefaultDeckId = update.DefaultDeckId;
        }

        if (!string.IsNullOrWhiteSpace(update.DefaultOutputPath))
        {
            config.DefaultOutputPath = update.DefaultOutputPath;
        }

        if (!string.IsNullOrWhiteSpace(update.DefaultFormat))
        {
            if (update.DefaultFormat != "png")
            {
                return ToolResult.Fail($"Format '{update.DefaultFormat}' is not supported. Use png.");
            }

            config.DefaultFormat = update.DefaultFormat;
        }

        if (!string.IsNullOrWhiteSpace(update.DefaultPaper))
        {
            if (update.DefaultPaper != "a4" && update.DefaultPaper != "a3")
            {
                return ToolResult.Fail($"Paper '{update.DefaultPaper}' is not supported. Use a4 or a3.");
            }

            config.DefaultPaper = update.DefaultPaper;
        }

        if (update.DefaultDpi.HasValue)
        {
            if (!IsSupportedDpi(update.DefaultDpi.Value))
            {
                return ToolResult.Fail($"DPI '{update.DefaultDpi.Value}' is not supported. Use one of: 150, 300, 600, 1200.");
            }

            config.DefaultDpi = update.DefaultDpi.Value;
        }

        if (!string.IsNullOrWhiteSpace(update.DefaultBackMirror))
        {
            if (!IsSupportedBackMirror(update.DefaultBackMirror))
            {
                return ToolResult.Fail($"Back mirror '{update.DefaultBackMirror}' is not supported. Use none, width, height, or both.");
            }

            config.DefaultBackMirror = update.DefaultBackMirror;
        }

        if (!string.IsNullOrWhiteSpace(update.DefaultDeckLayout))
        {
            if (update.DefaultDeckLayout != "individual" && update.DefaultDeckLayout != "grid" && update.DefaultDeckLayout != "strip")
            {
                return ToolResult.Fail($"Deck layout '{update.DefaultDeckLayout}' is not supported. Use individual, grid, or strip.");
            }

            config.DefaultDeckLayout = update.DefaultDeckLayout;
        }

        if (update.DefaultGridColumns.HasValue)
        {
            if (update.DefaultGridColumns.Value < 0)
            {
                return ToolResult.Fail("Grid columns must be 0 or higher.");
            }

            config.DefaultGridColumns = update.DefaultGridColumns.Value;
        }

        if (update.DefaultSpacing.HasValue)
        {
            if (update.DefaultSpacing.Value < 0)
            {
                return ToolResult.Fail("Spacing must be 0 or higher.");
            }

            config.DefaultSpacing = update.DefaultSpacing.Value;
        }

        return _configRepository.SaveConfig(config);
    }

    public ToolResult ResetConfig()
    {
        return _configRepository.ResetConfig();
    }

    public ToolResult ResetSavedContent()
    {
        var deletedCards = _cardRepository.DeleteAllSavedCards();
        var deletedDecks = _deckRepository.DeleteAllSavedDecks();
        var ensureResult = EnsureDefaultResources();
        if (!ensureResult.Success)
        {
            return ensureResult;
        }

        return ToolResult.Ok($"Reset saved content. Deleted {deletedCards} card resource(s) and {deletedDecks} deck resource(s). {ensureResult.Message}");
    }

    public ToolResult EnsureDefaultResources()
    {
        ResetGeneratedDefaultsWhenOutdated();

        var existingCards = LoadAllCards()
            .Where(card => !string.IsNullOrWhiteSpace(card.Id))
            .GroupBy(card => card.Id)
            .ToDictionary(group => group.Key, group => group.First());

        var defaultDeck = DefaultDeckFactory.CreateDefault52CardDeck(LoadAllElements(), existingCards);
        var defaultCards = defaultDeck.Entries
            .Select(entry => entry.Card)
            .OfType<CardResource>()
            .Where(card => !string.IsNullOrWhiteSpace(card.Id))
            .GroupBy(card => card.Id)
            .Select(group => group.First())
            .ToArray();

        var savedCardCount = 0;
        foreach (var card in defaultCards)
        {
            if (existingCards.ContainsKey(card.Id))
            {
                continue;
            }

            var cardSaveResult = _cardRepository.SaveDefaultCard(card);
            if (!cardSaveResult.Success)
            {
                return cardSaveResult;
            }

            existingCards[card.Id] = card;
            savedCardCount++;
        }

        var hasDefaultDeck = _deckRepository.LoadAllDecks()
            .Any(deck => deck.Id == DefaultDeckFactory.Default52CardDeckId);
        var savedDeck = false;
        if (!hasDefaultDeck)
        {
            var deckSaveResult = _deckRepository.SaveDefaultDeck(defaultDeck);
            if (!deckSaveResult.Success)
            {
                return deckSaveResult;
            }

            savedDeck = true;
        }

        if (savedCardCount == 0 && !savedDeck)
        {
            SaveDefaultContentVersion();
            return ToolResult.Ok("Default resources are available.");
        }

        var generated = new List<string>();
        if (savedCardCount > 0)
        {
            generated.Add($"{savedCardCount} card resource(s)");
        }

        if (savedDeck)
        {
            generated.Add($"deck '{defaultDeck.Id}'");
        }

        SaveDefaultContentVersion();
        return ToolResult.Ok($"Generated missing default resources: {string.Join(", ", generated)}.");
    }

    private void ResetGeneratedDefaultsWhenOutdated()
    {
        var versionPath = Godot.ProjectSettings.GlobalizePath(DefaultContentVersionPath);
        if (File.Exists(versionPath) && File.ReadAllText(versionPath).Trim() == DefaultContentVersion)
        {
            return;
        }

        _cardRepository.DeleteGeneratedDefaultCards();
        _deckRepository.DeleteGeneratedDefaultDecks();
    }

    private static void SaveDefaultContentVersion()
    {
        var versionPath = Godot.ProjectSettings.GlobalizePath(DefaultContentVersionPath);
        Directory.CreateDirectory(Path.GetDirectoryName(versionPath)!);
        File.WriteAllText(versionPath, DefaultContentVersion);
    }

    private static bool IsSupportedDpi(int dpi)
    {
        return dpi is 150 or 300 or 600 or 1200;
    }

    private static bool IsSupportedBackMirror(string backMirror)
    {
        return backMirror is "none" or "width" or "height" or "both";
    }

    public ToolResult ListCards()
    {
        var cards = LoadAllCards();
        return ToolResult.Ok(cards.Count == 0
            ? "Found 0 saved cards."
            : $"Found {cards.Count} saved cards:\n{string.Join("\n", cards.Select(card => $"- {card.Id}"))}");
    }

    public IReadOnlyList<ElementResource> LoadAllElements()
    {
        return ResourceRepository.LoadAll<ElementResource>("res://resources/elements")
            .OrderBy(element => element.DisplayName)
            .ThenBy(element => element.ElementType)
            .ToArray();
    }

    public IReadOnlyList<CardResource> LoadAllCards()
    {
        return _cardRepository.LoadAllCards();
    }

    public CardResource CreateCard(CardGeneration.Resources.Enums.CardType cardType)
    {
        return CardFactory.CreateCard(cardType, LoadAllElements());
    }

    public CardResource? LoadCardById(string cardId)
    {
        return _cardRepository.LoadCardById(cardId);
    }

    public ToolResult SaveCard(CardResource card)
    {
        var validationResult = _cardValidator.Validate([card]);
        if (!validationResult.Success)
        {
            return validationResult;
        }

        return _cardRepository.SaveCard(card);
    }

    public ToolResult DeleteCard(string? cardId)
    {
        return _cardRepository.DeleteCard(cardId ?? string.Empty);
    }

    public ToolResult DuplicateCard(string? cardId, string? newCardId = null)
    {
        if (string.IsNullOrWhiteSpace(cardId))
        {
            return ToolResult.Fail("Missing card id. Use --card <card_id>.");
        }

        var card = LoadCardById(cardId);
        if (card is null)
        {
            return ToolResult.Fail($"Card '{cardId}' was not found.");
        }

        var copy = CloneCard(card);
        copy.Id = string.IsNullOrWhiteSpace(newCardId)
            ? CreateUniqueCardId(card.Id)
            : MakeResourceId(newCardId, card.Id);
        return _cardRepository.SaveCard(copy);
    }

    public ToolResult ImportCardResource(string? filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return ToolResult.Fail("Missing input path. Use --input <path.tres>.");
        }

        var card = LoadExternalResource<CardResource>(filePath);
        return card is null ? ToolResult.Fail($"Could not import card resource from {filePath}.") : SaveCard(card);
    }

    public ToolResult ExportCardResource(CardResource card, string filePath)
    {
        var validationResult = _cardValidator.Validate([card]);
        if (!validationResult.Success)
        {
            return validationResult;
        }

        return SaveResourceFile(card, filePath, card.Id, "card");
    }

    public ToolResult ListDecks()
    {
        var decks = LoadAllDecks();
        return ToolResult.Ok(decks.Count == 0
            ? "Found 0 saved decks."
            : $"Found {decks.Count} saved decks:\n{string.Join("\n", decks.Select(deck => $"- {deck.Id}"))}");
    }

    public IReadOnlyList<CardDeckResource> LoadAllDecks()
    {
        return _deckRepository.LoadAllDecks();
    }

    public CardDeckResource CreateEmptyDeck()
    {
        return DefaultDeckFactory.CreateEmptyDeck();
    }

    public CardDeckResource CreateDefault52CardDeck()
    {
        var existingCards = LoadAllCards()
            .GroupBy(card => card.Id)
            .ToDictionary(group => group.Key, group => group.First());
        return DefaultDeckFactory.CreateDefault52CardDeck(LoadAllElements(), existingCards);
    }

    public CardDeckResource? LoadDeckById(string deckId)
    {
        return _deckRepository.LoadDeckById(deckId);
    }

    public ToolResult SaveDeck(CardDeckResource deck)
    {
        var validationResult = _deckValidator.Validate(deck);
        if (!validationResult.Success)
        {
            return validationResult;
        }

        return _deckRepository.SaveDeck(deck);
    }

    public ToolResult DeleteDeck(string? deckId)
    {
        return _deckRepository.DeleteDeck(deckId ?? string.Empty);
    }

    public ToolResult DuplicateDeck(string? deckId, string? newDeckId = null)
    {
        if (string.IsNullOrWhiteSpace(deckId))
        {
            return ToolResult.Fail("Missing deck id. Use --deck <deck_id>.");
        }

        var deck = LoadDeckById(deckId);
        if (deck is null)
        {
            return ToolResult.Fail($"Deck '{deckId}' was not found.");
        }

        var copy = CloneDeck(deck);
        copy.Id = string.IsNullOrWhiteSpace(newDeckId)
            ? CreateUniqueDeckId(deck.Id)
            : MakeResourceId(newDeckId, deck.Id);
        return _deckRepository.SaveDeck(copy);
    }

    public ToolResult SaveDeckToExistingResource(CardDeckResource deck, string resourcePath)
    {
        var validationResult = _deckValidator.Validate(deck);
        if (!validationResult.Success)
        {
            return validationResult;
        }

        return _deckRepository.SaveDeckToExistingResource(deck, resourcePath);
    }

    public ToolResult ImportDeckResource(string? filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return ToolResult.Fail("Missing input path. Use --input <path.tres>.");
        }

        var deck = LoadExternalResource<CardDeckResource>(filePath);
        return deck is null ? ToolResult.Fail($"Could not import deck resource from {filePath}.") : SaveDeck(deck);
    }

    public ToolResult ExportDeckResource(CardDeckResource deck, string filePath)
    {
        var validationResult = _deckValidator.Validate(deck);
        if (!validationResult.Success)
        {
            return validationResult;
        }

        return SaveResourceFile(deck, filePath, deck.Id, "deck");
    }

    public ToolResult ValidateCards()
    {
        return _cardValidator.Validate(_cardRepository.LoadAllCards());
    }

    public ToolResult ValidateDeck(string? deckId)
    {
        if (string.IsNullOrWhiteSpace(deckId))
        {
            return ToolResult.Fail("Missing deck id. Use --deck <deck_id>.");
        }

        var deck = LoadDeckById(deckId);
        return deck is null
            ? ToolResult.Fail($"Deck '{deckId}' was not found.")
            : _deckValidator.Validate(deck);
    }

    public ToolResult ValidateDecks()
    {
        var decks = LoadAllDecks();
        foreach (var deck in decks)
        {
            var result = _deckValidator.Validate(deck);
            if (!result.Success)
            {
                return result;
            }
        }

        return ToolResult.Ok($"Validated {decks.Count} deck(s).");
    }

    public ToolResult RenderCard(string? cardId, string outputPath)
    {
        if (string.IsNullOrWhiteSpace(cardId))
        {
            return ToolResult.Fail("Missing card id. Use --card <card_id>.");
        }

        var card = _cardRepository.LoadCardById(cardId);
        return card is null
            ? ToolResult.Fail($"Card '{cardId}' was not found.")
            : _cardRenderService.RenderCard(card, outputPath);
    }

    public ToolResult RenderCard(CardResource card, string outputPath, Action<ExportProgress>? progress = null)
    {
        return _cardRenderService.RenderCard(card, outputPath, card.Id, progress);
    }

    public Godot.Image RenderCardPreview(CardResource card, Action<ExportProgress>? progress = null)
    {
        progress?.Invoke(new ExportProgress(0, 1, $"Rendering preview: {card.Id}"));
        var image = CardGeneration.Rendering.CardImageRenderer.Render(card, new Godot.Vector2I(300, 420));
        progress?.Invoke(new ExportProgress(1, 1, $"Rendered preview: {card.Id}"));
        return image;
    }

    public ToolResult ExportDeck(string? deckId, string outputPath, string format, string layout, int columns, int spacing, ImageBackMode backMode = ImageBackMode.None)
    {
        if (string.IsNullOrWhiteSpace(deckId))
        {
            return ToolResult.Fail("Missing deck id. Use --deck <deck_id>.");
        }

        var deck = LoadDeckById(deckId);
        return deck is null
            ? ToolResult.Fail($"Deck '{deckId}' was not found.")
            : _deckExportService.ExportDeck(deck, outputPath, format, layout, columns, spacing, backMode: backMode);
    }

    public ToolResult ExportDeck(CardDeckResource deck, string outputPath, string format, string layout, int columns, int spacing, Action<ExportProgress>? progress = null, ImageBackMode backMode = ImageBackMode.None)
    {
        return _deckExportService.ExportDeck(deck, outputPath, format, layout, columns, spacing, progress, backMode);
    }

    public IReadOnlyList<ImagePreviewItem>? RenderDeckImagePreviews(CardDeckResource deck, string layout, int columns, int spacing, out string errorMessage, Action<ExportProgress>? progress = null, ImageBackMode backMode = ImageBackMode.None)
    {
        return _deckExportService.RenderPreviews(deck, layout, columns, spacing, out errorMessage, progress, backMode);
    }

    public ToolResult ExportSheet(string? deckId, string outputPath, string paper, int dpi, string backMirror = "none", bool includeMeasurementGuide = false, bool easyPrintBacks = false)
    {
        if (string.IsNullOrWhiteSpace(deckId))
        {
            return ToolResult.Fail("Missing deck id. Use --deck <deck_id>.");
        }

        var deck = LoadDeckById(deckId);
        return deck is null
            ? ToolResult.Fail($"Deck '{deckId}' was not found.")
            : _sheetExportService.ExportSheet(deck, outputPath, paper, dpi, backMirror, includeMeasurementGuide, easyPrintBacks: easyPrintBacks);
    }

    public ToolResult ExportSheet(CardDeckResource deck, string outputPath, string paper, int dpi, string backMirror = "none", bool includeMeasurementGuide = false, Action<ExportProgress>? progress = null, bool easyPrintBacks = false)
    {
        return _sheetExportService.ExportSheet(deck, outputPath, paper, dpi, backMirror, includeMeasurementGuide, progress, easyPrintBacks);
    }

    public IReadOnlyList<SheetPreviewPage>? RenderSheetPreviews(CardDeckResource deck, string paper, int dpi, string backMirror, bool includeMeasurementGuide, bool easyPrintBacks, out string errorMessage, Action<ExportProgress>? progress = null)
    {
        return _sheetExportService.RenderSheetPreviews(deck, paper, dpi, backMirror, includeMeasurementGuide, easyPrintBacks, out errorMessage, progress);
    }

    public ToolResult ExportDiy(string? deckId, string outputPath, int dpi, string backMirror = "none", bool includeMeasurementGuide = false)
    {
        if (string.IsNullOrWhiteSpace(deckId))
        {
            return ToolResult.Fail("Missing deck id. Use --deck <deck_id>.");
        }

        var deck = LoadDeckById(deckId);
        return deck is null
            ? ToolResult.Fail($"Deck '{deckId}' was not found.")
            : _diyExportService.ExportDiy(deck, outputPath, dpi, backMirror, includeMeasurementGuide);
    }

    public ToolResult ExportDiy(CardDeckResource deck, string outputPath, int dpi, string backMirror = "none", bool includeMeasurementGuide = false, Action<ExportProgress>? progress = null)
    {
        return _diyExportService.ExportDiy(deck, outputPath, dpi, backMirror, includeMeasurementGuide, progress);
    }

    public ToolResult ExportShowcase(string? deckId, string outputPath, string format)
    {
        if (string.IsNullOrWhiteSpace(deckId))
        {
            return ToolResult.Fail("Missing deck id. Use --deck <deck_id>.");
        }

        var deck = LoadDeckById(deckId);
        return deck is null
            ? ToolResult.Fail($"Deck '{deckId}' was not found.")
            : _deckExportService.ExportShowcase(deck, outputPath, format);
    }

    public ToolResult ExportShowcase(CardDeckResource deck, string outputPath, string format, Action<ExportProgress>? progress = null)
    {
        return _deckExportService.ExportShowcase(deck, outputPath, format, progress);
    }

    private static DefaultServices CreateDefaultServices()
    {
        var cardRepository = new CardRepository();
        var deckRepository = new DeckRepository();
        var cardValidator = new CardValidator();
        var deckValidator = new DeckValidator();
        var cardRenderService = new CardRenderService();
        var deckExportService = new DeckExportService(cardRenderService);
        var sheetExportService = new SheetExportService();
        var diyExportService = new DiyExportService(sheetExportService);
        var configRepository = new ConfigRepository();

        return new DefaultServices(
            cardRepository,
            deckRepository,
            cardValidator,
            deckValidator,
            cardRenderService,
            deckExportService,
            sheetExportService,
            diyExportService,
            configRepository);
    }

    private string CreateUniqueCardId(string sourceId)
    {
        var baseId = MakeResourceId(sourceId, "card");
        var existingIds = LoadAllCards()
            .Select(card => card.Id)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        return CreateUniqueCopyId(baseId, existingIds);
    }

    private string CreateUniqueDeckId(string sourceId)
    {
        var baseId = MakeResourceId(sourceId, "deck");
        var existingIds = LoadAllDecks()
            .Select(deck => deck.Id)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        return CreateUniqueCopyId(baseId, existingIds);
    }

    private static string CreateUniqueCopyId(string baseId, HashSet<string> existingIds)
    {
        for (var index = 1; index < 1000; index++)
        {
            var candidate = $"{baseId}_copy_{index}";
            if (!existingIds.Contains(candidate))
            {
                return candidate;
            }
        }

        return $"{baseId}_copy_{DateTime.Now:yyyyMMddHHmmss}";
    }

    private static string MakeResourceId(string value, string fallback)
    {
        var builder = new StringBuilder();
        foreach (var character in value.Trim().ToLowerInvariant())
        {
            if (char.IsLetterOrDigit(character))
            {
                builder.Append(character);
            }
            else if (character is '_' or '-' or ' ')
            {
                builder.Append('_');
            }
        }

        var id = builder.ToString().Trim('_');
        while (id.Contains("__", StringComparison.Ordinal))
        {
            id = id.Replace("__", "_", StringComparison.Ordinal);
        }

        return string.IsNullOrWhiteSpace(id) ? fallback : id;
    }

    private static CardResource CloneCard(CardResource source)
    {
        var clone = CreateCardForType(source.CardType);
        clone.Id = source.Id;
        clone.CardType = source.CardType;
        clone.CardImageTexture = source.CardImageTexture;
        clone.CardImageSourcePath = source.CardImageSourcePath;
        clone.BackImageTexture = source.BackImageTexture;

        if (source is MonsterCardResource sourceMonster && clone is MonsterCardResource cloneMonster)
        {
            cloneMonster.Requirements = sourceMonster.Requirements;
            cloneMonster.BasePower = sourceMonster.BasePower;
            cloneMonster.PowerBonuses = sourceMonster.PowerBonuses;
            cloneMonster.Effect = sourceMonster.Effect;
        }
        else if (source is TerrainCardResource sourceTerrain && clone is TerrainCardResource cloneTerrain)
        {
            cloneTerrain.ProducedResources = sourceTerrain.ProducedResources;
        }

        return clone;
    }

    private static CardDeckResource CloneDeck(CardDeckResource source)
    {
        return new CardDeckResource
        {
            Id = source.Id,
            MonsterBackImageTexture = source.MonsterBackImageTexture,
            TerrainBackImageTexture = source.TerrainBackImageTexture,
            Entries = (source.Entries ?? Array.Empty<CardDeckEntryResource>())
                .Select(entry => new CardDeckEntryResource
                {
                    Card = entry.Card,
                    Count = entry.Count
                })
                .ToArray()
        };
    }

    private static CardResource CreateCardForType(CardType cardType)
    {
        return cardType switch
        {
            CardType.Monster => new MonsterCardResource(),
            CardType.Terrain => new TerrainCardResource(),
            _ => throw new ArgumentOutOfRangeException(nameof(cardType), cardType, "Only monster and terrain cards are supported.")
        };
    }

    private sealed record DefaultServices(
        CardRepository CardRepository,
        DeckRepository DeckRepository,
        CardValidator CardValidator,
        DeckValidator DeckValidator,
        CardRenderService CardRenderService,
        DeckExportService DeckExportService,
        SheetExportService SheetExportService,
        DiyExportService DiyExportService,
            ConfigRepository ConfigRepository);

    private static T? LoadExternalResource<T>(string filePath) where T : Godot.Resource
    {
        foreach (var candidate in GetResourceLoadCandidates(filePath))
        {
            var resource = Godot.ResourceLoader.Load<T>(candidate);
            if (resource is not null)
            {
                return resource;
            }
        }

        return null;
    }

    private static string[] GetResourceLoadCandidates(string filePath)
    {
        var globalPath = ProjectPaths.ToGlobalPath(filePath);
        var localizedPath = Godot.ProjectSettings.LocalizePath(globalPath);
        return new[] { filePath, localizedPath, globalPath }
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Distinct()
            .ToArray();
    }

    private static ToolResult SaveResourceFile(Godot.Resource resource, string filePath, string id, string resourceType)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return ToolResult.Fail($"Cannot export {resourceType} resource without an id.");
        }

        var outputPath = ProjectPaths.ToGlobalPath(filePath);
        if (!Path.GetExtension(outputPath).Equals(".tres", StringComparison.OrdinalIgnoreCase))
        {
            outputPath = Path.Combine(outputPath, $"{SanitizeFileName(id)}.tres");
        }

        Directory.CreateDirectory(Path.GetDirectoryName(outputPath) ?? ProjectPaths.ToGlobalPath("output"));
        var error = Godot.ResourceSaver.Save(resource, outputPath);
        if (error != Godot.Error.Ok && Path.IsPathRooted(outputPath))
        {
            error = SaveResourceViaTemporaryPath(resource, outputPath);
        }

        return error == Godot.Error.Ok
            ? ToolResult.Ok($"Exported {resourceType} resource '{id}' to {outputPath}.")
            : ToolResult.Fail($"Failed to export {resourceType} resource '{id}' to {outputPath}: {error}.");
    }

    private static Godot.Error SaveResourceViaTemporaryPath(Godot.Resource resource, string outputPath)
    {
        var temporaryPath = $"user://resource_export_{Guid.NewGuid():N}.tres";
        var error = Godot.ResourceSaver.Save(resource, temporaryPath);
        if (error != Godot.Error.Ok)
        {
            return error;
        }

        var temporaryGlobalPath = Godot.ProjectSettings.GlobalizePath(temporaryPath);
        try
        {
            File.Copy(temporaryGlobalPath, outputPath, overwrite: true);
            File.Delete(temporaryGlobalPath);
            return Godot.Error.Ok;
        }
        catch
        {
            return Godot.Error.FileCantWrite;
        }
    }

    private static string SanitizeFileName(string fileName)
    {
        foreach (var invalidChar in Path.GetInvalidFileNameChars())
        {
            fileName = fileName.Replace(invalidChar, '_');
        }

        return string.IsNullOrWhiteSpace(fileName) ? "resource" : fileName;
    }
}
