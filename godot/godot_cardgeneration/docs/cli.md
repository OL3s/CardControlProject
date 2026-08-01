[Back](../README.md)

# CLI

Kortverktøyet skal kunne kjøres i Godot headless. CLI brukes for batchjobber, automatisert eksport og kontroll av kortdata uten å åpne GUI.

## Grunnform

```sh
godot --headless --path godot/godot_cardgeneration -- --command <command> [options]
```

Eksempel:

```sh
godot --headless --path godot/godot_cardgeneration -- --command validate-cards
```

## Kommandoer

Kommandoer:

```text
list-cards
list-decks
show-config
set-config
validate-cards
validate-deck
render-card
export-deck
export-sheet
export-diy
export-showcase
```

Status:

* `list-cards`: implementert.
* `list-decks`: implementert.
* `show-config`: implementert.
* `set-config`: implementert.
* `validate-cards`: implementert.
* `validate-deck`: implementert for lagrede deckresources.
* `render-card`: implementert for PNG.
* `export-deck`: implementert for PNG-layoutene `individual`, `grid` og `strip`.
* `export-sheet`: implementert for A4 og A3 PNG-printark med front/back og valgbar DPI.
* `export-diy`: stub.
* `export-showcase`: implementert som grid-output.

## Eksempler

Vise lagret config:

```sh
godot --headless --path godot/godot_cardgeneration -- --command show-config
```

Lagre defaults for repeterende eksport:

```sh
godot --headless --path godot/godot_cardgeneration -- --command set-config --deck sample_monster_deck --output output/sheets --paper a4 --dpi 600 --layout grid --columns 3 --spacing 24
```

Etterpå kan mange kommandoer kjøres kortere. Denne bruker lagret deck, output, papir og DPI:

```sh
godot --headless --path godot/godot_cardgeneration -- --command export-sheet
```

Validere alle kort:

```sh
godot --headless --path godot/godot_cardgeneration -- --command validate-cards
```

Rendere ett kort:

```sh
godot --headless --path godot/godot_cardgeneration -- --command render-card --card monster_flame_1_a --output output/cards/preview
```

Eksportere en kortstokk som bilder:

```sh
godot --headless --path godot/godot_cardgeneration -- --command export-deck --deck monster_cards --format png --output output/decks
```

Eksportere en kortstokk som ett gridbilde:

```sh
godot --headless --path godot/godot_cardgeneration -- --command export-deck --deck monster_cards --format png --layout grid --columns 4 --output output/decks/monster_cards_grid.png
```

Eksportere en kortstokk som ett langt vertikalt bilde:

```sh
godot --headless --path godot/godot_cardgeneration -- --command export-deck --deck monster_cards --format png --layout strip --output output/decks/monster_cards_strip.png
```

Eksportere en kortstokk som ett bilde per kort i en mappe:

```sh
godot --headless --path godot/godot_cardgeneration -- --command export-deck --deck monster_cards --format png --layout individual --output output/decks/monster_cards
```

Sample-deck som finnes i prosjektet nå:

```sh
godot --headless --path godot/godot_cardgeneration -- --command export-deck --deck sample_monster_deck --format png --output output/decks/sample
```

Eksportere printark:

```sh
godot --headless --path godot/godot_cardgeneration -- --command export-sheet --deck monster_cards --paper a4 --dpi 600 --output output/sheets
```

A3 bruker samme kommando med `--paper a3`:

```sh
godot --headless --path godot/godot_cardgeneration -- --command export-sheet --deck monster_cards --paper a3 --dpi 600 --output output/sheets
```

DPI må velges fra faste normalverdier:

* `150`: rask draft/preview.
* `300`: standard print.
* `600`: print-master og default.
* `1200`: ekstra høy detaljgrad.

Printark eksporteres som nummererte PNG-par:

```text
monster_cards_a4_600dpi_front_001.png
monster_cards_a4_600dpi_back_001.png
monster_cards_a4_600dpi_front_002.png
monster_cards_a4_600dpi_back_002.png
```

Kortene plasseres i pokerkortstørrelse, `63 x 88 mm`, ved valgt DPI. Baksiden bestemmes av `DeckCardType` på kortstokken, ikke av hvert enkelt kort.

Når et ark er fullt, lager eksporten automatisk neste nummererte front/back-par. Arkdelingen bruker antall kort som får plass på valgt papir og `ceil(cardCount / cardsPerSheet)`.

Eksportere DIY-pakke:

```sh
godot --headless --path godot/godot_cardgeneration -- --command export-diy --deck monster_cards --paper a4 --output output/diy
```

Eksportere showcase:

```sh
godot --headless --path godot/godot_cardgeneration -- --command export-showcase --deck monster_cards --format png --output output/showcase
```

## Shortcut-Flagg

CLI støtter også kortere flagg som setter kommando direkte:

```sh
godot --headless --path godot/godot_cardgeneration -- --validate-cards
```

```sh
godot --headless --path godot/godot_cardgeneration -- --show-config
```

```sh
godot --headless --path godot/godot_cardgeneration -- --set-config --deck sample_monster_deck --dpi 300
```

```sh
godot --headless --path godot/godot_cardgeneration -- --render-card monster_flame_1_a --output output/cards/preview
```

## Designregel

CLI skal bare være et tynt lag over appens service-lag. Hvis en funksjon finnes både i GUI og CLI, skal begge kalle samme C#-funksjon.

## Lagret Config

Langvarige CLI- og eksportinnstillinger ligger i `resources/config/card_tool_config.tres`.

Felt som brukes som defaults:

* `DefaultCardId`
* `DefaultDeckId`
* `DefaultOutputPath`
* `DefaultFormat`
* `DefaultPaper`
* `DefaultDpi`
* `DefaultDeckLayout`
* `DefaultGridColumns`
* `DefaultSpacing`

CLI bruker configverdier når tilsvarende flagg ikke er oppgitt. Direkte CLI-flagg overstyrer config for den ene kjøringen uten å lagre endringen. `set-config` lagrer bare feltene som oppgis.

GUI Settings-panelet bruker samme config-resource. Endringer i GUI og CLI skal derfor være synlige for hverandre.
