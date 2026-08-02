using System;
using System.Collections.Generic;
using System.IO;
using CardGeneration.App;
using CardGeneration.Rendering;
using CardGeneration.Resources;
using CardGeneration.Resources.Enums;
using Godot;

namespace CardGeneration.Services;

public sealed class SheetExportService
{
    private static readonly int[] SupportedDpiValues = [150, 300, 600, 1200];
    private const double MillimetersPerInch = 25.4;
    private const double CardWidthMillimeters = 63.0;
    private const double CardHeightMillimeters = 88.0;
    private const double MeasurementGuideHeightMillimeters = 14.0;
    private const double MeasurementGuideLengthMillimeters = 100.0;

    public ToolResult ExportSheet(CardDeckResource deck, string outputPath, string paper, int dpi, string backMirror = "none", bool includeMeasurementGuide = false, Action<ExportProgress>? progress = null)
    {
        if (!TryGetPaperSpec(paper, out var paperSpec))
        {
            return ToolResult.Fail($"Paper '{paper}' is not supported. Use a4 or a3.");
        }

        if (!IsSupportedDpi(dpi))
        {
            return ToolResult.Fail($"DPI '{dpi}' is not supported. Use one of: {string.Join(", ", SupportedDpiValues)}.");
        }

        if (!IsSupportedBackMirror(backMirror))
        {
            return ToolResult.Fail($"Back mirror '{backMirror}' is not supported. Use none, width, height, or both.");
        }

        var cards = ExpandDeckCards(deck);
        if (cards.Count == 0)
        {
            return ToolResult.Fail($"Deck '{deck.Id}' has no cards to export.");
        }

        var outputDirectory = ProjectPaths.ToGlobalPath(outputPath);
        if (Path.GetExtension(outputDirectory).Equals(".png", StringComparison.OrdinalIgnoreCase))
        {
            return ToolResult.Fail("Sheet export requires an output directory, not a .png file path.");
        }

        Directory.CreateDirectory(outputDirectory);

        var sheetWidth = MillimetersToPixels(paperSpec.WidthMillimeters, dpi);
        var sheetHeight = MillimetersToPixels(paperSpec.HeightMillimeters, dpi);
        var cardWidth = MillimetersToPixels(CardWidthMillimeters, dpi);
        var cardHeight = MillimetersToPixels(CardHeightMillimeters, dpi);
        var guideHeight = includeMeasurementGuide ? MillimetersToPixels(MeasurementGuideHeightMillimeters, dpi) : 0;
        var layoutHeight = Math.Max(cardHeight, sheetHeight - guideHeight);
        var columns = Math.Max(1, sheetWidth / cardWidth);
        var rows = Math.Max(1, layoutHeight / cardHeight);
        var cardsPerSheet = columns * rows;
        var xGap = (sheetWidth - columns * cardWidth) / (columns + 1);
        var yGap = (layoutHeight - rows * cardHeight) / (rows + 1);
        var sheetCount = (int)Math.Ceiling(cards.Count / (double)cardsPerSheet);
        var backImages = new Dictionary<CardType, Image>();
        var totalProgress = cards.Count * 2;
        var currentProgress = 0;

        for (var sheetIndex = 0; sheetIndex < sheetCount; sheetIndex++)
        {
            var frontSheet = CreatePrintSheet(sheetWidth, sheetHeight);
            var firstCardIndex = sheetIndex * cardsPerSheet;
            var lastCardIndexExclusive = Math.Min(firstCardIndex + cardsPerSheet, cards.Count);

            for (var cardIndex = firstCardIndex; cardIndex < lastCardIndexExclusive; cardIndex++)
            {
                progress?.Invoke(new ExportProgress(currentProgress, totalProgress, $"Rendering front {cardIndex + 1}/{cards.Count}: {cards[cardIndex].Id}"));
                var position = GetCardPosition(cardIndex - firstCardIndex, columns, cardWidth, cardHeight, xGap, yGap);
                var frontImage = CardImageRenderer.RenderResized(cards[cardIndex], cardWidth, cardHeight);
                frontSheet.BlendRect(frontImage, new Rect2I(Vector2I.Zero, frontImage.GetSize()), position);
                frontImage.Dispose();
                currentProgress++;
                progress?.Invoke(new ExportProgress(currentProgress, totalProgress, $"Placed front {cardIndex + 1}/{cards.Count}: {cards[cardIndex].Id}"));
            }

            var frontPath = Path.Combine(outputDirectory, $"{deck.Id}_{paperSpec.Name}_{dpi}dpi_front_{sheetIndex + 1:000}.png");
            if (includeMeasurementGuide)
            {
                DrawMeasurementGuide(frontSheet, sheetWidth, sheetHeight, dpi);
            }

            var frontError = frontSheet.SavePng(frontPath);
            frontSheet.Dispose();
            if (frontError != Error.Ok)
            {
                return ToolResult.Fail($"Failed to save front sheet {frontPath}: {frontError}.");
            }

            var backSheet = CreatePrintSheet(sheetWidth, sheetHeight);
            for (var cardIndex = firstCardIndex; cardIndex < lastCardIndexExclusive; cardIndex++)
            {
                progress?.Invoke(new ExportProgress(currentProgress, totalProgress, $"Placing back {cardIndex + 1}/{cards.Count}: {cards[cardIndex].Id}"));
                var position = GetCardPosition(cardIndex - firstCardIndex, columns, cardWidth, cardHeight, xGap, yGap);
                var backImage = GetBackImage(cards[cardIndex].CardType, deck.GetBackImageTexture(cards[cardIndex].CardType), cardWidth, cardHeight, backImages);
                backSheet.BlendRect(backImage, new Rect2I(Vector2I.Zero, backImage.GetSize()), position);
                currentProgress++;
                progress?.Invoke(new ExportProgress(currentProgress, totalProgress, $"Placed back {cardIndex + 1}/{cards.Count}: {cards[cardIndex].Id}"));
            }

            ApplyBackMirror(backSheet, backMirror);

            if (includeMeasurementGuide)
            {
                DrawMeasurementGuide(backSheet, sheetWidth, sheetHeight, dpi);
            }

            var backPath = Path.Combine(outputDirectory, $"{deck.Id}_{paperSpec.Name}_{dpi}dpi_back_{sheetIndex + 1:000}.png");
            var backError = backSheet.SavePng(backPath);
            backSheet.Dispose();
            if (backError != Error.Ok)
            {
                return ToolResult.Fail($"Failed to save back sheet {backPath}: {backError}.");
            }
        }

        foreach (var image in backImages.Values)
        {
            image.Dispose();
        }

        return ToolResult.Ok($"Exported {sheetCount} {paperSpec.Name.ToUpperInvariant()} {dpi} DPI front/back sheet pair(s) for deck '{deck.Id}' to {outputDirectory}.");
    }

    public Image? RenderSheetPreview(CardDeckResource deck, string paper, int dpi, string backMirror, bool includeMeasurementGuide, bool showBack, out string errorMessage)
    {
        errorMessage = string.Empty;
        if (!TryGetPaperSpec(paper, out var paperSpec))
        {
            errorMessage = $"Paper '{paper}' is not supported. Use a4 or a3.";
            return null;
        }

        if (!IsSupportedDpi(dpi))
        {
            errorMessage = $"DPI '{dpi}' is not supported. Use one of: {string.Join(", ", SupportedDpiValues)}.";
            return null;
        }

        if (!IsSupportedBackMirror(backMirror))
        {
            errorMessage = $"Back mirror '{backMirror}' is not supported. Use none, width, height, or both.";
            return null;
        }

        var cards = ExpandDeckCards(deck);
        if (cards.Count == 0)
        {
            errorMessage = $"Deck '{deck.Id}' has no cards to preview.";
            return null;
        }

        var previewDpi = Math.Min(dpi, 150);
        var sheetWidth = MillimetersToPixels(paperSpec.WidthMillimeters, previewDpi);
        var sheetHeight = MillimetersToPixels(paperSpec.HeightMillimeters, previewDpi);
        var cardWidth = MillimetersToPixels(CardWidthMillimeters, previewDpi);
        var cardHeight = MillimetersToPixels(CardHeightMillimeters, previewDpi);
        var guideHeight = includeMeasurementGuide ? MillimetersToPixels(MeasurementGuideHeightMillimeters, previewDpi) : 0;
        var layoutHeight = Math.Max(cardHeight, sheetHeight - guideHeight);
        var columns = Math.Max(1, sheetWidth / cardWidth);
        var rows = Math.Max(1, layoutHeight / cardHeight);
        var cardsPerSheet = columns * rows;
        var xGap = (sheetWidth - columns * cardWidth) / (columns + 1);
        var yGap = (layoutHeight - rows * cardHeight) / (rows + 1);
        var sheet = CreatePrintSheet(sheetWidth, sheetHeight);
        var backImages = new Dictionary<CardType, Image>();

        for (var cardIndex = 0; cardIndex < Math.Min(cards.Count, cardsPerSheet); cardIndex++)
        {
            var position = GetCardPosition(cardIndex, columns, cardWidth, cardHeight, xGap, yGap);
            if (showBack)
            {
                var backImage = GetBackImage(cards[cardIndex].CardType, deck.GetBackImageTexture(cards[cardIndex].CardType), cardWidth, cardHeight, backImages);
                sheet.BlendRect(backImage, new Rect2I(Vector2I.Zero, backImage.GetSize()), position);
                continue;
            }

            var frontImage = CardImageRenderer.RenderResized(cards[cardIndex], cardWidth, cardHeight);
            sheet.BlendRect(frontImage, new Rect2I(Vector2I.Zero, frontImage.GetSize()), position);
            frontImage.Dispose();
        }

        foreach (var image in backImages.Values)
        {
            image.Dispose();
        }

        if (showBack)
        {
            ApplyBackMirror(sheet, backMirror);
        }

        if (includeMeasurementGuide)
        {
            DrawMeasurementGuide(sheet, sheetWidth, sheetHeight, previewDpi);
        }

        return sheet;
    }

    private static Vector2I GetCardPosition(int slotIndex, int columns, int cardWidth, int cardHeight, int xGap, int yGap)
    {
        var column = slotIndex % columns;
        var row = slotIndex / columns;
        return new Vector2I(
            xGap + column * (cardWidth + xGap),
            yGap + row * (cardHeight + yGap));
    }

    private static Image CreatePrintSheet(int width, int height)
    {
        var sheet = Image.CreateEmpty(width, height, false, Image.Format.Rgba8);
        sheet.Fill(new Color(1, 1, 1, 1));
        return sheet;
    }

    private static void DrawMeasurementGuide(Image sheet, int sheetWidth, int sheetHeight, int dpi)
    {
        var lineLength = MillimetersToPixels(MeasurementGuideLengthMillimeters, dpi);
        var centimeter = MillimetersToPixels(10.0, dpi);
        var lineThickness = Math.Max(1, MillimetersToPixels(0.35, dpi));
        var tickThickness = Math.Max(1, MillimetersToPixels(0.3, dpi));
        var tickHeight = Math.Max(2, MillimetersToPixels(4.0, dpi));
        var y = sheetHeight - MillimetersToPixels(6.0, dpi);
        var startX = Math.Max(0, (sheetWidth - lineLength) / 2);
        var endX = Math.Min(sheetWidth - 1, startX + lineLength);
        var color = new Color(0.02f, 0.02f, 0.02f, 1);

        sheet.FillRect(new Rect2I(startX, y - lineThickness / 2, Math.Max(1, endX - startX), lineThickness), color);

        for (var index = 0; index <= 10; index++)
        {
            var x = Math.Min(sheetWidth - tickThickness, startX + index * centimeter);
            sheet.FillRect(new Rect2I(x, y - tickHeight, tickThickness, tickHeight * 2), color);
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

    private static Image GetBackImage(CardType cardType, Texture2D? backImageTexture, int width, int height, Dictionary<CardType, Image> backImages)
    {
        if (!backImages.TryGetValue(cardType, out var backImage))
        {
            backImage = CardImageRenderer.RenderBackResized(cardType, backImageTexture, width, height);
            backImages[cardType] = backImage;
        }

        return backImage;
    }

    private static bool IsSupportedDpi(int dpi)
    {
        foreach (var supportedDpi in SupportedDpiValues)
        {
            if (dpi == supportedDpi)
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsSupportedBackMirror(string backMirror)
    {
        return backMirror is "none" or "width" or "height" or "both";
    }

    private static void ApplyBackMirror(Image image, string backMirror)
    {
        switch (backMirror)
        {
            case "width":
                image.FlipX();
                break;
            case "height":
                image.FlipY();
                break;
            case "both":
                image.FlipX();
                image.FlipY();
                break;
        }
    }

    private static int MillimetersToPixels(double millimeters, int dpi)
    {
        return (int)Math.Round(millimeters * dpi / MillimetersPerInch);
    }

    private static bool TryGetPaperSpec(string paper, out PaperSpec paperSpec)
    {
        switch (paper.ToLowerInvariant())
        {
            case "a4":
                paperSpec = new PaperSpec("a4", 210, 297);
                return true;
            case "a3":
                paperSpec = new PaperSpec("a3", 297, 420);
                return true;
            default:
                paperSpec = default;
                return false;
        }
    }

    private readonly record struct PaperSpec(string Name, double WidthMillimeters, double HeightMillimeters);
}
