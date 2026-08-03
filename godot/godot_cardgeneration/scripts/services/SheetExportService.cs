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

    public ToolResult ExportSheet(CardDeckResource deck, string outputPath, string paper, int dpi, string backMirror = "none", bool includeMeasurementGuide = false, Action<ExportProgress>? progress = null, bool easyPrintBacks = false)
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
        var pagePlans = CreatePagePlans(cards, cardsPerSheet, easyPrintBacks);
        var sheetCount = pagePlans.Count;
        var backImages = new Dictionary<CardType, Image>();
        var totalBackCards = easyPrintBacks ? sheetCount * cardsPerSheet : cards.Count;
        var totalProgress = cards.Count + totalBackCards;
        var currentProgress = 0;
        var frontCardsPlaced = 0;

        for (var sheetIndex = 0; sheetIndex < sheetCount; sheetIndex++)
        {
            var frontSheet = CreatePrintSheet(sheetWidth, sheetHeight);
            var pagePlan = pagePlans[sheetIndex];

            for (var slotIndex = 0; slotIndex < pagePlan.FrontCards.Count; slotIndex++)
            {
                var card = pagePlan.FrontCards[slotIndex];
                progress?.Invoke(new ExportProgress(currentProgress, totalProgress, $"Rendering front {frontCardsPlaced + 1}/{cards.Count}: {card.Id}"));
                var position = GetCardPosition(slotIndex, columns, cardWidth, cardHeight, xGap, yGap);
                var frontImage = CardImageRenderer.Render(card, new Vector2I(cardWidth, cardHeight), deck.GetElementIconOverrides(), deck.PowerIconTexture);
                frontSheet.BlendRect(frontImage, new Rect2I(Vector2I.Zero, frontImage.GetSize()), position);
                frontImage.Dispose();
                currentProgress++;
                frontCardsPlaced++;
                progress?.Invoke(new ExportProgress(currentProgress, totalProgress, $"Placed front {frontCardsPlaced}/{cards.Count}: {card.Id}"));
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
            var backSlotCount = pagePlan.FilledBackType.HasValue ? cardsPerSheet : pagePlan.FrontCards.Count;
            for (var slotIndex = 0; slotIndex < backSlotCount; slotIndex++)
            {
                var cardType = pagePlan.FilledBackType ?? pagePlan.FrontCards[slotIndex].CardType;
                progress?.Invoke(new ExportProgress(currentProgress, totalProgress, $"Placing back slot {slotIndex + 1}/{backSlotCount} on page {sheetIndex + 1}/{sheetCount}"));
                var position = GetCardPosition(slotIndex, columns, cardWidth, cardHeight, xGap, yGap);
                var backImage = GetBackImage(cardType, deck.GetBackImageTexture(cardType), cardWidth, cardHeight, backImages);
                backSheet.BlendRect(backImage, new Rect2I(Vector2I.Zero, backImage.GetSize()), position);
                currentProgress++;
                progress?.Invoke(new ExportProgress(currentProgress, totalProgress, $"Placed back slot {slotIndex + 1}/{backSlotCount} on page {sheetIndex + 1}/{sheetCount}"));
            }

            ApplyBackMirror(backSheet, easyPrintBacks ? "none" : backMirror);

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

        var modeDescription = easyPrintBacks ? " easy-back" : string.Empty;
        return ToolResult.Ok($"Exported {sheetCount} {paperSpec.Name.ToUpperInvariant()} {dpi} DPI{modeDescription} front/back sheet pair(s) for deck '{deck.Id}' to {outputDirectory}.");
    }

    public IReadOnlyList<SheetPreviewPage>? RenderSheetPreviews(CardDeckResource deck, string paper, int dpi, string backMirror, bool includeMeasurementGuide, bool easyPrintBacks, out string errorMessage, Action<ExportProgress>? progress = null)
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
        var pagePlans = CreatePagePlans(cards, cardsPerSheet, easyPrintBacks);
        var sheetCount = pagePlans.Count;
        var totalBackCards = easyPrintBacks ? sheetCount * cardsPerSheet : cards.Count;
        var totalProgress = cards.Count + totalBackCards;
        var currentProgress = 0;
        var frontCardsRendered = 0;
        var pages = new List<SheetPreviewPage>(sheetCount);
        var backImages = new Dictionary<CardType, Image>();

        try
        {
            for (var sheetIndex = 0; sheetIndex < sheetCount; sheetIndex++)
            {
                Image? frontSheet = CreatePrintSheet(sheetWidth, sheetHeight);
                Image? backSheet = CreatePrintSheet(sheetWidth, sheetHeight);
                try
                {
                    var pagePlan = pagePlans[sheetIndex];
                    for (var slotIndex = 0; slotIndex < pagePlan.FrontCards.Count; slotIndex++)
                    {
                        var card = pagePlan.FrontCards[slotIndex];
                        progress?.Invoke(new ExportProgress(currentProgress, totalProgress, $"Rendering preview front {frontCardsRendered + 1}/{cards.Count}: {card.Id}"));
                        var position = GetCardPosition(slotIndex, columns, cardWidth, cardHeight, xGap, yGap);
                        var frontImage = CardImageRenderer.Render(card, new Vector2I(cardWidth, cardHeight), deck.GetElementIconOverrides(), deck.PowerIconTexture);
                        frontSheet.BlendRect(frontImage, new Rect2I(Vector2I.Zero, frontImage.GetSize()), position);
                        frontImage.Dispose();
                        currentProgress++;
                        frontCardsRendered++;
                        progress?.Invoke(new ExportProgress(currentProgress, totalProgress, $"Rendered preview front {frontCardsRendered}/{cards.Count}: {card.Id}"));
                    }

                    var backSlotCount = pagePlan.FilledBackType.HasValue ? cardsPerSheet : pagePlan.FrontCards.Count;
                    for (var slotIndex = 0; slotIndex < backSlotCount; slotIndex++)
                    {
                        var cardType = pagePlan.FilledBackType ?? pagePlan.FrontCards[slotIndex].CardType;
                        progress?.Invoke(new ExportProgress(currentProgress, totalProgress, $"Rendering preview back {slotIndex + 1}/{backSlotCount} on page {sheetIndex + 1}/{sheetCount}"));
                        var position = GetCardPosition(slotIndex, columns, cardWidth, cardHeight, xGap, yGap);
                        var backImage = GetBackImage(cardType, deck.GetBackImageTexture(cardType), cardWidth, cardHeight, backImages);
                        backSheet.BlendRect(backImage, new Rect2I(Vector2I.Zero, backImage.GetSize()), position);
                        currentProgress++;
                        progress?.Invoke(new ExportProgress(currentProgress, totalProgress, $"Rendered preview back {slotIndex + 1}/{backSlotCount} on page {sheetIndex + 1}/{sheetCount}"));
                    }

                    ApplyBackMirror(backSheet, easyPrintBacks ? "none" : backMirror);
                    if (includeMeasurementGuide)
                    {
                        DrawMeasurementGuide(frontSheet, sheetWidth, sheetHeight, previewDpi);
                        DrawMeasurementGuide(backSheet, sheetWidth, sheetHeight, previewDpi);
                    }

                    pages.Add(new SheetPreviewPage(sheetIndex + 1, frontSheet, backSheet));
                    frontSheet = null;
                    backSheet = null;
                }
                finally
                {
                    frontSheet?.Dispose();
                    backSheet?.Dispose();
                }
            }

            return pages;
        }
        catch (Exception exception)
        {
            foreach (var page in pages)
            {
                page.Dispose();
            }

            errorMessage = $"Could not render print preview: {exception.Message}";
            return null;
        }
        finally
        {
            foreach (var image in backImages.Values)
            {
                image.Dispose();
            }
        }
    }

    private static Vector2I GetCardPosition(int slotIndex, int columns, int cardWidth, int cardHeight, int xGap, int yGap)
    {
        var column = slotIndex % columns;
        var row = slotIndex / columns;
        return new Vector2I(
            xGap + column * (cardWidth + xGap),
            yGap + row * (cardHeight + yGap));
    }

    private static IReadOnlyList<SheetPagePlan> CreatePagePlans(IReadOnlyList<CardResource> cards, int cardsPerSheet, bool easyPrintBacks)
    {
        var plans = new List<SheetPagePlan>();
        if (!easyPrintBacks)
        {
            AddPagePlans(plans, cards, cardsPerSheet, null);
            return plans;
        }

        var groupedCards = new Dictionary<CardType, List<CardResource>>();
        var typeOrder = new List<CardType>();
        foreach (var card in cards)
        {
            if (!groupedCards.TryGetValue(card.CardType, out var group))
            {
                group = [];
                groupedCards[card.CardType] = group;
                typeOrder.Add(card.CardType);
            }

            group.Add(card);
        }

        foreach (var cardType in typeOrder)
        {
            AddPagePlans(plans, groupedCards[cardType], cardsPerSheet, cardType);
        }

        return plans;
    }

    private static void AddPagePlans(List<SheetPagePlan> plans, IReadOnlyList<CardResource> cards, int cardsPerSheet, CardType? filledBackType)
    {
        for (var firstIndex = 0; firstIndex < cards.Count; firstIndex += cardsPerSheet)
        {
            var pageCards = new List<CardResource>(Math.Min(cardsPerSheet, cards.Count - firstIndex));
            for (var index = firstIndex; index < Math.Min(firstIndex + cardsPerSheet, cards.Count); index++)
            {
                pageCards.Add(cards[index]);
            }

            plans.Add(new SheetPagePlan(pageCards, filledBackType));
        }
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

    private sealed record SheetPagePlan(IReadOnlyList<CardResource> FrontCards, CardType? FilledBackType);
}

public sealed class SheetPreviewPage : IDisposable
{
    public SheetPreviewPage(int pageNumber, Image front, Image back)
    {
        PageNumber = pageNumber;
        Front = front;
        Back = back;
    }

    public int PageNumber { get; }

    public Image Front { get; }

    public Image Back { get; }

    public void Dispose()
    {
        Front.Dispose();
        Back.Dispose();
    }
}
