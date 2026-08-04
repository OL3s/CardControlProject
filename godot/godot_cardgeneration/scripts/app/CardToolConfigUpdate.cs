namespace CardGeneration.App;

public sealed class CardToolConfigUpdate
{
    public string? DefaultCardId { get; init; }
    public string? DefaultDeckId { get; init; }
    public string? DefaultOutputPath { get; init; }
    public string? DefaultFormat { get; init; }
    public string? DefaultPaper { get; init; }
    public int? DefaultDpi { get; init; }
    public string? DefaultBackMirror { get; init; }
    public string? DefaultPrintMode { get; init; }
    public string? DefaultDeckLayout { get; init; }
    public int? DefaultGridColumns { get; init; }
    public int? DefaultSpacing { get; init; }
    public double? DefaultPrintCompensationPercent { get; init; }
}
