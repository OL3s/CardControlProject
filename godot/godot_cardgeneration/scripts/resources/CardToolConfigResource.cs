using Godot;

namespace CardGeneration.Resources;

[GlobalClass]
public partial class CardToolConfigResource : Resource
{
    [Export] public string DefaultCardId { get; set; } = string.Empty;
    [Export] public string DefaultDeckId { get; set; } = "default_deck";
    [Export] public string DefaultOutputPath { get; set; } = "output";
    [Export] public string DefaultFormat { get; set; } = "png";
    [Export] public string DefaultPaper { get; set; } = "a4";
    [Export(PropertyHint.Enum, "150,300,600,1200")] public int DefaultDpi { get; set; } = 600;
    [Export(PropertyHint.Enum, "none,width,height,both")] public string DefaultBackMirror { get; set; } = "none";
    [Export(PropertyHint.Enum, "home,production")] public string DefaultPrintMode { get; set; } = "home";
    [Export(PropertyHint.Enum, "individual,grid,strip")] public string DefaultDeckLayout { get; set; } = "individual";
    [Export(PropertyHint.Range, "0,24,1")] public int DefaultGridColumns { get; set; }
    [Export(PropertyHint.Range, "0,256,1")] public int DefaultSpacing { get; set; } = 24;
    [Export(PropertyHint.Range, "90,110,0.1")] public double DefaultPrintCompensationPercent { get; set; } = 100.0;
}
