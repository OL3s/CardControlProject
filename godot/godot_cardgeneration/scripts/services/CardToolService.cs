using CardGeneration.App;

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

    public CardToolService()
        : this(
            new CardRepository(),
            new DeckRepository(),
            new CardValidator(),
            new DeckValidator(),
            new CardRenderService(),
            new DeckExportService(),
            new SheetExportService(),
            new DiyExportService())
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
        DiyExportService diyExportService)
    {
        _cardRepository = cardRepository;
        _deckRepository = deckRepository;
        _cardValidator = cardValidator;
        _deckValidator = deckValidator;
        _cardRenderService = cardRenderService;
        _deckExportService = deckExportService;
        _sheetExportService = sheetExportService;
        _diyExportService = diyExportService;
    }

    public ToolResult ListCards()
    {
        var cards = _cardRepository.LoadAllCards();
        return ToolResult.Ok($"Found {cards.Count} saved cards.");
    }

    public ToolResult ListDecks()
    {
        var decks = _deckRepository.LoadAllDecks();
        return ToolResult.Ok($"Found {decks.Count} saved decks.");
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

        var deck = _deckRepository.LoadDeckById(deckId);
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

    public ToolResult ExportDeck(string? deckId, string outputPath, string format)
    {
        if (string.IsNullOrWhiteSpace(deckId))
        {
            return ToolResult.Fail("Missing deck id. Use --deck <deck_id>.");
        }

        var deck = _deckRepository.LoadDeckById(deckId);
        return deck is null
            ? ToolResult.Fail($"Deck '{deckId}' was not found.")
            : _deckExportService.ExportDeck(deck, outputPath, format);
    }

    public ToolResult ExportSheet(string? deckId, string outputPath, string paper)
    {
        if (string.IsNullOrWhiteSpace(deckId))
        {
            return ToolResult.Fail("Missing deck id. Use --deck <deck_id>.");
        }

        var deck = _deckRepository.LoadDeckById(deckId);
        return deck is null
            ? ToolResult.Fail($"Deck '{deckId}' was not found.")
            : _sheetExportService.ExportSheet(deck, outputPath, paper);
    }

    public ToolResult ExportDiy(string? deckId, string outputPath, string paper)
    {
        if (string.IsNullOrWhiteSpace(deckId))
        {
            return ToolResult.Fail("Missing deck id. Use --deck <deck_id>.");
        }

        var deck = _deckRepository.LoadDeckById(deckId);
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
}
