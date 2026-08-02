using System;
using CardGeneration.App;
using CardGeneration.Resources;

namespace CardGeneration.Cli;

public sealed class CliOptions
{
    public string Command { get; private set; } = string.Empty;
    public string? CardId { get; private set; }
    public string? DeckId { get; private set; }
    public string? InputPath { get; private set; }
    public string OutputPath { get; private set; } = "res://output";
    public string Format { get; private set; } = "png";
    public string Paper { get; private set; } = "a4";
    public int Dpi { get; private set; } = 600;
    public string Layout { get; private set; } = "individual";
    public int Columns { get; private set; }
    public int Spacing { get; private set; } = 24;
    public bool ShowHelp { get; private set; }

    public bool HasCardId { get; private set; }
    public bool HasDeckId { get; private set; }
    public bool HasInputPath { get; private set; }
    public bool HasOutputPath { get; private set; }
    public bool HasFormat { get; private set; }
    public bool HasPaper { get; private set; }
    public bool HasDpi { get; private set; }
    public bool HasLayout { get; private set; }
    public bool HasColumns { get; private set; }
    public bool HasSpacing { get; private set; }

    public static CliOptions Parse(string[] args)
    {
        var options = new CliOptions();

        for (var index = 0; index < args.Length; index++)
        {
            var arg = args[index];
            switch (arg)
            {
                case "--help":
                case "-h":
                    options.ShowHelp = true;
                    break;
                case "--command":
                    options.Command = ReadValue(args, ref index, "--command");
                    break;
                case "--card":
                    options.CardId = ReadValue(args, ref index, "--card");
                    options.HasCardId = true;
                    break;
                case "--deck":
                    options.DeckId = ReadValue(args, ref index, "--deck");
                    options.HasDeckId = true;
                    break;
                case "--input":
                    options.InputPath = ReadValue(args, ref index, "--input");
                    options.HasInputPath = true;
                    break;
                case "--output":
                    options.OutputPath = ReadValue(args, ref index, "--output");
                    options.HasOutputPath = true;
                    break;
                case "--format":
                    options.Format = ReadValue(args, ref index, "--format");
                    options.HasFormat = true;
                    break;
                case "--paper":
                    options.Paper = ReadValue(args, ref index, "--paper");
                    options.HasPaper = true;
                    break;
                case "--dpi":
                    options.Dpi = int.Parse(ReadValue(args, ref index, "--dpi"));
                    options.HasDpi = true;
                    break;
                case "--layout":
                    options.Layout = ReadValue(args, ref index, "--layout");
                    options.HasLayout = true;
                    break;
                case "--columns":
                    options.Columns = int.Parse(ReadValue(args, ref index, "--columns"));
                    options.HasColumns = true;
                    break;
                case "--spacing":
                    options.Spacing = int.Parse(ReadValue(args, ref index, "--spacing"));
                    options.HasSpacing = true;
                    break;
                case "--show-config":
                    options.Command = "show-config";
                    break;
                case "--set-config":
                    options.Command = "set-config";
                    break;
                case "--list-cards":
                    options.Command = "list-cards";
                    break;
                case "--list-decks":
                    options.Command = "list-decks";
                    break;
                case "--import-card":
                    options.Command = "import-card";
                    if (TryReadOptionalValue(args, ref index, out var importCardPath))
                    {
                        options.InputPath ??= importCardPath;
                        options.HasInputPath = true;
                    }

                    break;
                case "--import-deck":
                    options.Command = "import-deck";
                    if (TryReadOptionalValue(args, ref index, out var importDeckPath))
                    {
                        options.InputPath ??= importDeckPath;
                        options.HasInputPath = true;
                    }

                    break;
                case "--validate-cards":
                    options.Command = "validate-cards";
                    break;
                case "--validate-deck":
                    options.Command = "validate-deck";
                    if (TryReadOptionalValue(args, ref index, out var validateDeckId))
                    {
                        options.DeckId ??= validateDeckId;
                        options.HasDeckId = true;
                    }

                    break;
                case "--render-card":
                    options.Command = "render-card";
                    if (TryReadOptionalValue(args, ref index, out var renderCardId))
                    {
                        options.CardId ??= renderCardId;
                        options.HasCardId = true;
                    }

                    break;
                case "--export-deck":
                    options.Command = "export-deck";
                    if (TryReadOptionalValue(args, ref index, out var exportDeckId))
                    {
                        options.DeckId ??= exportDeckId;
                        options.HasDeckId = true;
                    }

                    break;
                case "--export-sheet":
                    options.Command = "export-sheet";
                    if (TryReadOptionalValue(args, ref index, out var exportSheetDeckId))
                    {
                        options.DeckId ??= exportSheetDeckId;
                        options.HasDeckId = true;
                    }

                    break;
                case "--export-diy":
                    options.Command = "export-diy";
                    if (TryReadOptionalValue(args, ref index, out var exportDiyDeckId))
                    {
                        options.DeckId ??= exportDiyDeckId;
                        options.HasDeckId = true;
                    }

                    break;
                case "--export-showcase":
                    options.Command = "export-showcase";
                    if (TryReadOptionalValue(args, ref index, out var exportShowcaseDeckId))
                    {
                        options.DeckId ??= exportShowcaseDeckId;
                        options.HasDeckId = true;
                    }

                    break;
                default:
                    if (!arg.StartsWith("--", StringComparison.Ordinal) && string.IsNullOrWhiteSpace(options.Command))
                    {
                        options.Command = arg;
                    }
                    else if (!arg.StartsWith("--", StringComparison.Ordinal)
                             && options.Command is "import-card" or "import-deck"
                             && string.IsNullOrWhiteSpace(options.InputPath))
                    {
                        options.InputPath = arg;
                        options.HasInputPath = true;
                    }

                    break;
            }
        }

        return options;
    }

    public void ApplyConfigDefaults(CardToolConfigResource config)
    {
        if (!HasCardId)
        {
            CardId = config.DefaultCardId;
        }

        if (!HasDeckId)
        {
            DeckId = config.DefaultDeckId;
        }

        if (!HasOutputPath)
        {
            OutputPath = config.DefaultOutputPath;
        }

        if (!HasFormat)
        {
            Format = config.DefaultFormat;
        }

        if (!HasPaper)
        {
            Paper = config.DefaultPaper;
        }

        if (!HasDpi)
        {
            Dpi = config.DefaultDpi;
        }

        if (!HasLayout)
        {
            Layout = config.DefaultDeckLayout;
        }

        if (!HasColumns)
        {
            Columns = config.DefaultGridColumns;
        }

        if (!HasSpacing)
        {
            Spacing = config.DefaultSpacing;
        }
    }

    public CardToolConfigUpdate ToConfigUpdate()
    {
        return new CardToolConfigUpdate
        {
            DefaultCardId = HasCardId ? CardId : null,
            DefaultDeckId = HasDeckId ? DeckId : null,
            DefaultOutputPath = HasOutputPath ? OutputPath : null,
            DefaultFormat = HasFormat ? Format : null,
            DefaultPaper = HasPaper ? Paper : null,
            DefaultDpi = HasDpi ? Dpi : null,
            DefaultDeckLayout = HasLayout ? Layout : null,
            DefaultGridColumns = HasColumns ? Columns : null,
            DefaultSpacing = HasSpacing ? Spacing : null
        };
    }

    private static string ReadValue(string[] args, ref int index, string flag)
    {
        if (index + 1 >= args.Length)
        {
            throw new ArgumentException($"Missing value after {flag}.");
        }

        index++;
        return args[index];
    }

    private static bool TryReadOptionalValue(string[] args, ref int index, out string? value)
    {
        if (index + 1 >= args.Length || args[index + 1].StartsWith("--", StringComparison.Ordinal))
        {
            value = null;
            return false;
        }

        index++;
        value = args[index];
        return true;
    }
}
