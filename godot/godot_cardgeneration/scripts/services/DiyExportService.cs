using CardGeneration.App;
using CardGeneration.Resources;
using System;
using System.IO;

namespace CardGeneration.Services;

public sealed class DiyExportService
{
    private readonly SheetExportService _sheetExportService;

    public DiyExportService()
        : this(new SheetExportService())
    {
    }

    public DiyExportService(SheetExportService sheetExportService)
    {
        _sheetExportService = sheetExportService;
    }

    public ToolResult ExportDiy(CardDeckResource deck, string outputPath, int dpi, string backMirror = "none", bool includeMeasurementGuide = false, Action<ExportProgress>? progress = null)
    {
        var outputDirectory = ProjectPaths.ToGlobalPath(outputPath);
        if (Path.GetExtension(outputDirectory).Equals(".png", StringComparison.OrdinalIgnoreCase))
        {
            return ToolResult.Fail("DIY export requires an output directory, not a .png file path.");
        }

        var a4Result = _sheetExportService.ExportSheet(deck, Path.Combine(outputDirectory, "a4"), "a4", dpi, backMirror, includeMeasurementGuide, progress);
        if (!a4Result.Success)
        {
            return a4Result;
        }

        var a3Result = _sheetExportService.ExportSheet(deck, Path.Combine(outputDirectory, "a3"), "a3", dpi, backMirror, includeMeasurementGuide, progress);
        if (!a3Result.Success)
        {
            return a3Result;
        }

        return ToolResult.Ok($"Exported DIY print sheets for deck '{deck.Id}' as A4 and A3 at {dpi} DPI to {outputDirectory}.");
    }
}
