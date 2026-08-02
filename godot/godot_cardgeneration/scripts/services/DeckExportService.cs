using System;
using System.Collections.Generic;
using System.IO;
using CardGeneration.App;
using CardGeneration.Rendering;
using CardGeneration.Resources;
using Godot;

namespace CardGeneration.Services;

public sealed class DeckExportService
{
    private readonly CardRenderService _cardRenderService;

    public DeckExportService()
        : this(new CardRenderService())
    {
    }

    public DeckExportService(CardRenderService cardRenderService)
    {
        _cardRenderService = cardRenderService;
    }

    public ToolResult ExportDeck(CardDeckResource deck, string outputPath, string format, string layout, int columns, int spacing, Action<ExportProgress>? progress = null)
    {
        if (format != "png")
        {
            return ToolResult.Fail($"Deck export format '{format}' is not supported yet. Use png.");
        }

        var cards = ExpandDeckCards(deck);
        if (cards.Count == 0)
        {
            return ToolResult.Fail($"Deck '{deck.Id}' has no cards to export.");
        }

        return layout switch
        {
            "individual" => ExportIndividualCards(deck, cards, outputPath, progress),
            "grid" => ExportCombinedImage(deck, cards, outputPath, ResolveGridColumns(cards.Count, columns), spacing, "grid", progress),
            "strip" => ExportCombinedImage(deck, cards, outputPath, 1, spacing, "strip", progress),
            _ => ToolResult.Fail($"Deck layout '{layout}' is not supported. Use individual, grid, or strip.")
        };
    }

    public ToolResult ExportShowcase(CardDeckResource deck, string outputPath, string format)
    {
        return ExportDeck(deck, outputPath, format, "grid", 0, 32);
    }

    private ToolResult ExportIndividualCards(CardDeckResource deck, IReadOnlyList<CardResource> cards, string outputPath, Action<ExportProgress>? progress)
    {
        var outputDirectory = ProjectPaths.ToGlobalPath(outputPath);
        if (Path.GetExtension(outputDirectory).Equals(".png", StringComparison.OrdinalIgnoreCase))
        {
            return ToolResult.Fail("Individual deck export requires an output directory, not a .png file path.");
        }

        Directory.CreateDirectory(outputDirectory);

        foreach (var entry in deck.Entries)
        {
            if (entry.Card is null)
            {
                return ToolResult.Fail($"Deck '{deck.Id}' has an entry without a card.");
            }
        }

        for (var index = 0; index < cards.Count; index++)
        {
            var card = cards[index];
            progress?.Invoke(new ExportProgress(index, cards.Count, $"Rendering {index + 1}/{cards.Count}: {card.Id}"));
            var fileNameStem = $"{deck.Id}_{index + 1:000}_{card.Id}";
            var result = _cardRenderService.RenderCard(card, outputDirectory, fileNameStem);
            if (!result.Success)
            {
                return result;
            }

            progress?.Invoke(new ExportProgress(index + 1, cards.Count, $"Saved {index + 1}/{cards.Count}: {card.Id}"));
        }

        return ToolResult.Ok($"Exported {cards.Count} individual card PNGs from deck '{deck.Id}' to {outputDirectory}.");
    }

    private static ToolResult ExportCombinedImage(CardDeckResource deck, IReadOnlyList<CardResource> cards, string outputPath, int columns, int spacing, string layout, Action<ExportProgress>? progress)
    {
        columns = Math.Max(1, columns);
        spacing = Math.Max(0, spacing);

        var rows = (int)Math.Ceiling(cards.Count / (double)columns);
        var width = columns * CardImageRenderer.PreviewWidth + (columns + 1) * spacing;
        var height = rows * CardImageRenderer.PreviewHeight + (rows + 1) * spacing;
        var combined = Image.CreateEmpty(width, height, false, Image.Format.Rgba8);
        combined.Fill(new Color(0, 0, 0, 0));

        for (var index = 0; index < cards.Count; index++)
        {
            var column = index % columns;
            var row = index / columns;
            var x = spacing + column * (CardImageRenderer.PreviewWidth + spacing);
            var y = spacing + row * (CardImageRenderer.PreviewHeight + spacing);
            progress?.Invoke(new ExportProgress(index, cards.Count, $"Rendering {index + 1}/{cards.Count}: {cards[index].Id}"));
            var cardImage = CardImageRenderer.Render(cards[index]);
            combined.BlendRect(cardImage, new Rect2I(Vector2I.Zero, cardImage.GetSize()), new Vector2I(x, y));
            cardImage.Dispose();
            progress?.Invoke(new ExportProgress(index + 1, cards.Count, $"Placed {index + 1}/{cards.Count}: {cards[index].Id}"));
        }

        var filePath = ProjectPaths.GetPngOutputPath(outputPath, $"{deck.Id}_{layout}");
        var error = combined.SavePng(filePath);
        combined.Dispose();
        return error == Error.Ok
            ? ToolResult.Ok($"Exported deck '{deck.Id}' as {layout} PNG to {filePath}.")
            : ToolResult.Fail($"Failed to export deck '{deck.Id}' as {layout} PNG to {filePath}: {error}.");
    }

    private static IReadOnlyList<CardResource> ExpandDeckCards(CardDeckResource deck)
    {
        var cards = new List<CardResource>();
        foreach (var entry in deck.Entries)
        {
            if (entry.Card is null)
            {
                continue;
            }

            for (var copyIndex = 0; copyIndex < entry.Count; copyIndex++)
            {
                cards.Add(entry.Card);
            }
        }

        return cards;
    }

    private static int ResolveGridColumns(int cardCount, int requestedColumns)
    {
        return requestedColumns > 0 ? requestedColumns : (int)Math.Ceiling(Math.Sqrt(cardCount));
    }
}
