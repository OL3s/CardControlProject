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
    private const long StreamingSheetThresholdBytes = 256L * 1024 * 1024;

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
        var sheetBytes = checked((long)sheetWidth * sheetHeight * 4);
        if (ShouldUseStreamingExport(sheetBytes))
        {
            progress?.Invoke(new ExportProgress(0, 1, $"Using memory-safe streaming for {paperSpec.Name.ToUpperInvariant()} {dpi} DPI sheets."));
            return ExportStreamingSheets(
                deck,
                outputDirectory,
                paperSpec,
                dpi,
                easyPrintBacks ? "none" : backMirror,
                includeMeasurementGuide,
                easyPrintBacks,
                cards.Count,
                pagePlans,
                sheetWidth,
                sheetHeight,
                cardWidth,
                cardHeight,
                columns,
                rows,
                cardsPerSheet,
                xGap,
                yGap,
                progress);
        }

        var backImages = new Dictionary<CardType, Image>();
        var totalBackCards = easyPrintBacks ? sheetCount * cardsPerSheet : cards.Count;
        var totalProgress = cards.Count + totalBackCards;
        var currentProgress = 0;
        var frontCardsPlaced = 0;

        try
        {
            for (var sheetIndex = 0; sheetIndex < sheetCount; sheetIndex++)
            {
                var pagePlan = pagePlans[sheetIndex];
                using (var frontSheet = CreatePrintSheet(sheetWidth, sheetHeight))
                {
                    for (var slotIndex = 0; slotIndex < pagePlan.FrontCards.Count; slotIndex++)
                    {
                        var card = pagePlan.FrontCards[slotIndex];
                        progress?.Invoke(new ExportProgress(currentProgress, totalProgress, $"Rendering front {frontCardsPlaced + 1}/{cards.Count}: {card.Id}"));
                        var position = GetCardPosition(slotIndex, columns, cardWidth, cardHeight, xGap, yGap);
                        using var frontImage = CardImageRenderer.Render(card, new Vector2I(cardWidth, cardHeight), deck.GetElementIconOverrides(), deck.PowerIconTexture);
                        frontSheet.BlendRect(frontImage, new Rect2I(Vector2I.Zero, frontImage.GetSize()), position);
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
                    if (frontError != Error.Ok)
                    {
                        return ToolResult.Fail($"Failed to save front sheet {frontPath}: {frontError}.");
                    }
                }

                using (var backSheet = CreatePrintSheet(sheetWidth, sheetHeight))
                {
                    var backSlotCount = pagePlan.FilledBackType.HasValue ? cardsPerSheet : pagePlan.FrontCards.Count;
                    for (var slotIndex = 0; slotIndex < backSlotCount; slotIndex++)
                    {
                        var cardType = pagePlan.FilledBackType ?? pagePlan.FrontCards[slotIndex].CardType;
                        progress?.Invoke(new ExportProgress(currentProgress, totalProgress, $"Placing back slot {slotIndex + 1}/{backSlotCount} on page {sheetIndex + 1}/{sheetCount}"));
                        var position = GetCardPosition(slotIndex, columns, cardWidth, cardHeight, xGap, yGap);
                        var backImage = GetBackImage(cardType, deck, cardWidth, cardHeight, backImages);
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
                    if (backError != Error.Ok)
                    {
                        return ToolResult.Fail($"Failed to save back sheet {backPath}: {backError}.");
                    }
                }
            }
        }
        finally
        {
            foreach (var image in backImages.Values)
            {
                image.Dispose();
            }
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
                        var backImage = GetBackImage(cardType, deck, cardWidth, cardHeight, backImages);
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

    private static ToolResult ExportStreamingSheets(
        CardDeckResource deck,
        string outputDirectory,
        PaperSpec paperSpec,
        int dpi,
        string backMirror,
        bool includeMeasurementGuide,
        bool easyPrintBacks,
        int totalFrontCards,
        IReadOnlyList<SheetPagePlan> pagePlans,
        int sheetWidth,
        int sheetHeight,
        int cardWidth,
        int cardHeight,
        int columns,
        int rows,
        int cardsPerSheet,
        int xGap,
        int yGap,
        Action<ExportProgress>? progress)
    {
        var totalBackCards = easyPrintBacks ? pagePlans.Count * cardsPerSheet : totalFrontCards;
        var totalProgress = totalFrontCards + totalBackCards;
        var currentProgress = 0;
        var frontCardsRendered = 0;
        var backBuffers = new Dictionary<CardType, CardPixelBuffer>();
        try
        {
            for (var pageIndex = 0; pageIndex < pagePlans.Count; pageIndex++)
            {
                var pagePlan = pagePlans[pageIndex];
                var frontPath = Path.Combine(outputDirectory, $"{deck.Id}_{paperSpec.Name}_{dpi}dpi_front_{pageIndex + 1:000}.png");
                WriteStreamingFrontSheet(
                    frontPath,
                    deck,
                    pagePlan.FrontCards,
                    sheetWidth,
                    sheetHeight,
                    cardWidth,
                    cardHeight,
                    columns,
                    rows,
                    xGap,
                    yGap,
                    dpi,
                    includeMeasurementGuide,
                    totalFrontCards,
                    totalProgress,
                    ref currentProgress,
                    ref frontCardsRendered,
                    progress);

                var backSlotCount = pagePlan.FilledBackType.HasValue ? cardsPerSheet : pagePlan.FrontCards.Count;
                var backTypes = new CardType[backSlotCount];
                for (var slotIndex = 0; slotIndex < backSlotCount; slotIndex++)
                {
                    backTypes[slotIndex] = pagePlan.FilledBackType ?? pagePlan.FrontCards[slotIndex].CardType;
                }

                var backPath = Path.Combine(outputDirectory, $"{deck.Id}_{paperSpec.Name}_{dpi}dpi_back_{pageIndex + 1:000}.png");
                WriteStreamingBackSheet(
                    backPath,
                    deck,
                    backTypes,
                    backBuffers,
                    sheetWidth,
                    sheetHeight,
                    cardWidth,
                    cardHeight,
                    columns,
                    rows,
                    xGap,
                    yGap,
                    dpi,
                    includeMeasurementGuide,
                    backMirror,
                    pageIndex,
                    pagePlans.Count,
                    totalProgress,
                    ref currentProgress,
                    progress);
            }
        }
        catch (Exception exception)
        {
            return ToolResult.Fail($"Memory-safe sheet export failed: {exception.Message}");
        }

        var modeDescription = easyPrintBacks ? " easy-back" : string.Empty;
        return ToolResult.Ok($"Exported {pagePlans.Count} {paperSpec.Name.ToUpperInvariant()} {dpi} DPI{modeDescription} front/back sheet pair(s) with memory-safe streaming for deck '{deck.Id}' to {outputDirectory}.");
    }

    private static void WriteStreamingFrontSheet(
        string outputPath,
        CardDeckResource deck,
        IReadOnlyList<CardResource> cards,
        int sheetWidth,
        int sheetHeight,
        int cardWidth,
        int cardHeight,
        int columns,
        int rows,
        int xGap,
        int yGap,
        int dpi,
        bool includeMeasurementGuide,
        int totalFrontCards,
        int totalProgress,
        ref int currentProgress,
        ref int frontCardsRendered,
        Action<ExportProgress>? progress)
    {
        using var writer = new StreamingPngWriter(outputPath, sheetWidth, sheetHeight);
        var scanline = new byte[checked(sheetWidth * 4)];
        var activeRow = int.MinValue;
        CardPixelBuffer[] rowBuffers = [];
        for (var outputY = 0; outputY < sheetHeight; outputY++)
        {
            var layoutRow = GetCardLayoutRow(outputY, rows, cardHeight, yGap);
            if (layoutRow != activeRow)
            {
                if (rowBuffers.Length > 0 && (long)cardWidth * cardHeight * 4 >= 32L * 1024 * 1024)
                {
                    rowBuffers = [];
                    GC.Collect(2, GCCollectionMode.Forced, blocking: true, compacting: false);
                }

                activeRow = layoutRow;
                rowBuffers = layoutRow < 0
                    ? []
                    : RenderFrontCardRow(cards, layoutRow, columns, cardWidth, cardHeight, deck);
                foreach (var buffer in rowBuffers)
                {
                    currentProgress++;
                    frontCardsRendered++;
                    progress?.Invoke(new ExportProgress(currentProgress, totalProgress, $"Streamed front {frontCardsRendered}/{totalFrontCards}: {buffer.Label}"));
                }
            }

            Array.Fill(scanline, (byte)255);
            if (layoutRow >= 0)
            {
                var localY = outputY - GetCardRowTop(layoutRow, cardHeight, yGap);
                for (var column = 0; column < rowBuffers.Length; column++)
                {
                    BlendCardRow(scanline, rowBuffers[column].Pixels, localY, xGap + column * (cardWidth + xGap), cardWidth, flipX: false);
                }
            }

            if (includeMeasurementGuide)
            {
                ApplyMeasurementGuideToRow(scanline, outputY, sheetWidth, sheetHeight, dpi);
            }

            writer.WriteRow(scanline);
        }

        writer.Complete();
    }

    private static void WriteStreamingBackSheet(
        string outputPath,
        CardDeckResource deck,
        IReadOnlyList<CardType> cardTypes,
        Dictionary<CardType, CardPixelBuffer> backBuffers,
        int sheetWidth,
        int sheetHeight,
        int cardWidth,
        int cardHeight,
        int columns,
        int rows,
        int xGap,
        int yGap,
        int dpi,
        bool includeMeasurementGuide,
        string backMirror,
        int pageIndex,
        int pageCount,
        int totalProgress,
        ref int currentProgress,
        Action<ExportProgress>? progress)
    {
        var flipX = backMirror is "width" or "both";
        var flipY = backMirror is "height" or "both";
        using var writer = new StreamingPngWriter(outputPath, sheetWidth, sheetHeight);
        var scanline = new byte[checked(sheetWidth * 4)];
        var activeRow = int.MinValue;
        CardPixelBuffer[] rowBuffers = [];
        for (var outputY = 0; outputY < sheetHeight; outputY++)
        {
            var sourceY = flipY ? sheetHeight - 1 - outputY : outputY;
            var layoutRow = GetCardLayoutRow(sourceY, rows, cardHeight, yGap);
            if (layoutRow != activeRow)
            {
                activeRow = layoutRow;
                rowBuffers = layoutRow < 0
                    ? []
                    : GetBackCardRow(cardTypes, layoutRow, columns, cardWidth, cardHeight, deck, backBuffers);
                foreach (var buffer in rowBuffers)
                {
                    currentProgress++;
                    progress?.Invoke(new ExportProgress(currentProgress, totalProgress, $"Streamed back {buffer.Label} on page {pageIndex + 1}/{pageCount}."));
                }
            }

            Array.Fill(scanline, (byte)255);
            if (layoutRow >= 0)
            {
                var localY = sourceY - GetCardRowTop(layoutRow, cardHeight, yGap);
                for (var column = 0; column < rowBuffers.Length; column++)
                {
                    var sourceX = xGap + column * (cardWidth + xGap);
                    var destinationX = flipX ? sheetWidth - sourceX - cardWidth : sourceX;
                    BlendCardRow(scanline, rowBuffers[column].Pixels, localY, destinationX, cardWidth, flipX);
                }
            }

            // The guide is added after mirroring in the existing in-memory pipeline.
            if (includeMeasurementGuide)
            {
                ApplyMeasurementGuideToRow(scanline, outputY, sheetWidth, sheetHeight, dpi);
            }

            writer.WriteRow(scanline);
        }

        writer.Complete();
    }

    private static CardPixelBuffer[] RenderFrontCardRow(
        IReadOnlyList<CardResource> cards,
        int layoutRow,
        int columns,
        int cardWidth,
        int cardHeight,
        CardDeckResource deck)
    {
        var firstSlot = layoutRow * columns;
        var count = Math.Min(columns, Math.Max(0, cards.Count - firstSlot));
        if (count == 0)
        {
            return [];
        }

        var buffers = new CardPixelBuffer[count];
        for (var index = 0; index < count; index++)
        {
            var card = cards[firstSlot + index];
            using var image = CardImageRenderer.Render(card, new Vector2I(cardWidth, cardHeight), deck.GetElementIconOverrides(), deck.PowerIconTexture);
            buffers[index] = new CardPixelBuffer(card.Id, image.GetData());
        }

        return buffers;
    }

    private static CardPixelBuffer[] GetBackCardRow(
        IReadOnlyList<CardType> cardTypes,
        int layoutRow,
        int columns,
        int cardWidth,
        int cardHeight,
        CardDeckResource deck,
        Dictionary<CardType, CardPixelBuffer> cache)
    {
        var firstSlot = layoutRow * columns;
        var count = Math.Min(columns, Math.Max(0, cardTypes.Count - firstSlot));
        var buffers = new CardPixelBuffer[count];
        for (var index = 0; index < count; index++)
        {
            var cardType = cardTypes[firstSlot + index];
            if (!cache.TryGetValue(cardType, out var buffer))
            {
                using var image = CardImageRenderer.RenderBackResized(
                    cardType,
                    deck.GetBackImageTexture(cardType),
                    deck.GetBackImageSourcePath(cardType),
                    deck.GetBackImageScaleMode(cardType),
                    cardWidth,
                    cardHeight);
                buffer = new CardPixelBuffer(cardType.ToString(), image.GetData());
                cache[cardType] = buffer;
            }

            buffers[index] = buffer;
        }

        return buffers;
    }

    private static int GetCardLayoutRow(int y, int rows, int cardHeight, int yGap)
    {
        for (var row = 0; row < rows; row++)
        {
            var top = GetCardRowTop(row, cardHeight, yGap);
            if (y >= top && y < top + cardHeight)
            {
                return row;
            }
        }

        return -1;
    }

    private static int GetCardRowTop(int row, int cardHeight, int yGap)
    {
        return yGap + row * (cardHeight + yGap);
    }

    private static void BlendCardRow(byte[] destination, byte[] cardPixels, int cardY, int destinationX, int cardWidth, bool flipX)
    {
        var sourceOffset = checked(cardY * cardWidth * 4);
        for (var sourceX = 0; sourceX < cardWidth; sourceX++)
        {
            var sourceIndex = sourceOffset + sourceX * 4;
            var outputX = destinationX + (flipX ? cardWidth - 1 - sourceX : sourceX);
            var destinationIndex = outputX * 4;
            var alpha = cardPixels[sourceIndex + 3];
            if (alpha == 0)
            {
                continue;
            }

            if (alpha == 255)
            {
                destination[destinationIndex] = cardPixels[sourceIndex];
                destination[destinationIndex + 1] = cardPixels[sourceIndex + 1];
                destination[destinationIndex + 2] = cardPixels[sourceIndex + 2];
            }
            else
            {
                var inverseAlpha = 255 - alpha;
                destination[destinationIndex] = (byte)((cardPixels[sourceIndex] * alpha + destination[destinationIndex] * inverseAlpha) / 255);
                destination[destinationIndex + 1] = (byte)((cardPixels[sourceIndex + 1] * alpha + destination[destinationIndex + 1] * inverseAlpha) / 255);
                destination[destinationIndex + 2] = (byte)((cardPixels[sourceIndex + 2] * alpha + destination[destinationIndex + 2] * inverseAlpha) / 255);
            }

            destination[destinationIndex + 3] = 255;
        }
    }

    private static void ApplyMeasurementGuideToRow(byte[] row, int y, int sheetWidth, int sheetHeight, int dpi)
    {
        var lineLength = MillimetersToPixels(MeasurementGuideLengthMillimeters, dpi);
        var centimeter = MillimetersToPixels(10.0, dpi);
        var lineThickness = Math.Max(1, MillimetersToPixels(0.35, dpi));
        var tickThickness = Math.Max(1, MillimetersToPixels(0.3, dpi));
        var tickHeight = Math.Max(2, MillimetersToPixels(4.0, dpi));
        var guideY = sheetHeight - MillimetersToPixels(6.0, dpi);
        var startX = Math.Max(0, (sheetWidth - lineLength) / 2);
        var endX = Math.Min(sheetWidth - 1, startX + lineLength);

        if (y >= guideY - lineThickness / 2 && y < guideY - lineThickness / 2 + lineThickness)
        {
            PaintGuideRange(row, startX, endX);
        }

        if (y < guideY - tickHeight || y >= guideY + tickHeight)
        {
            return;
        }

        for (var index = 0; index <= 10; index++)
        {
            var x = Math.Min(sheetWidth - tickThickness, startX + index * centimeter);
            PaintGuideRange(row, x, x + tickThickness);
        }
    }

    private static void PaintGuideRange(byte[] row, int startX, int endX)
    {
        for (var x = startX; x < endX; x++)
        {
            var offset = x * 4;
            row[offset] = 5;
            row[offset + 1] = 5;
            row[offset + 2] = 5;
            row[offset + 3] = 255;
        }
    }

    private static bool ShouldUseStreamingExport(long sheetBytes)
    {
        var availableMemory = GC.GetGCMemoryInfo().TotalAvailableMemoryBytes;
        var memoryAwareThreshold = availableMemory > 0
            ? Math.Min(StreamingSheetThresholdBytes, Math.Max(64L * 1024 * 1024, availableMemory / 4))
            : StreamingSheetThresholdBytes;
        return sheetBytes > memoryAwareThreshold
            || string.Equals(System.Environment.GetEnvironmentVariable("CONQUORA_FORCE_STREAMING_SHEETS"), "1", StringComparison.Ordinal);
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

    private static Image GetBackImage(CardType cardType, CardDeckResource deck, int width, int height, Dictionary<CardType, Image> backImages)
    {
        if (!backImages.TryGetValue(cardType, out var backImage))
        {
            backImage = CardImageRenderer.RenderBackResized(
                cardType,
                deck.GetBackImageTexture(cardType),
                deck.GetBackImageSourcePath(cardType),
                deck.GetBackImageScaleMode(cardType),
                width,
                height);
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

    private sealed record CardPixelBuffer(string Label, byte[] Pixels);
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
