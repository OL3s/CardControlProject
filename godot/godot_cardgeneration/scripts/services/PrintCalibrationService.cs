using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using CardGeneration.App;
using Godot;

namespace CardGeneration.Services;

public sealed class PrintCalibrationService
{
    public const int ExportDpi = 300;
    private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;
    private static readonly IReadOnlyDictionary<char, string[]> Glyphs = CreateGlyphs();

    public ToolResult Export(string outputPath, string paper, double printCompensationPercent)
    {
        var outputDirectory = ProjectPaths.ToGlobalPath(outputPath);
        if (Path.GetExtension(outputDirectory).Equals(".png", StringComparison.OrdinalIgnoreCase))
        {
            return ToolResult.Fail("Print calibration export requires an output directory, not a .png file path.");
        }

        var pages = RenderPages(paper, ExportDpi, printCompensationPercent, out var errorMessage);
        if (pages is null)
        {
            return ToolResult.Fail(errorMessage);
        }

        Directory.CreateDirectory(outputDirectory);
        try
        {
            var frontPath = Path.Combine(outputDirectory, $"conquora_print_test_{paper.ToLowerInvariant()}_front.png");
            var backPath = Path.Combine(outputDirectory, $"conquora_print_test_{paper.ToLowerInvariant()}_back.png");
            var frontError = pages[0].Image.SavePng(frontPath);
            if (frontError != Error.Ok)
            {
                return ToolResult.Fail($"Failed to save print calibration front page {frontPath}: {frontError}.");
            }
            var frontMetadataError = TrySetDpi(frontPath);
            if (frontMetadataError is not null)
            {
                return ToolResult.Fail(frontMetadataError);
            }

            var backError = pages[1].Image.SavePng(backPath);
            if (backError != Error.Ok)
            {
                return ToolResult.Fail($"Failed to save print calibration back page {backPath}: {backError}.");
            }
            var backMetadataError = TrySetDpi(backPath);
            if (backMetadataError is not null)
            {
                return ToolResult.Fail(backMetadataError);
            }

            return ToolResult.Ok($"Exported two-page {paper.ToUpperInvariant()} print calibration test at {printCompensationPercent:0.#}% compensation to {outputDirectory}.");
        }
        finally
        {
            DisposePages(pages);
        }
    }

    private static string? TrySetDpi(string outputPath)
    {
        try
        {
            StreamingPngWriter.SetDpi(outputPath, ExportDpi);
            return null;
        }
        catch (Exception exception)
        {
            return $"Saved calibration sheet but failed to write {ExportDpi} DPI metadata to {outputPath}: {exception.Message}";
        }
    }

    public IReadOnlyList<ImagePreviewItem>? RenderPreviews(string paper, double printCompensationPercent, out string errorMessage)
    {
        return RenderPages(paper, 150, printCompensationPercent, out errorMessage);
    }

    private static IReadOnlyList<ImagePreviewItem>? RenderPages(string paper, int dpi, double printCompensationPercent, out string errorMessage)
    {
        errorMessage = string.Empty;
        if (!PrintSheetLayout.TryCreate(paper, dpi, printCompensationPercent, includeMeasurementGuide: true, out var layout, out errorMessage))
        {
            return null;
        }

        if (layout.Columns < 2 || layout.Rows < 2)
        {
            errorMessage = "Print calibration requires at least a 2 x 2 card grid.";
            return null;
        }

        var front = RenderSvgPage(layout, isFront: true, out errorMessage);
        if (front is null)
        {
            return null;
        }

        var back = RenderSvgPage(layout, isFront: false, out errorMessage);
        if (back is null)
        {
            front.Dispose();
            return null;
        }

        return
        [
            new ImagePreviewItem("Calibration front", front),
            new ImagePreviewItem("Calibration back (unmirrored)", back)
        ];
    }

    private static Image? RenderSvgPage(PrintSheetLayout layout, bool isFront, out string errorMessage)
    {
        var svg = BuildSvg(layout);
        var image = new Image();
        var error = image.LoadSvgFromString(svg);
        if (error == Error.Ok)
        {
            DrawTextOverlay(image, layout, isFront);
            errorMessage = string.Empty;
            return image;
        }

        image.Dispose();
        errorMessage = $"Could not render print calibration SVG: {error}.";
        return null;
    }

    private static string BuildSvg(PrintSheetLayout layout)
    {
        var svg = new StringBuilder();
        svg.Append("<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"")
            .Append(layout.SheetSize.X)
            .Append("\" height=\"")
            .Append(layout.SheetSize.Y)
            .Append("\" viewBox=\"0 0 ")
            .Append(layout.SheetSize.X)
            .Append(' ')
            .Append(layout.SheetSize.Y)
            .Append("\">");
        svg.Append("<rect width=\"100%\" height=\"100%\" fill=\"white\"/>");

        var omittedSlot = layout.CardsPerSheet - 1;
        for (var slotIndex = 0; slotIndex < omittedSlot; slotIndex++)
        {
            var slot = layout.GetSlotRect(slotIndex);
            DrawDashedRect(svg, slot, Math.Max(1, PrintSheetLayout.MillimetersToPixels(0.3, layout.Dpi)));
            var trim = new Rect2I(slot.Position + layout.TrimRect.Position, layout.TrimRect.Size);
            DrawSolidRect(svg, trim, Math.Max(1, PrintSheetLayout.MillimetersToPixels(0.45, layout.Dpi)));
        }

        DrawMeasurementGuide(svg, layout);
        svg.Append("</svg>");
        return svg.ToString();
    }

    private static void DrawMeasurementGuide(StringBuilder svg, PrintSheetLayout layout)
    {
        var length = layout.GetMeasurementGuideLength();
        var startX = (layout.SheetSize.X - length) / 2;
        var y = layout.SheetSize.Y - PrintSheetLayout.MillimetersToPixels(6.0, layout.Dpi);
        var thickness = Math.Max(1, PrintSheetLayout.MillimetersToPixels(0.35 * layout.CompensationScale, layout.Dpi));
        var tickHeight = Math.Max(2, PrintSheetLayout.MillimetersToPixels(4.0 * layout.CompensationScale, layout.Dpi));
        AppendLine(svg, startX, y, startX + length, y, thickness);
        for (var index = 0; index <= 10; index++)
        {
            var x = startX + (int)Math.Round(length * index / 10.0);
            AppendLine(svg, x, y - tickHeight, x, y + tickHeight, thickness);
        }

    }

    private static void DrawDashedRect(StringBuilder svg, Rect2I rect, int strokeWidth)
    {
        var halfStroke = strokeWidth / 2.0;
        svg.Append("<rect x=\"").Append((rect.Position.X + halfStroke).ToString("0.###", Invariant))
            .Append("\" y=\"").Append((rect.Position.Y + halfStroke).ToString("0.###", Invariant))
            .Append("\" width=\"").Append((rect.Size.X - strokeWidth).ToString("0.###", Invariant))
            .Append("\" height=\"").Append((rect.Size.Y - strokeWidth).ToString("0.###", Invariant))
            .Append("\" fill=\"none\" stroke=\"black\" stroke-width=\"").Append(strokeWidth)
            .Append("\" stroke-dasharray=\"").Append(strokeWidth * 5).Append(' ').Append(strokeWidth * 4).Append("\"/>");
    }

    private static void DrawSolidRect(StringBuilder svg, Rect2I rect, int strokeWidth)
    {
        var halfStroke = strokeWidth / 2.0;
        svg.Append("<rect x=\"").Append((rect.Position.X + halfStroke).ToString("0.###", Invariant))
            .Append("\" y=\"").Append((rect.Position.Y + halfStroke).ToString("0.###", Invariant))
            .Append("\" width=\"").Append((rect.Size.X - strokeWidth).ToString("0.###", Invariant))
            .Append("\" height=\"").Append((rect.Size.Y - strokeWidth).ToString("0.###", Invariant))
            .Append("\" fill=\"none\" stroke=\"black\" stroke-width=\"").Append(strokeWidth).Append("\"/>");
    }

    private static void AppendLine(StringBuilder svg, int x1, int y1, int x2, int y2, int strokeWidth)
    {
        svg.Append("<line x1=\"").Append(x1).Append("\" y1=\"").Append(y1).Append("\" x2=\"").Append(x2)
            .Append("\" y2=\"").Append(y2).Append("\" stroke=\"black\" stroke-width=\"").Append(strokeWidth).Append("\"/>");
    }

    private static void DisposePages(IEnumerable<ImagePreviewItem> pages)
    {
        foreach (var page in pages)
        {
            page.Dispose();
        }
    }

    private static void DrawTextOverlay(Image image, PrintSheetLayout layout, bool isFront)
    {
        var omittedSlot = layout.CardsPerSheet - 1;
        for (var slotIndex = 0; slotIndex < omittedSlot; slotIndex++)
        {
            var slot = layout.GetSlotRect(slotIndex);
            var trim = new Rect2I(slot.Position + layout.TrimRect.Position, layout.TrimRect.Size);
            DrawBitmapText(image, trim, "63 MM X 88 MM", -0.8, layout);
            DrawBitmapText(image, trim, "BLEED 69 MM X 94 MM", 0.8, layout, 2.6);
        }

        if (isFront)
        {
            DrawBitmapText(image, layout.GetSlotRect(0), "BACK GAP HERE", 1.8, layout, 2.5);
            DrawBitmapText(image, layout.GetSlotRect(0), "SELECT BOTH", 2.8, layout, 3.4);
            DrawBitmapText(image, layout.GetSlotRect(layout.Columns - 1), "BACK GAP HERE", 1.8, layout, 2.5);
            DrawBitmapText(image, layout.GetSlotRect(layout.Columns - 1), "SELECT HEIGHT", 2.8, layout, 3.4);
            DrawBitmapText(image, layout.GetSlotRect((layout.Rows - 1) * layout.Columns), "BACK GAP HERE", 1.8, layout, 2.5);
            DrawBitmapText(image, layout.GetSlotRect((layout.Rows - 1) * layout.Columns), "SELECT WIDTH", 2.8, layout, 3.4);
            var missing = layout.GetSlotRect(omittedSlot);
            DrawBitmapText(image, missing, "NO CARD OUTLINE", -1.0, layout, 3.0);
            DrawBitmapText(image, missing, "BACK GAP HERE", 0.1, layout, 3.0);
            DrawBitmapText(image, missing, "SELECT NONE", 1.2, layout, 4.0);
        }
        else
        {
            var missing = layout.GetSlotRect(omittedSlot);
            DrawBitmapText(image, missing, "BACK GAP", -0.6, layout, 4.0);
            DrawBitmapText(image, missing, "UNMIRRORED", 1.2, layout, 2.8);
        }

        var guideY = layout.SheetSize.Y - PrintSheetLayout.MillimetersToPixels(14.0, layout.Dpi);
        var guideRect = new Rect2I(0, guideY, layout.SheetSize.X, PrintSheetLayout.MillimetersToPixels(3.5, layout.Dpi));
        DrawBitmapText(image, guideRect, $"TARGET 10 CM COMPENSATION {layout.CompensationPercent.ToString("0.#", Invariant)}%", 0, layout, 2.4);
    }

    private static void DrawBitmapText(Image image, Rect2I rect, string text, double lineOffset, PrintSheetLayout layout, double fontMillimeters = 3.2)
    {
        var requestedHeight = Math.Max(7, PrintSheetLayout.MillimetersToPixels(fontMillimeters, layout.Dpi));
        var scale = Math.Max(1, requestedHeight / 7);
        var advance = 6 * scale;
        var textWidth = Math.Max(0, text.Length * advance - scale);
        while (textWidth > rect.Size.X - scale * 2 && scale > 1)
        {
            scale--;
            advance = 6 * scale;
            textWidth = Math.Max(0, text.Length * advance - scale);
        }

        var textHeight = 7 * scale;
        var startX = rect.Position.X + (rect.Size.X - textWidth) / 2;
        var centerY = rect.Position.Y + rect.Size.Y / 2;
        var startY = centerY + (int)Math.Round(lineOffset * textHeight) - textHeight / 2;
        var color = new Color(0.02f, 0.02f, 0.02f, 1);
        for (var charIndex = 0; charIndex < text.Length; charIndex++)
        {
            if (!Glyphs.TryGetValue(char.ToUpperInvariant(text[charIndex]), out var glyph))
            {
                continue;
            }

            for (var row = 0; row < glyph.Length; row++)
            {
                for (var column = 0; column < glyph[row].Length; column++)
                {
                    if (glyph[row][column] != '1')
                    {
                        continue;
                    }

                    image.FillRect(new Rect2I(startX + charIndex * advance + column * scale, startY + row * scale, scale, scale), color);
                }
            }
        }
    }

    private static IReadOnlyDictionary<char, string[]> CreateGlyphs()
    {
        return new Dictionary<char, string[]>
        {
            [' '] = ["00000", "00000", "00000", "00000", "00000", "00000", "00000"],
            ['%'] = ["11001", "11010", "00100", "01000", "10011", "00011", "00000"],
            ['.'] = ["00000", "00000", "00000", "00000", "00000", "00110", "00110"],
            ['0'] = ["01110", "10001", "10011", "10101", "11001", "10001", "01110"],
            ['1'] = ["00100", "01100", "00100", "00100", "00100", "00100", "01110"],
            ['2'] = ["01110", "10001", "00001", "00010", "00100", "01000", "11111"],
            ['3'] = ["11110", "00001", "00001", "01110", "00001", "00001", "11110"],
            ['4'] = ["00010", "00110", "01010", "10010", "11111", "00010", "00010"],
            ['5'] = ["11111", "10000", "10000", "11110", "00001", "00001", "11110"],
            ['6'] = ["01110", "10000", "10000", "11110", "10001", "10001", "01110"],
            ['7'] = ["11111", "00001", "00010", "00100", "01000", "01000", "01000"],
            ['8'] = ["01110", "10001", "10001", "01110", "10001", "10001", "01110"],
            ['9'] = ["01110", "10001", "10001", "01111", "00001", "00001", "01110"],
            ['A'] = ["01110", "10001", "10001", "11111", "10001", "10001", "10001"],
            ['B'] = ["11110", "10001", "10001", "11110", "10001", "10001", "11110"],
            ['C'] = ["01111", "10000", "10000", "10000", "10000", "10000", "01111"],
            ['D'] = ["11110", "10001", "10001", "10001", "10001", "10001", "11110"],
            ['E'] = ["11111", "10000", "10000", "11110", "10000", "10000", "11111"],
            ['F'] = ["11111", "10000", "10000", "11110", "10000", "10000", "10000"],
            ['G'] = ["01111", "10000", "10000", "10111", "10001", "10001", "01110"],
            ['H'] = ["10001", "10001", "10001", "11111", "10001", "10001", "10001"],
            ['I'] = ["01110", "00100", "00100", "00100", "00100", "00100", "01110"],
            ['J'] = ["00001", "00001", "00001", "00001", "10001", "10001", "01110"],
            ['K'] = ["10001", "10010", "10100", "11000", "10100", "10010", "10001"],
            ['L'] = ["10000", "10000", "10000", "10000", "10000", "10000", "11111"],
            ['M'] = ["10001", "11011", "10101", "10101", "10001", "10001", "10001"],
            ['N'] = ["10001", "11001", "10101", "10011", "10001", "10001", "10001"],
            ['O'] = ["01110", "10001", "10001", "10001", "10001", "10001", "01110"],
            ['P'] = ["11110", "10001", "10001", "11110", "10000", "10000", "10000"],
            ['Q'] = ["01110", "10001", "10001", "10001", "10101", "10010", "01101"],
            ['R'] = ["11110", "10001", "10001", "11110", "10100", "10010", "10001"],
            ['S'] = ["01111", "10000", "10000", "01110", "00001", "00001", "11110"],
            ['T'] = ["11111", "00100", "00100", "00100", "00100", "00100", "00100"],
            ['U'] = ["10001", "10001", "10001", "10001", "10001", "10001", "01110"],
            ['V'] = ["10001", "10001", "10001", "10001", "10001", "01010", "00100"],
            ['W'] = ["10001", "10001", "10001", "10101", "10101", "10101", "01010"],
            ['X'] = ["10001", "10001", "01010", "00100", "01010", "10001", "10001"],
            ['Y'] = ["10001", "10001", "01010", "00100", "00100", "00100", "00100"],
            ['Z'] = ["11111", "00001", "00010", "00100", "01000", "10000", "11111"]
        };
    }
}
