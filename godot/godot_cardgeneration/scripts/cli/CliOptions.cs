using System;

namespace CardGeneration.Cli;

public sealed class CliOptions
{
    public string Command { get; private set; } = string.Empty;
    public string? CardId { get; private set; }
    public string? DeckId { get; private set; }
    public string OutputPath { get; private set; } = "res://output";
    public string Format { get; private set; } = "png";
    public string Paper { get; private set; } = "a4";
    public bool ShowHelp { get; private set; }

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
                    break;
                case "--deck":
                    options.DeckId = ReadValue(args, ref index, "--deck");
                    break;
                case "--output":
                    options.OutputPath = ReadValue(args, ref index, "--output");
                    break;
                case "--format":
                    options.Format = ReadValue(args, ref index, "--format");
                    break;
                case "--paper":
                    options.Paper = ReadValue(args, ref index, "--paper");
                    break;
                case "--list-cards":
                    options.Command = "list-cards";
                    break;
                case "--list-decks":
                    options.Command = "list-decks";
                    break;
                case "--validate-cards":
                    options.Command = "validate-cards";
                    break;
                case "--validate-deck":
                    options.Command = "validate-deck";
                    options.DeckId ??= TryReadOptionalValue(args, ref index);
                    break;
                case "--render-card":
                    options.Command = "render-card";
                    options.CardId ??= TryReadOptionalValue(args, ref index);
                    break;
                case "--export-deck":
                    options.Command = "export-deck";
                    options.DeckId ??= TryReadOptionalValue(args, ref index);
                    break;
                case "--export-sheet":
                    options.Command = "export-sheet";
                    options.DeckId ??= TryReadOptionalValue(args, ref index);
                    break;
                case "--export-diy":
                    options.Command = "export-diy";
                    options.DeckId ??= TryReadOptionalValue(args, ref index);
                    break;
                case "--export-showcase":
                    options.Command = "export-showcase";
                    options.DeckId ??= TryReadOptionalValue(args, ref index);
                    break;
                default:
                    if (!arg.StartsWith("--", StringComparison.Ordinal) && string.IsNullOrWhiteSpace(options.Command))
                    {
                        options.Command = arg;
                    }

                    break;
            }
        }

        return options;
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

    private static string? TryReadOptionalValue(string[] args, ref int index)
    {
        if (index + 1 >= args.Length || args[index + 1].StartsWith("--", StringComparison.Ordinal))
        {
            return null;
        }

        index++;
        return args[index];
    }
}
