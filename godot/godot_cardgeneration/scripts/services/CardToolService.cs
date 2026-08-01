using System.Linq;
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
            services.DiyExportService)
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
        return ToolResult.Ok(cards.Count == 0
            ? "Found 0 saved cards."
            : $"Found {cards.Count} saved cards:\n{string.Join("\n", cards.Select(card => $"- {card.Id}"))}");
    }

    public ToolResult ListDecks()
    {
        var decks = _deckRepository.LoadAllDecks();
        return ToolResult.Ok(decks.Count == 0
            ? "Found 0 saved decks."
            : $"Found {decks.Count} saved decks:\n{string.Join("\n", decks.Select(deck => $"- {deck.Id}"))}");
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

    public ToolResult ExportDeck(string? deckId, string outputPath, string format, string layout, int columns, int spacing)
    {
        if (string.IsNullOrWhiteSpace(deckId))
        {
            return ToolResult.Fail("Missing deck id. Use --deck <deck_id>.");
        }

        var deck = _deckRepository.LoadDeckById(deckId);
        return deck is null
            ? ToolResult.Fail($"Deck '{deckId}' was not found.")
            : _deckExportService.ExportDeck(deck, outputPath, format, layout, columns, spacing);
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

        return new DefaultServices(
            cardRepository,
            deckRepository,
            cardValidator,
            deckValidator,
            cardRenderService,
            deckExportService,
            sheetExportService,
            diyExportService);
    }

    private sealed record DefaultServices(
        CardRepository CardRepository,
        DeckRepository DeckRepository,
        CardValidator CardValidator,
        DeckValidator DeckValidator,
        CardRenderService CardRenderService,
        DeckExportService DeckExportService,
        SheetExportService SheetExportService,
        DiyExportService DiyExportService);
}
