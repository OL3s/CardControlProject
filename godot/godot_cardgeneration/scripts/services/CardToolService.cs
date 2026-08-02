using System.Linq;
using System.Text;
using System.Collections.Generic;
using CardGeneration.App;
using CardGeneration.Resources;

namespace CardGeneration.Services;

public sealed class CardToolService
{
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

    private static bool IsSupportedDpi(int dpi)
    {
        return dpi is 150 or 300 or 600 or 1200;
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
        return _cardRepository.SaveCard(card);
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
        var decks = _deckRepository.LoadAllDecks().ToList();
        if (decks.All(deck => deck.Id != DefaultDeckFactory.Default52CardDeckId))
        {
            decks.Insert(0, CreateDefault52CardDeck());
        }

        return decks;
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
        var deck = _deckRepository.LoadDeckById(deckId);
        if (deck is not null)
        {
            return deck;
        }

        return deckId == DefaultDeckFactory.Default52CardDeckId ? CreateDefault52CardDeck() : null;
    }

    public ToolResult SaveDeck(CardDeckResource deck)
    {
        return _deckRepository.SaveDeck(deck);
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

    public ToolResult RenderCard(CardResource card, string outputPath)
    {
        return _cardRenderService.RenderCard(card, outputPath);
    }

    public ToolResult ExportDeck(string? deckId, string outputPath, string format, string layout, int columns, int spacing)
    {
        if (string.IsNullOrWhiteSpace(deckId))
        {
            return ToolResult.Fail("Missing deck id. Use --deck <deck_id>.");
        }

        var deck = LoadDeckById(deckId);
        return deck is null
            ? ToolResult.Fail($"Deck '{deckId}' was not found.")
            : _deckExportService.ExportDeck(deck, outputPath, format, layout, columns, spacing);
    }

    public ToolResult ExportDeck(CardDeckResource deck, string outputPath, string format, string layout, int columns, int spacing)
    {
        return _deckExportService.ExportDeck(deck, outputPath, format, layout, columns, spacing);
    }

    public ToolResult ExportSheet(string? deckId, string outputPath, string paper, int dpi)
    {
        if (string.IsNullOrWhiteSpace(deckId))
        {
            return ToolResult.Fail("Missing deck id. Use --deck <deck_id>.");
        }

        var deck = LoadDeckById(deckId);
        return deck is null
            ? ToolResult.Fail($"Deck '{deckId}' was not found.")
            : _sheetExportService.ExportSheet(deck, outputPath, paper, dpi);
    }

    public ToolResult ExportSheet(CardDeckResource deck, string outputPath, string paper, int dpi)
    {
        return _sheetExportService.ExportSheet(deck, outputPath, paper, dpi);
    }

    public ToolResult ExportDiy(string? deckId, string outputPath, string paper)
    {
        if (string.IsNullOrWhiteSpace(deckId))
        {
            return ToolResult.Fail("Missing deck id. Use --deck <deck_id>.");
        }

        var deck = LoadDeckById(deckId);
        return deck is null
            ? ToolResult.Fail($"Deck '{deckId}' was not found.")
            : _diyExportService.ExportDiy(deck, outputPath, paper);
    }

    public ToolResult ExportShowcase(string? deckId, string outputPath, string format)
    {
        if (string.IsNullOrWhiteSpace(deckId))
        {
            return ToolResult.Fail("Missing deck id. Use --deck <deck_id>.");
        }

        var deck = _deckRepository.LoadDeckById(deckId);
        return deck is null
            ? ToolResult.Fail($"Deck '{deckId}' was not found.")
            : _deckExportService.ExportShowcase(deck, outputPath, format);
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
        var diyExportService = new DiyExportService();
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
}
