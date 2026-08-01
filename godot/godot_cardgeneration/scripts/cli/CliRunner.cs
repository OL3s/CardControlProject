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
            "validate-cards" => _cardToolService.ValidateCards(),
            "validate-deck" => _cardToolService.ValidateDeck(options.DeckId),
            "render-card" => _cardToolService.RenderCard(options.CardId, options.OutputPath),
            "export-deck" => _cardToolService.ExportDeck(options.DeckId, options.OutputPath, options.Format, options.Layout, options.Columns, options.Spacing),
            "export-sheet" => _cardToolService.ExportSheet(options.DeckId, options.OutputPath, options.Paper, options.Dpi),
            "export-diy" => _cardToolService.ExportDiy(options.DeckId, options.OutputPath, options.Paper),
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
          validate-cards
          validate-deck --deck <deck_id>
          render-card --card <card_id> --output <path>
          export-deck --deck <deck_id> --format png --layout individual --output <path>
          export-sheet --deck <deck_id> --paper a4 --dpi 600 --output <path>
          export-diy --deck <deck_id> --paper a4 --output <path>
          export-showcase --deck <deck_id> --format png --output <path>

        Shortcut flags are also supported:
          --validate-cards
          --show-config
          --set-config
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

        Config:
          show-config prints the saved defaults.
          set-config stores supplied options as future defaults.
          Export commands use config defaults when flags are omitted.
        """;
    }
}
