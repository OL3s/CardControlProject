using System;
using System.Diagnostics;
using CardGeneration.App;
using CardGeneration.Services;
using Godot;

namespace CardGeneration.Cli;

public sealed class CliRunner
{
    private readonly CardToolService _cardToolService;

    public CliRunner(CardToolService cardToolService)
    {
        _cardToolService = cardToolService;
    }

    public int Run(string[] args)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            var options = CliOptions.Parse(args);
            if (options.ShowHelp || string.IsNullOrWhiteSpace(options.Command))
            {
                AppLogger.CliInfo("Showing CLI help.");
                GD.Print(GetHelpText());
                return 0;
            }

            AppLogger.CliInfo($"START command={options.Command}; arguments={args.Length}");
            if (options.Command != "set-config")
            {
                options.ApplyConfigDefaults(_cardToolService.LoadConfig());
            }

            using var progress = CliProgressReporter.Create(options.ProgressMode);
            var result = Execute(options, progress.Report);
            if (result.Success)
            {
                AppLogger.CliInfo($"DONE command={options.Command}; exit_code={result.ExitCode}; elapsed_ms={stopwatch.ElapsedMilliseconds}; result={result.Message}");
                GD.Print(result.Message);
            }
            else
            {
                AppLogger.CliError($"FAILED command={options.Command}; exit_code={result.ExitCode}; elapsed_ms={stopwatch.ElapsedMilliseconds}; result={result.Message}");
                GD.PushError(result.Message);
            }

            return result.ExitCode;
        }
        catch (Exception exception)
        {
            AppLogger.CliError($"FAILED command parsing or execution; exit_code=1; elapsed_ms={stopwatch.ElapsedMilliseconds}", exception);
            GD.PushError(exception.Message);
            return 1;
        }
    }

    private ToolResult Execute(CliOptions options, Action<ExportProgress> progress)
    {
        return options.Command switch
        {
            "list-cards" => _cardToolService.ListCards(),
            "list-decks" => _cardToolService.ListDecks(),
            "show-config" => _cardToolService.ShowConfig(),
            "set-config" => _cardToolService.SetConfig(options.ToConfigUpdate()),
            "reset-config" => _cardToolService.ResetConfig(),
            "reset-content" => _cardToolService.ResetSavedContent(),
            "import-card" => _cardToolService.ImportCardResource(options.InputPath),
            "import-deck" => _cardToolService.ImportDeckResource(options.InputPath),
            "delete-card" => _cardToolService.DeleteCard(options.CardId),
            "duplicate-card" => _cardToolService.DuplicateCard(options.CardId, options.NewId),
            "delete-deck" => _cardToolService.DeleteDeck(options.DeckId),
            "duplicate-deck" => _cardToolService.DuplicateDeck(options.DeckId, options.NewId),
            "validate-cards" => _cardToolService.ValidateCards(),
            "validate-deck" => _cardToolService.ValidateDeck(options.DeckId),
            "export-deck" => _cardToolService.ExportDeck(options.DeckId, options.OutputPath, options.Format, options.Layout, options.Columns, options.Spacing, progress, ParseImageBackMode(options.BackImages)),
            "export-sheet" => _cardToolService.ExportSheet(options.DeckId, options.OutputPath, options.Paper, options.Dpi, options.BackMirror, options.IncludeMeasurementGuide, progress, options.EasyPrintBacks),
            "export-diy" => _cardToolService.ExportDiy(options.DeckId, options.OutputPath, options.Dpi, options.BackMirror, options.IncludeMeasurementGuide, progress),
            "export-showcase" => _cardToolService.ExportShowcase(options.DeckId, options.OutputPath, options.Format, progress),
            _ => ToolResult.Fail($"Unknown command '{options.Command}'. Use --help to list commands.")
        };
    }

    private static string GetHelpText()
    {
        return """
        Conquora Card Generation CLI

        Usage:
          godot --headless --path godot/godot_cardgeneration -- --command <command> [options]

        Commands:
          list-cards
          list-decks
          show-config
          set-config [options]
          reset-config
          reset-content
          import-card --input <path.tres>
          import-deck --input <path.tres>
          delete-card --card <card_id>
          duplicate-card --card <card_id> [--new-id <card_id>]
          delete-deck --deck <deck_id>
          duplicate-deck --deck <deck_id> [--new-id <deck_id>]
          validate-cards
          validate-deck --deck <deck_id>
          export-deck --deck <deck_id> --format png --layout individual --output <path>
          export-sheet --deck <deck_id> --paper a4 --dpi 600 --back-mirror none --output <path>
          export-diy --deck <deck_id> --dpi 600 --output <path>
          export-showcase --deck <deck_id> --format png --output <path>

        Shortcut flags are also supported:
          --validate-cards
          --show-config
          --set-config
          --reset-config
          --reset-content
          --import-card <path.tres>
          --import-deck <path.tres>
          --delete-card <card_id>
          --duplicate-card <card_id>
          --delete-deck <deck_id>
          --duplicate-deck <deck_id>
          --export-deck <deck_id>
          --export-sheet <deck_id>
          --export-diy <deck_id>
          --export-showcase <deck_id>

        Deck PNG layouts:
          individual  One PNG per card in the output folder.
          grid        One PNG with all cards in a grid.
          strip       One long vertical PNG with all cards.

        Deck image backs:
          --backs none  Export fronts only.
          --backs used  Prepend backs for card types present in the deck.
          --backs all   Prepend Monster and Terrain backs.

        Print DPI choices:
          150  Draft preview quality.
          300  Standard print quality.
          600  Print-master quality and default.
          1200 High-detail archival quality.

        Back mirror choices for print sheets:
          none   Back sheet uses the same slot positions as the front sheet.
          width  Mirror the whole back sheet left/right.
          height Mirror the whole back sheet top/bottom.
          both   Mirror the whole back sheet both ways.

        Print sheet options:
          --measurement-guide  Add a 10 cm guide line with 1 cm ticks for print scaling checks.
          --easy-backs         Group fronts by card type and fill every paired back sheet.
          --progress <mode>    Progress output: auto, always, or never (default: auto).

        Config:
          show-config prints the saved defaults.
          set-config stores supplied options as future defaults.
          reset-config restores factory settings defaults.
          reset-content deletes saved cards/decks and regenerates default cards/deck.
          Export commands use config defaults when flags are omitted.
        """;
    }

    private static ImageBackMode ParseImageBackMode(string value)
    {
        return value.ToLowerInvariant() switch
        {
            "none" => ImageBackMode.None,
            "used" => ImageBackMode.Used,
            "all" => ImageBackMode.All,
            _ => throw new ArgumentException($"Back image mode '{value}' is not supported. Use none, used, or all.")
        };
    }
}
