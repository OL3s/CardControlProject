using System;
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
        try
        {
            var options = CliOptions.Parse(args);
            if (options.ShowHelp || string.IsNullOrWhiteSpace(options.Command))
            {
                GD.Print(GetHelpText());
                return 0;
            }

            if (options.Command != "set-config")
            {
                options.ApplyConfigDefaults(_cardToolService.LoadConfig());
            }

            var result = Execute(options);
            if (result.Success)
            {
                GD.Print(result.Message);
            }
            else
            {
                GD.PushError(result.Message);
            }

            return result.ExitCode;
        }
        catch (Exception exception)
        {
            GD.PushError(exception.Message);
            return 1;
        }
    }

    private ToolResult Execute(CliOptions options)
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
            "render-card" => _cardToolService.RenderCard(options.CardId, options.OutputPath),
            "export-deck" => _cardToolService.ExportDeck(options.DeckId, options.OutputPath, options.Format, options.Layout, options.Columns, options.Spacing),
            "export-sheet" => _cardToolService.ExportSheet(options.DeckId, options.OutputPath, options.Paper, options.Dpi, options.BackMirror, options.IncludeMeasurementGuide),
            "export-diy" => _cardToolService.ExportDiy(options.DeckId, options.OutputPath, options.Dpi, options.BackMirror, options.IncludeMeasurementGuide),
            "export-showcase" => _cardToolService.ExportShowcase(options.DeckId, options.OutputPath, options.Format),
            _ => ToolResult.Fail($"Unknown command '{options.Command}'. Use --help to list commands.")
        };
    }

    private static string GetHelpText()
    {
        return """
        Godot Card Generation CLI

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
          render-card --card <card_id> --output <path>
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
          --render-card <card_id>
          --export-deck <deck_id>
          --export-sheet <deck_id>
          --export-diy <deck_id>
          --export-showcase <deck_id>

        Deck PNG layouts:
          individual  One PNG per card in the output folder.
          grid        One PNG with all cards in a grid.
          strip       One long vertical PNG with all cards.

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

        Config:
          show-config prints the saved defaults.
          set-config stores supplied options as future defaults.
          reset-config restores factory settings defaults.
          reset-content deletes saved cards/decks and regenerates default cards/deck.
          Export commands use config defaults when flags are omitted.
        """;
    }
}
