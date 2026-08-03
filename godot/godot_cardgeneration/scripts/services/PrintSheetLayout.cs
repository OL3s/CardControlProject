using System;
using Godot;

namespace CardGeneration.Services;

public sealed class PrintSheetLayout
{
    public const double MillimetersPerInch = 25.4;
    public const double TrimWidthMillimeters = 63.0;
    public const double TrimHeightMillimeters = 88.0;
    public const double BleedMillimeters = 3.0;
    public const double ExportWidthMillimeters = TrimWidthMillimeters + BleedMillimeters * 2.0;
    public const double ExportHeightMillimeters = TrimHeightMillimeters + BleedMillimeters * 2.0;
    public const double MeasurementGuideHeightMillimeters = 14.0;
    public const double MeasurementGuideLengthMillimeters = 100.0;
    public const double MinCompensationPercent = 90.0;
    public const double MaxCompensationPercent = 110.0;
    public const double DefaultCompensationPercent = 100.0;

    private PrintSheetLayout(
        string paperName,
        int dpi,
        double compensationPercent,
        bool includeMeasurementGuide,
        Vector2I sheetSize,
        Vector2I cardSize,
        Rect2I trimRect,
        int columns,
        int rows,
        Vector2I gridOrigin)
    {
        PaperName = paperName;
        Dpi = dpi;
        CompensationPercent = compensationPercent;
        IncludeMeasurementGuide = includeMeasurementGuide;
        SheetSize = sheetSize;
        CardSize = cardSize;
        TrimRect = trimRect;
        Columns = columns;
        Rows = rows;
        GridOrigin = gridOrigin;
    }

    public string PaperName { get; }
    public int Dpi { get; }
    public double CompensationPercent { get; }
    public double CompensationScale => CompensationPercent / 100.0;
    public bool IncludeMeasurementGuide { get; }
    public Vector2I SheetSize { get; }
    public Vector2I CardSize { get; }
    public Rect2I TrimRect { get; }
    public int Columns { get; }
    public int Rows { get; }
    public int CardsPerSheet => Columns * Rows;
    public Vector2I GridOrigin { get; }

    public static bool TryCreate(
        string paper,
        int dpi,
        double compensationPercent,
        bool includeMeasurementGuide,
        out PrintSheetLayout layout,
        out string errorMessage)
    {
        layout = null!;
        errorMessage = string.Empty;
        if (!TryGetPaperSize(paper, out var paperName, out var paperWidthMillimeters, out var paperHeightMillimeters))
        {
            errorMessage = $"Paper '{paper}' is not supported. Use a4 or a3.";
            return false;
        }

        if (!double.IsFinite(compensationPercent)
            || compensationPercent < MinCompensationPercent
            || compensationPercent > MaxCompensationPercent)
        {
            errorMessage = $"Print compensation must be between {MinCompensationPercent:0.#}% and {MaxCompensationPercent:0.#}%.";
            return false;
        }

        var scale = compensationPercent / 100.0;
        var exportWidthMillimeters = ExportWidthMillimeters * scale;
        var exportHeightMillimeters = ExportHeightMillimeters * scale;
        var layoutHeightMillimeters = paperHeightMillimeters - (includeMeasurementGuide ? MeasurementGuideHeightMillimeters : 0.0);
        // Capacity is physical geometry and must not change when preview uses a lower DPI.
        var columns = Math.Max(1, (int)Math.Floor((paperWidthMillimeters + 0.000001) / exportWidthMillimeters));
        var rows = Math.Max(1, (int)Math.Floor((layoutHeightMillimeters + 0.000001) / exportHeightMillimeters));
        var sheetSize = new Vector2I(
            MillimetersToPixels(paperWidthMillimeters, dpi),
            MillimetersToPixels(paperHeightMillimeters, dpi));
        var guideHeight = includeMeasurementGuide ? MillimetersToPixels(MeasurementGuideHeightMillimeters, dpi) : 0;
        var layoutHeight = sheetSize.Y - guideHeight;
        var cardSize = new Vector2I(
            Math.Min(MillimetersToPixels(exportWidthMillimeters, dpi), sheetSize.X / columns),
            Math.Min(MillimetersToPixels(exportHeightMillimeters, dpi), layoutHeight / rows));
        var trimSize = new Vector2I(
            MillimetersToPixels(TrimWidthMillimeters * scale, dpi),
            MillimetersToPixels(TrimHeightMillimeters * scale, dpi));
        var trimRect = new Rect2I((cardSize - trimSize) / 2, trimSize);
        var gridSize = new Vector2I(columns * cardSize.X, rows * cardSize.Y);
        var gridOrigin = new Vector2I(
            (sheetSize.X - gridSize.X) / 2,
            (layoutHeight - gridSize.Y) / 2);

        layout = new PrintSheetLayout(
            paperName,
            dpi,
            compensationPercent,
            includeMeasurementGuide,
            sheetSize,
            cardSize,
            trimRect,
            columns,
            rows,
            gridOrigin);
        return true;
    }

    public Rect2I GetSlotRect(int slotIndex)
    {
        var column = slotIndex % Columns;
        var row = slotIndex / Columns;
        return new Rect2I(
            GridOrigin.X + column * CardSize.X,
            GridOrigin.Y + row * CardSize.Y,
            CardSize.X,
            CardSize.Y);
    }

    public int GetLayoutRow(int y)
    {
        if (y < GridOrigin.Y || y >= GridOrigin.Y + Rows * CardSize.Y)
        {
            return -1;
        }

        return (y - GridOrigin.Y) / CardSize.Y;
    }

    public int GetRowTop(int row)
    {
        return GridOrigin.Y + row * CardSize.Y;
    }

    public int GetColumnLeft(int column)
    {
        return GridOrigin.X + column * CardSize.X;
    }

    public int GetMeasurementGuideLength()
    {
        return MillimetersToPixels(MeasurementGuideLengthMillimeters * CompensationScale, Dpi);
    }

    public static int MillimetersToPixels(double millimeters, int dpi)
    {
        return (int)Math.Round(millimeters * dpi / MillimetersPerInch);
    }

    private static bool TryGetPaperSize(
        string paper,
        out string paperName,
        out double widthMillimeters,
        out double heightMillimeters)
    {
        switch (paper.ToLowerInvariant())
        {
            case "a4":
                paperName = "a4";
                widthMillimeters = 210.0;
                heightMillimeters = 297.0;
                return true;
            case "a3":
                paperName = "a3";
                widthMillimeters = 297.0;
                heightMillimeters = 420.0;
                return true;
            default:
                paperName = string.Empty;
                widthMillimeters = 0;
                heightMillimeters = 0;
                return false;
        }
    }
}
