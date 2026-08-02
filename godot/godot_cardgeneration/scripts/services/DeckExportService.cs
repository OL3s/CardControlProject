using System;
using System.Collections.Generic;
using System.IO;
using CardGeneration.App;
using CardGeneration.Rendering;
using CardGeneration.Resources;
using CardGeneration.Resources.Enums;
using Godot;

namespace CardGeneration.Services;

public sealed class DeckExportService
{
    private static readonly Vector2I PreviewCardSize = new(150, 210);
    private readonly CardRenderService _cardRenderService;

    public DeckExportService()
        : this(new CardRenderService())
    {
    }

    public DeckExportService(CardRenderService cardRenderService)
    {
        _cardRenderService = cardRenderService;
    }

    public ToolResult ExportDeck(CardDeckResource deck, string outputPath, string format, string layout, int columns, int spacing, Action<ExportProgress>? progress = null, ImageBackMode backMode = ImageBackMode.None)
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

        var items = BuildImageItems(deck, cards, backMode);

        return layout switch
        {
            "individual" => ExportIndividualCards(deck, items, outputPath, progress),
            "grid" => ExportCombinedImage(deck, items, outputPath, ResolveGridColumns(items.Count, columns), spacing, "grid", progress),
            "strip" => ExportCombinedImage(deck, items, outputPath, 1, spacing, "strip", progress),
            _ => ToolResult.Fail($"Deck layout '{layout}' is not supported. Use individual, grid, or strip.")
        };
    }

    public ToolResult ExportShowcase(CardDeckResource deck, string outputPath, string format, Action<ExportProgress>? progress = null)
    {
        return ExportDeck(deck, outputPath, format, "grid", 0, 32, progress);
    }

    public IReadOnlyList<ImagePreviewItem>? RenderPreviews(CardDeckResource deck, string layout, int columns, int spacing, out string errorMessage, Action<ExportProgress>? progress = null, ImageBackMode backMode = ImageBackMode.None)
    {
        errorMessage = string.Empty;
        var cards = ExpandDeckCards(deck);
        if (cards.Count == 0)
        {
            errorMessage = $"Deck '{deck.Id}' has no cards to preview.";
            return null;
        }

        var imageItems = BuildImageItems(deck, cards, backMode);

        var previewSpacing = Math.Max(0, (int)Math.Round(spacing * PreviewCardSize.X / (double)CardImageRenderer.PreviewWidth));
        if (layout == "individual")
        {
            var items = new List<ImagePreviewItem>(imageItems.Count);
            try
            {
                for (var index = 0; index < imageItems.Count; index++)
                {
                    progress?.Invoke(new ExportProgress(index, imageItems.Count, $"Rendering preview {index + 1}/{imageItems.Count}: {imageItems[index].Label}"));
                    items.Add(new ImagePreviewItem($"{index + 1}. {imageItems[index].Label}", RenderImageItem(imageItems[index], PreviewCardSize)));
                    progress?.Invoke(new ExportProgress(index + 1, imageItems.Count, $"Rendered preview {index + 1}/{imageItems.Count}: {imageItems[index].Label}"));
                }

                return items;
            }
            catch (Exception exception)
            {
                DisposePreviewItems(items);
                errorMessage = $"Could not render individual image previews: {exception.Message}";
                return null;
            }
        }

        var previewColumns = layout switch
        {
            "grid" => ResolveGridColumns(imageItems.Count, columns),
            "strip" => 1,
            _ => 0
        };
        if (previewColumns == 0)
        {
            errorMessage = $"Deck layout '{layout}' is not supported. Use individual, grid, or strip.";
            return null;
        }

        try
        {
            var image = RenderCombinedImage(imageItems, previewColumns, previewSpacing, PreviewCardSize, progress);
            return [new ImagePreviewItem($"{deck.Id} {layout}", image)];
        }
        catch (Exception exception)
        {
            errorMessage = $"Could not render {layout} image preview: {exception.Message}";
            return null;
        }
    }

    private static ToolResult ExportIndividualCards(CardDeckResource deck, IReadOnlyList<DeckImageItem> items, string outputPath, Action<ExportProgress>? progress)
    {
        var outputDirectory = ProjectPaths.ToGlobalPath(outputPath);
        if (Path.GetExtension(outputDirectory).Equals(".png", StringComparison.OrdinalIgnoreCase))
        {
            return ToolResult.Fail("Individual deck export requires an output directory, not a .png file path.");
        }

        Directory.CreateDirectory(outputDirectory);

        for (var index = 0; index < items.Count; index++)
        {
            var item = items[index];
            progress?.Invoke(new ExportProgress(index, items.Count, $"Rendering {index + 1}/{items.Count}: {item.Label}"));
            var image = RenderImageItem(item, new Vector2I(CardImageRenderer.PreviewWidth, CardImageRenderer.PreviewHeight));
            var filePath = Path.Combine(outputDirectory, $"{deck.Id}_{index + 1:000}_{item.FileNameStem}.png");
            var error = image.SavePng(filePath);
            image.Dispose();
            if (error != Error.Ok)
            {
                return ToolResult.Fail($"Failed to export image '{item.Label}' to {filePath}: {error}.");
            }

            progress?.Invoke(new ExportProgress(index + 1, items.Count, $"Saved {index + 1}/{items.Count}: {item.Label}"));
        }

        return ToolResult.Ok($"Exported {items.Count} individual PNGs from deck '{deck.Id}' to {outputDirectory}.");
    }

    private static ToolResult ExportCombinedImage(CardDeckResource deck, IReadOnlyList<DeckImageItem> items, string outputPath, int columns, int spacing, string layout, Action<ExportProgress>? progress)
    {
        columns = Math.Max(1, columns);
        spacing = Math.Max(0, spacing);

        var combined = RenderCombinedImage(items, columns, spacing, new Vector2I(CardImageRenderer.PreviewWidth, CardImageRenderer.PreviewHeight), progress);

        var filePath = ProjectPaths.GetPngOutputPath(outputPath, $"{deck.Id}_{layout}");
        var error = combined.SavePng(filePath);
        combined.Dispose();
        return error == Error.Ok
            ? ToolResult.Ok($"Exported deck '{deck.Id}' as {layout} PNG to {filePath}.")
            : ToolResult.Fail($"Failed to export deck '{deck.Id}' as {layout} PNG to {filePath}: {error}.");
    }

    private static Image RenderCombinedImage(IReadOnlyList<DeckImageItem> items, int columns, int spacing, Vector2I cardSize, Action<ExportProgress>? progress = null)
    {
        columns = Math.Max(1, columns);
        spacing = Math.Max(0, spacing);
        var rows = (int)Math.Ceiling(items.Count / (double)columns);
        var width = columns * cardSize.X + (columns + 1) * spacing;
        var height = rows * cardSize.Y + (rows + 1) * spacing;
        var combined = Image.CreateEmpty(width, height, false, Image.Format.Rgba8);
        combined.Fill(new Color(0, 0, 0, 0));

        try
        {
            for (var index = 0; index < items.Count; index++)
            {
                var column = index % columns;
                var row = index / columns;
                var position = new Vector2I(spacing + column * (cardSize.X + spacing), spacing + row * (cardSize.Y + spacing));
                progress?.Invoke(new ExportProgress(index, items.Count, $"Rendering {index + 1}/{items.Count}: {items[index].Label}"));
                var cardImage = RenderImageItem(items[index], cardSize);
                combined.BlendRect(cardImage, new Rect2I(Vector2I.Zero, cardImage.GetSize()), position);
                cardImage.Dispose();
                progress?.Invoke(new ExportProgress(index + 1, items.Count, $"Placed {index + 1}/{items.Count}: {items[index].Label}"));
            }

            return combined;
        }
        catch
        {
            combined.Dispose();
            throw;
        }
    }

    private static IReadOnlyList<DeckImageItem> BuildImageItems(CardDeckResource deck, IReadOnlyList<CardResource> cards, ImageBackMode backMode)
    {
        var items = new List<DeckImageItem>();
        foreach (var cardType in new[] { CardType.Monster, CardType.Terrain, CardType.King })
        {
            if (backMode == ImageBackMode.All || backMode == ImageBackMode.Used && ContainsCardType(cards, cardType))
            {
                items.Add(DeckImageItem.ForBack(cardType, deck.GetBackImageTexture(cardType)));
            }
        }

        foreach (var card in cards)
        {
            items.Add(DeckImageItem.ForFront(card));
        }

        return items;
    }

    private static bool ContainsCardType(IReadOnlyList<CardResource> cards, CardType cardType)
    {
        foreach (var card in cards)
        {
            if (card.CardType == cardType)
            {
                return true;
            }
        }

        return false;
    }

    private static Image RenderImageItem(DeckImageItem item, Vector2I size)
    {
        return item.Card is null
            ? CardImageRenderer.RenderBack(item.CardType, item.BackImageTexture, size)
            : CardImageRenderer.Render(item.Card, size);
    }

    private static void DisposePreviewItems(IEnumerable<ImagePreviewItem> items)
    {
        foreach (var item in items)
        {
            item.Dispose();
        }
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

    private sealed record DeckImageItem(string Label, string FileNameStem, CardResource? Card, CardType CardType, Texture2D? BackImageTexture)
    {
        public static DeckImageItem ForFront(CardResource card) => new(card.Id, card.Id, card, card.CardType, null);

        public static DeckImageItem ForBack(CardType cardType, Texture2D? texture)
        {
            var typeName = cardType.ToString().ToLowerInvariant();
            return new DeckImageItem($"{typeName} back", $"{typeName}_back", null, cardType, texture);
        }
    }
}

public enum ImageBackMode
{
    None,
    Used,
    All
}

public sealed class ImagePreviewItem : IDisposable
{
    public ImagePreviewItem(string label, Image image)
    {
        Label = label;
        Image = image;
    }

    public string Label { get; }

    public Image Image { get; }

    public void Dispose()
    {
        Image.Dispose();
    }
}
