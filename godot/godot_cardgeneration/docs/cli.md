[Back](../README.md)

# CLI

Conquora Card Generation skal kunne kjøres i Godot headless. CLI brukes for batchjobber, automatisert eksport og kontroll av kortdata uten å åpne GUI. Den innebygde default-preseten er **Elements: Conquora**. Målet for CLI-paritet er batch/data/export: liste/hente data, import, validering, config og eksport. Full interaktiv kort- og deckredigering kan være GUI-only i denne fasen.

## Grunnform

```sh
godot --headless --path godot/godot_cardgeneration -- --command <command> [options]
```

## Testpolicy

Ved utvikling og validering av endringer skal CLI-kommandoer som hovedregel bruke `--deck default_test`. Decken har ett terrengkort og ett monsterkort og er derfor den raske standarden for validering, rendering og eksport.

Bruk hele `default_deck` når testen må dekke alle kort, elementer, tiers eller innholdsvarianter, eller når en kritisk endring trenger full sluttvalidering. Dette gjelder særlig endringer i defaultinnhold, kortspesifikk layout og samlet produksjonseksport.

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
reset-config
reset-content
import-card
import-deck
delete-card
duplicate-card
delete-deck
duplicate-deck
validate-cards
validate-deck
export-deck
export-sheet
export-diy
export-print-test
export-showcase
```

Status:

* `list-cards`: implementert.
* `list-decks`: implementert.
* `show-config`: implementert.
* `set-config`: implementert.
* `reset-config`: implementert for å nullstille config/settings til fabrikkverdier.
* `reset-content`: implementert for å slette lagrede kort/decks og regenerere defaultkort og deckene `default_deck` og `default_test`.
* `import-card`: implementert for `.tres` kortresource.
* `import-deck`: implementert for `.tres` deckresource.
* `delete-card`: implementert for vanlige user card resources og genererte defaultkort; packaged `res://` defaults kan ikke slettes.
* `duplicate-card`: implementert for å kopiere et kort til en vanlig user card resource.
* `delete-deck`: implementert for vanlige user deck resources og genererte defaultdecks; packaged `res://` defaults kan ikke slettes.
* `duplicate-deck`: implementert for å kopiere en deck til en vanlig user deck resource.
* `validate-cards`: implementert.
* `validate-deck`: implementert for lagrede og innebygde deckresources.
* `export-deck`: implementert for PNG-layoutene `individual`, `grid` og `strip`.
* `export-sheet`: implementert for A4 og A3 PNG-printark med `3 mm` bleed, front/back, valgbar DPI, print compensation, valgfri bakside-speiling og valgfri 10 cm målelinje.
* `export-diy`: implementert som A4- og A3-printark i egne undermapper med samme printvalg.
* `export-print-test`: implementert som et tosidig 300 DPI-kalibreringsark med trim, bleed, mirror-guide og 10 cm-linje.
* `export-showcase`: implementert som et eldre grid-output-preset for CLI.

## Eksempler

Vise lagret config:

```sh
godot --headless --path godot/godot_cardgeneration -- --command show-config
```

Lagre defaults for repeterende eksport:

```sh
godot --headless --path godot/godot_cardgeneration -- --command set-config --deck sample_monster_deck --output output/sheets --paper a4 --dpi 600 --layout grid --columns 3 --spacing 24
```

Nullstille settings/config til fabrikkverdier:

```sh
godot --headless --path godot/godot_cardgeneration -- --command reset-config
```

Slette lagrede kort/decks og regenerere defaultkort/defaultdeck:

```sh
godot --headless --path godot/godot_cardgeneration -- --command reset-content
```

Importere et kortresource:

```sh
godot --headless --path godot/godot_cardgeneration -- --command import-card --input /path/to/card.tres
```

Importere et deckresource:

```sh
godot --headless --path godot/godot_cardgeneration -- --command import-deck --input /path/to/deck.tres
```

Duplisere et defaultkort før endring:

```sh
godot --headless --path godot/godot_cardgeneration -- --command duplicate-card --card default_monster_flame_1_a --new-id my_flame_monster
```

Slette en vanlig user card resource:

```sh
godot --headless --path godot/godot_cardgeneration -- --command delete-card --card my_flame_monster
```

Duplisere defaultdecken før endring:

```sh
godot --headless --path godot/godot_cardgeneration -- --command duplicate-deck --deck default_deck --new-id my_deck
```

Slette en vanlig user deck resource:

```sh
godot --headless --path godot/godot_cardgeneration -- --command delete-deck --deck my_deck
```

Etterpå kan mange kommandoer kjøres kortere. Denne bruker lagret deck, output, papir, DPI og bakside-speiling:

```sh
godot --headless --path godot/godot_cardgeneration -- --command export-sheet
```

Validere alle kort:

```sh
godot --headless --path godot/godot_cardgeneration -- --command validate-cards
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

Speile baksidearket venstre/høyre for tosidig print:

```sh
godot --headless --path godot/godot_cardgeneration -- --command export-sheet --deck monster_cards --paper a4 --dpi 600 --back-mirror width --output output/sheets
```

Legge til 10 cm målelinje for å sjekke printskalering:

```sh
godot --headless --path godot/godot_cardgeneration -- --command export-sheet --deck monster_cards --paper a4 --dpi 600 --measurement-guide --output output/sheets
```

Kompensere dersom printeren krymper utskriften:

```sh
godot --headless --path godot/godot_cardgeneration -- --command export-sheet --deck monster_cards --paper a4 --dpi 600 --measurement-guide --print-compensation 106 --output output/sheets
```

Lage det tosidige kalibreringsarket uten deck:

```sh
godot --headless --path godot/godot_cardgeneration -- --command export-print-test --paper a4 --print-compensation 106 --output output/print-test
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

Bakside-speiling for printark:

* `none`: samme slot-plassering som frontarket.
* `width`: speil hele baksidearket venstre/høyre.
* `height`: speil hele baksidearket opp/ned.
* `both`: speil begge veier.

Printark eksporteres som nummererte PNG-par:

```text
monster_cards_a4_600dpi_front_001.png
monster_cards_a4_600dpi_back_001.png
monster_cards_a4_600dpi_front_002.png
monster_cards_a4_600dpi_back_002.png
```

Hvert produksjonskort bruker et `69 x 94 mm` slot: `63 x 88 mm` ferdig trimområde med `3 mm` bleed på alle sider. `--print-compensation` godtar `90-110%` og skalerer trim, bleed, gridankere og 10 cm-linjen samlet; default er `100%`. En deck kan inneholde flere korttyper, så baksiden renderes per korttype for hvert kort. Front- og baksideark bruker de samme slot-rektanglene; speiling skjer på hele baksidearket etter at alle baksider er plassert. `--measurement-guide` reserverer en bunnstripe og tegner kontrollinjen med centimeter-ticks.

Kalibreringsarket utelater nederste-høyre kort på begge sider. Baksiden er alltid uspeilet. Etter fysisk duplex-print holdes arket mot lys: manglende bakside øverst venstre betyr `both`, øverst høyre `height`, nederst venstre `width`, og nederst høyre `none`. Juster kompensasjonen til solid trimomriss måler `63 x 88 mm` og kontrollinjen måler `10 cm`.

Store printark bruker automatisk memory-safe PNG-streaming i stedet for å allokere hele arket som én RGBA-buffer. Dette gjelder normalt A3 ved `600`/`1200 DPI` og A4 ved `1200 DPI`. Arkene skrives fortsatt sekvensielt og atomisk til de samme PNG-filnavnene; funksjonen endrer ikke layout, speiling eller målelinjen.

`--easy-backs` grupperer forsidene etter korttype og fyller alle slots på hvert tilhørende baksideark. Modusen bruker mer papir og blekk, men gjør slot-alignment og `--back-mirror` unødvendig; speiling ignoreres når easy backs er aktivert.

Eksportkommandoer viser fremdrift i interaktiv terminal. Fremdriften skrives til stderr, slik at stdout fortsatt kan brukes til maskinlesbare resultater. Bruk `--progress auto` (default) for automatisk terminaldeteksjon, `--progress always` for å tvinge fremdrift eller `--progress never` for stille kjøring i CI/script.

Deck image-export støtter `--backs none`, `--backs used` og `--backs all`. Valgte baksider legges foran kortforsidene i rekkefølgen Monster og Terrain.

Når et ark er fullt, lager eksporten automatisk neste nummererte front/back-par. Arkdelingen bruker antall kort som får plass på valgt papir og `ceil(cardCount / cardsPerSheet)`.

Eksportere DIY-pakke som A4- og A3-printark:

```sh
godot --headless --path godot/godot_cardgeneration -- --command export-diy --deck monster_cards --dpi 600 --output output/diy
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
godot --headless --path godot/godot_cardgeneration -- --reset-config
```

```sh
godot --headless --path godot/godot_cardgeneration -- --reset-content
```

```sh
godot --headless --path godot/godot_cardgeneration -- --import-card /path/to/card.tres
```

```sh
godot --headless --path godot/godot_cardgeneration -- --import-deck /path/to/deck.tres
```

```sh
godot --headless --path godot/godot_cardgeneration -- --duplicate-card default_monster_flame_1_a --new-id my_flame_monster
```

```sh
godot --headless --path godot/godot_cardgeneration -- --delete-card my_flame_monster
```

```sh
godot --headless --path godot/godot_cardgeneration -- --duplicate-deck default_deck --new-id my_deck
```

```sh
godot --headless --path godot/godot_cardgeneration -- --delete-deck my_deck
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
* `DefaultBackMirror`
* `DefaultDeckLayout`
* `DefaultGridColumns`
* `DefaultSpacing`

Default card er tom etter fabrikkreset; default deck er `default_deck`. CLI bruker configverdier når tilsvarende flagg ikke er oppgitt. Direkte CLI-flagg overstyrer config for den ene kjøringen uten å lagre endringen. `set-config` lagrer bare feltene som oppgis.

GUI Settings-panelet bruker samme config-resource. Export-skjermen bruker disse verdiene som startverdier, men endringer i Export gjelder bare den ene eksporten og lagres ikke som nye defaults. Endringer i GUI Settings og CLI `set-config` skal derfor være synlige for hverandre.

Cards/Decks-listene viser både default resources under `res://resources/...` og brukerbiblioteket under `user://resources/...`. User resources overstyrer default resources med samme ID. Ved oppstart genereres manglende defaultkort til `user://resources/cards/default/...` med `default_`-prefix, og deckene `default_deck` og `default_test` genereres under `user://resources/decks/default/` hvis de mangler fra både default- og brukerressurser.

Packaged `res://` resources er read-only også i CLI. Genererte defaultkort/decks under `user://resources/.../default` kan slettes og lages på nytt ved oppstart hvis de mangler. Endringer i defaults skal starte med `duplicate-card`/`duplicate-deck` eller GUI `Save as new`.

Default deck er `default_deck`. Den er en innebygd 52-korts preset fra `DefaultDeckFactory`, og de 52 kortene fra decken blir også tilgjengelige som card resources med `default_`-prefix ved oppstart. `default_test` inneholder ett terrengkort og ett monsterkort for raske CLI- og eksporttester.
