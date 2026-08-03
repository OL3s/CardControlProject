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
    private const long StreamingSheetThresholdBytes = 256L * 1024 * 1024;

    public ToolResult ExportSheet(CardDeckResource deck, string outputPath, string paper, int dpi, string backMirror = "none", bool includeMeasurementGuide = false, Action<ExportProgress>? progress = null, bool easyPrintBacks = false, double printCompensationPercent = PrintSheetLayout.DefaultCompensationPercent)
    {
        if (!IsSupportedDpi(dpi))
        {
            return ToolResult.Fail($"DPI '{dpi}' is not supported. Use one of: {string.Join(", ", SupportedDpiValues)}.");
        }

        if (!IsSupportedBackMirror(backMirror))
        {
            return ToolResult.Fail($"Back mirror '{backMirror}' is not supported. Use none, width, height, or both.");
        }

        if (!PrintSheetLayout.TryCreate(paper, dpi, printCompensationPercent, includeMeasurementGuide, out var layout, out var layoutError))
        {
            return ToolResult.Fail(layoutError);
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

        var pagePlans = CreatePagePlans(cards, layout.CardsPerSheet, easyPrintBacks);
        var sheetCount = pagePlans.Count;
        var sheetBytes = checked((long)layout.SheetSize.X * layout.SheetSize.Y * 4);
        if (ShouldUseStreamingExport(sheetBytes))
        {
            progress?.Invoke(new ExportProgress(0, 1, $"Using memory-safe streaming for {layout.PaperName.ToUpperInvariant()} {dpi} DPI sheets."));
            return ExportStreamingSheets(
                deck,
                outputDirectory,
                easyPrintBacks ? "none" : backMirror,
                easyPrintBacks,
                cards.Count,
                pagePlans,
                layout,
                progress);
        }

        var backImages = new Dictionary<CardType, Image>();
        var totalBackCards = easyPrintBacks ? sheetCount * layout.CardsPerSheet : cards.Count;
        var totalProgress = cards.Count + totalBackCards;
        var currentProgress = 0;
        var frontCardsPlaced = 0;

        try
        {
            for (var sheetIndex = 0; sheetIndex < sheetCount; sheetIndex++)
            {
                var pagePlan = pagePlans[sheetIndex];
                using (var frontSheet = CreatePrintSheet(layout.SheetSize.X, layout.SheetSize.Y))
                {
                    for (var slotIndex = 0; slotIndex < pagePlan.FrontCards.Count; slotIndex++)
                    {
                        var card = pagePlan.FrontCards[slotIndex];
                        progress?.Invoke(new ExportProgress(currentProgress, totalProgress, $"Rendering front {frontCardsPlaced + 1}/{cards.Count}: {card.Id}"));
                        var position = layout.GetSlotRect(slotIndex).Position;
                        using var frontImage = CardImageRenderer.RenderPrint(card, layout.CardSize, layout.TrimRect, deck.GetElementIconOverrides(), deck.PowerIconTexture);
                        frontSheet.BlendRect(frontImage, new Rect2I(Vector2I.Zero, frontImage.GetSize()), position);
                        currentProgress++;
                        frontCardsPlaced++;
                        progress?.Invoke(new ExportProgress(currentProgress, totalProgress, $"Placed front {frontCardsPlaced}/{cards.Count}: {card.Id}"));
                    }

                    var frontPath = Path.Combine(outputDirectory, $"{deck.Id}_{layout.PaperName}_{dpi}dpi_front_{sheetIndex + 1:000}.png");
                    if (includeMeasurementGuide)
                    {
                        DrawMeasurementGuide(frontSheet, layout);
                    }

                    var frontError = frontSheet.SavePng(frontPath);
                    if (frontError != Error.Ok)
                    {
                        return ToolResult.Fail($"Failed to save front sheet {frontPath}: {frontError}.");
                    }
                }

                using (var backSheet = CreatePrintSheet(layout.SheetSize.X, layout.SheetSize.Y))
                {
                    var backSlotCount = pagePlan.FilledBackType.HasValue ? layout.CardsPerSheet : pagePlan.FrontCards.Count;
                    for (var slotIndex = 0; slotIndex < backSlotCount; slotIndex++)
                    {
                        var cardType = pagePlan.FilledBackType ?? pagePlan.FrontCards[slotIndex].CardType;
                        progress?.Invoke(new ExportProgress(currentProgress, totalProgress, $"Placing back slot {slotIndex + 1}/{backSlotCount} on page {sheetIndex + 1}/{sheetCount}"));
                        var position = layout.GetSlotRect(slotIndex).Position;
                        var backImage = GetBackImage(cardType, deck, layout, backImages);
                        backSheet.BlendRect(backImage, new Rect2I(Vector2I.Zero, backImage.GetSize()), position);
                        currentProgress++;
                        progress?.Invoke(new ExportProgress(currentProgress, totalProgress, $"Placed back slot {slotIndex + 1}/{backSlotCount} on page {sheetIndex + 1}/{sheetCount}"));
                    }

                    ApplyBackMirror(backSheet, easyPrintBacks ? "none" : backMirror);

                    if (includeMeasurementGuide)
                    {
                        DrawMeasurementGuide(backSheet, layout);
                    }

                    var backPath = Path.Combine(outputDirectory, $"{deck.Id}_{layout.PaperName}_{dpi}dpi_back_{sheetIndex + 1:000}.png");
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
        return ToolResult.Ok($"Exported {sheetCount} {layout.PaperName.ToUpperInvariant()} {dpi} DPI{modeDescription} front/back sheet pair(s) at {layout.CompensationPercent:0.#}% print compensation for deck '{deck.Id}' to {outputDirectory}.");
    }

    public IReadOnlyList<SheetPreviewPage>? RenderSheetPreviews(CardDeckResource deck, string paper, int dpi, string backMirror, bool includeMeasurementGuide, bool easyPrintBacks, out string errorMessage, Action<ExportProgress>? progress = null, double printCompensationPercent = PrintSheetLayout.DefaultCompensationPercent)
    {
        errorMessage = string.Empty;
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
        if (!PrintSheetLayout.TryCreate(paper, previewDpi, printCompensationPercent, includeMeasurementGuide, out var layout, out errorMessage))
        {
            return null;
        }

        var pagePlans = CreatePagePlans(cards, layout.CardsPerSheet, easyPrintBacks);
        var sheetCount = pagePlans.Count;
        var totalBackCards = easyPrintBacks ? sheetCount * layout.CardsPerSheet : cards.Count;
        var totalProgress = cards.Count + totalBackCards;
        var currentProgress = 0;
        var frontCardsRendered = 0;
        var pages = new List<SheetPreviewPage>(sheetCount);
        var backImages = new Dictionary<CardType, Image>();

        try
        {
            for (var sheetIndex = 0; sheetIndex < sheetCount; sheetIndex++)
            {
                Image? frontSheet = CreatePrintSheet(layout.SheetSize.X, layout.SheetSize.Y);
                Image? backSheet = CreatePrintSheet(layout.SheetSize.X, layout.SheetSize.Y);
                try
                {
                    var pagePlan = pagePlans[sheetIndex];
                    for (var slotIndex = 0; slotIndex < pagePlan.FrontCards.Count; slotIndex++)
                    {
                        var card = pagePlan.FrontCards[slotIndex];
                        progress?.Invoke(new ExportProgress(currentProgress, totalProgress, $"Rendering preview front {frontCardsRendered + 1}/{cards.Count}: {card.Id}"));
                        var position = layout.GetSlotRect(slotIndex).Position;
                        var frontImage = CardImageRenderer.RenderPrint(card, layout.CardSize, layout.TrimRect, deck.GetElementIconOverrides(), deck.PowerIconTexture);
                        frontSheet.BlendRect(frontImage, new Rect2I(Vector2I.Zero, frontImage.GetSize()), position);
                        frontImage.Dispose();
                        currentProgress++;
                        frontCardsRendered++;
                        progress?.Invoke(new ExportProgress(currentProgress, totalProgress, $"Rendered preview front {frontCardsRendered}/{cards.Count}: {card.Id}"));
                    }

                    var backSlotCount = pagePlan.FilledBackType.HasValue ? layout.CardsPerSheet : pagePlan.FrontCards.Count;
                    for (var slotIndex = 0; slotIndex < backSlotCount; slotIndex++)
                    {
                        var cardType = pagePlan.FilledBackType ?? pagePlan.FrontCards[slotIndex].CardType;
                        progress?.Invoke(new ExportProgress(currentProgress, totalProgress, $"Rendering preview back {slotIndex + 1}/{backSlotCount} on page {sheetIndex + 1}/{sheetCount}"));
                        var position = layout.GetSlotRect(slotIndex).Position;
                        var backImage = GetBackImage(cardType, deck, layout, backImages);
                        backSheet.BlendRect(backImage, new Rect2I(Vector2I.Zero, backImage.GetSize()), position);
                        currentProgress++;
                        progress?.Invoke(new ExportProgress(currentProgress, totalProgress, $"Rendered preview back {slotIndex + 1}/{backSlotCount} on page {sheetIndex + 1}/{sheetCount}"));
                    }

                    ApplyBackMirror(backSheet, easyPrintBacks ? "none" : backMirror);
                    if (includeMeasurementGuide)
                    {
                        DrawMeasurementGuide(frontSheet, layout);
                        DrawMeasurementGuide(backSheet, layout);
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
        string backMirror,
        bool easyPrintBacks,
        int totalFrontCards,
        IReadOnlyList<SheetPagePlan> pagePlans,
        PrintSheetLayout layout,
        Action<ExportProgress>? progress)
    {
        var totalBackCards = easyPrintBacks ? pagePlans.Count * layout.CardsPerSheet : totalFrontCards;
        var totalProgress = totalFrontCards + totalBackCards;
        var currentProgress = 0;
        var frontCardsRendered = 0;
        var backBuffers = new Dictionary<CardType, CardPixelBuffer>();
        try
        {
            for (var pageIndex = 0; pageIndex < pagePlans.Count; pageIndex++)
            {
                var pagePlan = pagePlans[pageIndex];
                var frontPath = Path.Combine(outputDirectory, $"{deck.Id}_{layout.PaperName}_{layout.Dpi}dpi_front_{pageIndex + 1:000}.png");
                WriteStreamingFrontSheet(
                    frontPath,
                    deck,
                    pagePlan.FrontCards,
                    layout,
                    totalFrontCards,
                    totalProgress,
                    ref currentProgress,
                    ref frontCardsRendered,
                    progress);

                var backSlotCount = pagePlan.FilledBackType.HasValue ? layout.CardsPerSheet : pagePlan.FrontCards.Count;
                var backTypes = new CardType[backSlotCount];
                for (var slotIndex = 0; slotIndex < backSlotCount; slotIndex++)
                {
                    backTypes[slotIndex] = pagePlan.FilledBackType ?? pagePlan.FrontCards[slotIndex].CardType;
                }

                var backPath = Path.Combine(outputDirectory, $"{deck.Id}_{layout.PaperName}_{layout.Dpi}dpi_back_{pageIndex + 1:000}.png");
                WriteStreamingBackSheet(
                    backPath,
                    deck,
                    backTypes,
                    backBuffers,
                    layout,
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
        return ToolResult.Ok($"Exported {pagePlans.Count} {layout.PaperName.ToUpperInvariant()} {layout.Dpi} DPI{modeDescription} front/back sheet pair(s) at {layout.CompensationPercent:0.#}% print compensation with memory-safe streaming for deck '{deck.Id}' to {outputDirectory}.");
    }

    private static void WriteStreamingFrontSheet(
        string outputPath,
        CardDeckResource deck,
        IReadOnlyList<CardResource> cards,
        PrintSheetLayout layout,
        int totalFrontCards,
        int totalProgress,
        ref int currentProgress,
        ref int frontCardsRendered,
        Action<ExportProgress>? progress)
    {
        using var writer = new StreamingPngWriter(outputPath, layout.SheetSize.X, layout.SheetSize.Y);
        var scanline = new byte[checked(layout.SheetSize.X * 4)];
        var activeRow = int.MinValue;
        CardPixelBuffer[] rowBuffers = [];
        for (var outputY = 0; outputY < layout.SheetSize.Y; outputY++)
        {
            var layoutRow = layout.GetLayoutRow(outputY);
            if (layoutRow != activeRow)
            {
                if (rowBuffers.Length > 0 && (long)layout.CardSize.X * layout.CardSize.Y * 4 >= 32L * 1024 * 1024)
                {
                    rowBuffers = [];
                    GC.Collect(2, GCCollectionMode.Forced, blocking: true, compacting: false);
                }

                activeRow = layoutRow;
                rowBuffers = layoutRow < 0
                    ? []
                    : RenderFrontCardRow(cards, layoutRow, layout, deck);
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
                var localY = outputY - layout.GetRowTop(layoutRow);
                for (var column = 0; column < rowBuffers.Length; column++)
                {
                    BlendCardRow(scanline, rowBuffers[column].Pixels, localY, layout.GetColumnLeft(column), layout.CardSize.X, flipX: false);
                }
            }

            if (layout.IncludeMeasurementGuide)
            {
                ApplyMeasurementGuideToRow(scanline, outputY, layout);
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
        PrintSheetLayout layout,
        string backMirror,
        int pageIndex,
        int pageCount,
        int totalProgress,
        ref int currentProgress,
        Action<ExportProgress>? progress)
    {
        var flipX = backMirror is "width" or "both";
        var flipY = backMirror is "height" or "both";
        using var writer = new StreamingPngWriter(outputPath, layout.SheetSize.X, layout.SheetSize.Y);
        var scanline = new byte[checked(layout.SheetSize.X * 4)];
        var activeRow = int.MinValue;
        CardPixelBuffer[] rowBuffers = [];
        for (var outputY = 0; outputY < layout.SheetSize.Y; outputY++)
        {
            var sourceY = flipY ? layout.SheetSize.Y - 1 - outputY : outputY;
            var layoutRow = layout.GetLayoutRow(sourceY);
            if (layoutRow != activeRow)
            {
                activeRow = layoutRow;
                rowBuffers = layoutRow < 0
                    ? []
                    : GetBackCardRow(cardTypes, layoutRow, layout, deck, backBuffers);
                foreach (var buffer in rowBuffers)
                {
                    currentProgress++;
                    progress?.Invoke(new ExportProgress(currentProgress, totalProgress, $"Streamed back {buffer.Label} on page {pageIndex + 1}/{pageCount}."));
                }
            }

            Array.Fill(scanline, (byte)255);
            if (layoutRow >= 0)
            {
                var localY = sourceY - layout.GetRowTop(layoutRow);
                for (var column = 0; column < rowBuffers.Length; column++)
                {
                    var sourceX = layout.GetColumnLeft(column);
                    var destinationX = flipX ? layout.SheetSize.X - sourceX - layout.CardSize.X : sourceX;
                    BlendCardRow(scanline, rowBuffers[column].Pixels, localY, destinationX, layout.CardSize.X, flipX);
                }
            }

            // The guide is added after mirroring in the existing in-memory pipeline.
            if (layout.IncludeMeasurementGuide)
            {
                ApplyMeasurementGuideToRow(scanline, outputY, layout);
            }

            writer.WriteRow(scanline);
        }

        writer.Complete();
    }

    private static CardPixelBuffer[] RenderFrontCardRow(
        IReadOnlyList<CardResource> cards,
        int layoutRow,
        PrintSheetLayout layout,
        CardDeckResource deck)
    {
        var firstSlot = layoutRow * layout.Columns;
        var count = Math.Min(layout.Columns, Math.Max(0, cards.Count - firstSlot));
        if (count == 0)
        {
            return [];
        }

        var buffers = new CardPixelBuffer[count];
        for (var index = 0; index < count; index++)
        {
            var card = cards[firstSlot + index];
            using var image = CardImageRenderer.RenderPrint(card, layout.CardSize, layout.TrimRect, deck.GetElementIconOverrides(), deck.PowerIconTexture);
            buffers[index] = new CardPixelBuffer(card.Id, image.GetData());
        }

        return buffers;
    }

    private static CardPixelBuffer[] GetBackCardRow(
        IReadOnlyList<CardType> cardTypes,
        int layoutRow,
        PrintSheetLayout layout,
        CardDeckResource deck,
        Dictionary<CardType, CardPixelBuffer> cache)
    {
        var firstSlot = layoutRow * layout.Columns;
        var count = Math.Min(layout.Columns, Math.Max(0, cardTypes.Count - firstSlot));
        var buffers = new CardPixelBuffer[count];
        for (var index = 0; index < count; index++)
        {
            var cardType = cardTypes[firstSlot + index];
            if (!cache.TryGetValue(cardType, out var buffer))
            {
                using var image = CardImageRenderer.RenderBackPrint(
                    cardType,
                    deck.GetBackImageTexture(cardType),
                    deck.GetBackImageSourcePath(cardType),
                    deck.GetBackImageScaleMode(cardType),
                    layout.CardSize,
                    layout.TrimRect);
                buffer = new CardPixelBuffer(cardType.ToString(), image.GetData());
                cache[cardType] = buffer;
            }

            buffers[index] = buffer;
        }

        return buffers;
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

    private static void ApplyMeasurementGuideToRow(byte[] row, int y, PrintSheetLayout layout)
    {
        var lineLength = layout.GetMeasurementGuideLength();
        var centimeter = lineLength / 10.0;
        var lineThickness = Math.Max(1, PrintSheetLayout.MillimetersToPixels(0.35 * layout.CompensationScale, layout.Dpi));
        var tickThickness = Math.Max(1, PrintSheetLayout.MillimetersToPixels(0.3 * layout.CompensationScale, layout.Dpi));
        var tickHeight = Math.Max(2, PrintSheetLayout.MillimetersToPixels(4.0 * layout.CompensationScale, layout.Dpi));
        var guideY = layout.SheetSize.Y - PrintSheetLayout.MillimetersToPixels(6.0, layout.Dpi);
        var startX = Math.Max(0, (layout.SheetSize.X - lineLength) / 2);
        var endX = Math.Min(layout.SheetSize.X - 1, startX + lineLength);

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
            var x = Math.Min(layout.SheetSize.X - tickThickness, startX + (int)Math.Round(index * centimeter));
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

    private static void DrawMeasurementGuide(Image sheet, PrintSheetLayout layout)
    {
        var lineLength = layout.GetMeasurementGuideLength();
        var centimeter = lineLength / 10.0;
        var lineThickness = Math.Max(1, PrintSheetLayout.MillimetersToPixels(0.35 * layout.CompensationScale, layout.Dpi));
        var tickThickness = Math.Max(1, PrintSheetLayout.MillimetersToPixels(0.3 * layout.CompensationScale, layout.Dpi));
        var tickHeight = Math.Max(2, PrintSheetLayout.MillimetersToPixels(4.0 * layout.CompensationScale, layout.Dpi));
        var y = layout.SheetSize.Y - PrintSheetLayout.MillimetersToPixels(6.0, layout.Dpi);
        var startX = Math.Max(0, (layout.SheetSize.X - lineLength) / 2);
        var endX = Math.Min(layout.SheetSize.X - 1, startX + lineLength);
        var color = new Color(0.02f, 0.02f, 0.02f, 1);

        sheet.FillRect(new Rect2I(startX, y - lineThickness / 2, Math.Max(1, endX - startX), lineThickness), color);

        for (var index = 0; index <= 10; index++)
        {
            var x = Math.Min(layout.SheetSize.X - tickThickness, startX + (int)Math.Round(index * centimeter));
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

    private static Image GetBackImage(CardType cardType, CardDeckResource deck, PrintSheetLayout layout, Dictionary<CardType, Image> backImages)
    {
        if (!backImages.TryGetValue(cardType, out var backImage))
        {
            backImage = CardImageRenderer.RenderBackPrint(
                cardType,
                deck.GetBackImageTexture(cardType),
                deck.GetBackImageSourcePath(cardType),
                deck.GetBackImageScaleMode(cardType),
                layout.CardSize,
                layout.TrimRect);
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
