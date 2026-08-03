[Back](../../README.md)

# Conquora Card Generation

Denne mappen inneholder Godot-prosjektet for Conquora Card Generation. Verktøyet skal brukes til å lage, lagre, vise og eksportere kort og kortstokker for fysisk prototype, Tabletop Simulator og senere digital bruk. Den innebygde default-preseten representerer førsteutgaven **Elements: Conquora**.

Applikasjonsversjonen ligger i `project.godot` som `application/config/version` og vises nederst til høyre på hovedmenyen.

## Navigasjon

* [Prosjektoversikt](../../README.md)
* [Fysisk versjon](../../physical/README.md)
* [Godot gameplay](../godot_gameplay/README.md)
* [Aktivt regelutkast](../../shared/docs/gameidea-working.md)
* [Arkitektur](docs/architecture.md)
* [CLI](docs/cli.md)
* [Framdriftsplan](docs/progress-plan.md)
* [Conquora-logokonsept](../../shared/docs/logo-concept.md)

## Formål

Kortgenereringen skal gjøre det mulig å lage kort én gang fra Godot `Resource`-data og grafiske lag, og deretter bruke samme data til preview, bilder, kortstokker, printark og DIY-pakker.

Verktøyet skal ha både GUI og CLI/headless-modus. GUI og CLI skal bruke samme service-lag for lasting, import, validering, rendering, eksport og config-defaults. Kort- og deckredigering kan være GUI-only i denne fasen; batch/data/export-funksjoner skal finnes i begge innganger.

All kode, filnavn, mappenavn og tekniske navn holdes på engelsk. Dokumentasjonen holdes på norsk.

## Hovedflyt I Appen

Første GUI-retning bruker en smal hovedmeny:

* `Cards`
* `Decks`
* `Export`
* `Settings`

`Cards` viser kort fra default resources og brukerbiblioteket, preview, edit, duplicate og delete. Nye kort opprettes med `+` inne på denne skjermen. Før editoren åpnes velges korttype, fordi monster- og terrengkort har ulik data og oppsett.

`Decks` viser kortstokker fra default resources og brukerbiblioteket, korttelling, preview, edit, duplicate og delete. Nye kortstokker opprettes med `+` inne på denne skjermen. `+` kan starte en tom kortstokk eller en default 52-korts preset fra `shared/docs`. Ved oppstart sjekker appen om decken `default_deck` og alle kortene fra den finnes i default- eller brukerressurser, og genererer manglende resources til `user://`.

Default deck er `default_deck` og representerer **Elements: Conquora**. Korttabellene i `shared/docs` er source of truth, og `DefaultDeckFactory` speiler dem som 20 terreng og 32 monstre. Hvert monsterelement har fordelingen 4 Tier 1, 3 Tier 2 og 1 Tier 3. Ved oppstart lagres manglende defaultkort med `default_`-prefix til `user://resources/cards/default/...` og manglende defaultdeck til `user://resources/decks/default/default_deck.tres`, slik at de kommer tilbake etter sletting og ny appstart. En intern innholdsversjon erstatter automatisk eldre genererte defaults når presetformatet endres, uten å slette vanlige brukerkort eller bruker-decks. `sample_monster_deck` er bare en liten smoke-test resource.

Packaged default resources under `res://` er read-only i appen. Genererte defaultkort/decks under `user://resources/.../default` kan slettes, og manglende defaultkort/decks lages på nytt ved neste oppstart. Defaults kan åpnes i editoren for inspeksjon og som utgangspunkt, men `Save` nekter å overskrive dem. Bruk `Save as new` eller duplicate for å lage en vanlig user resource før endringer lagres.

Hovedmenyen skal ikke vise et tilfeldig kortpreview og skal ikke ha egne `New Card`/`New Deck` valg. Kortpreview hører hjemme i `Cards`, `Decks`, editor- og eksportskjermene. Preview viser både front og bakside der det er relevant, og dobbelklikk på preview åpner større visning.

Korteditoren støtter felles kortfelt, image source path, preview, lagring og PNG-eksport. Korttypen er valgt før editoren åpnes. Den avtalte kortmodellen har eksplisitt element for både monster og terreng. Elementet er uavhengig av monsterets ressurskrav og terrengets produserte ressurser. Monster har i tillegg eksplisitt Tier 1-3, vist som kobberdiamanter ved elementmedaljongen. [Felles spesifikasjon for kortutseende](../../shared/docs/card-appearance.md) er visuell source of truth.

Deckeditoren støtter deck-ID, tilgjengelige kort på tvers av korttyper, deck entries med antall, `Save` og `Save New`. Monster- og terrengbakside velges som egne deck-eide artwork source paths med `Fit`, `Stretch` eller `Cover`; tomt path bruker default-artworket. Venstre side viser lagrede kort som preview-fliser i en horisontal scrollrad med ikonknapper for å legge til én kopi eller velge flere kort. Høyre side viser deckinnhold som preview-fliser med ikonknapper for slett, dupliser og multiselect. Eksport gjøres bare fra `Export`-skjermen. En deck er et ferdig produkt med både monster- og terrengkort.

`Export` er eneste eksportflate og eksporterer alltid en deck, med to eksporttyper: `Images` og `Print`. Images eksporterer en deck som individuelle bilder, grid eller strip, og kan forhåndsvises uten å skrive filer. Kort alene eksporteres ikke, siden ikoner, power-glyph og baksider er deck-eid. `Back Images` kan utelate baksider, legge til baksiden for hver korttype som faktisk brukes, eller legge til begge baksidetypene. Baksidene kommer først i rekkefølgen Monster og Terrain. Print lager nummererte A4- eller A3-ark med for- og baksider for fysisk utskrift og kutting. Normal print beholder samme kortplassering foran og bak. `Easy backs` grupperer forsidene etter korttype og fyller alle plassene på det tilhørende baksidearket; dette bruker mer papir og blekk, men krever ikke speiling eller nøyaktig slot-alignment. Preview genereres asynkront med fremdriftslinje og viser resultatet i en scrollbar visning. Verdiene starter fra lagrede defaults, men kan endres direkte i Export for den ene eksporten uten å lagres som nye defaults.

Export-knappen åpner Godots file dialog i valgt/default exportmappe. Cancel avbryter eksporten, og Save eller mappevalg starter eksporten.

`Settings` redigerer den samme config-resourcen som CLI bruker. Dette er oppstarts- og CLI-defaults, ikke valg som normalt må endres før hver eksport. Endringer gjort i GUI skal derfor påvirke senere CLI-kjøringer uten tilsvarende `--`-flagg, og `set-config` i CLI skal påvirke GUI-innstillingene. Settings har også handlinger for å nullstille config-defaults og for å slette lagrede kort/decks før defaultkort og decken `default_deck` regenereres.

## Nåværende Flyt

1. Les kortdata fra Godot resources.
2. Bygg kortet i lag med den felles `Image`-baserte rendereren.
3. Bruk samme renderer i GUI-preview og CLI/headless eksport.
4. Eksporter individuelle kortbilder, grid, strip eller printark som PNG.
5. Plasser kortene på A4/A3-ark i pokerkortstørrelse basert på valgt DPI, med valgfri speiling av hele baksidearket for tosidig print.

## Lagmodell

Kort bygges i lag slik at korttype, bilde og spillinformasjon kan styres separat av kortgeneratoren.

Forsiden bygges nedenfra og opp:

```text
card
  base_background
  card_image
  panels
  icons_and_text
  print_guides
```

`base_background` er et heldekkende fargelag nederst. Rammen har separate, dempede nøytraltoner for monster og terreng, men skal ikke konkurrere med elementfargene i kortbildet og ressursikonene. Målet er at spilleren skal kunne skille korttypene uten at for eksempel et gressmonster får en kraftig rød ramme.

`card_image` ligger oppå basebakgrunnen. For monsterkort er dette monsterbildet, og for terrengkort er det terrengbildet. De endelige bildene finnes ikke ennå. Standardkortene peker til kanoniske PNG-plasser under `assets/artwork/monsters/` og `assets/artwork/terrain/`, slik at manglende masters er synlige og et korrekt navngitt bilde tas i bruk uten kodeendringer.

Et tomt image source path viser den vanlige, ensfargede placeholderen og betyr at kortet ikke har fått bilde ennå. Et path som er satt, men ikke kan finnes eller lastes, viser i stedet en egen crossed-image-placeholder. Dermed er det synlig i både editor og eksport om bildet mangler med vilje eller pathen er ugyldig.

`panels` ligger oppå kortbildet og samler spillinformasjon. Panelene skal gjøre ikoner og eventuell tekst lesbare uavhengig av hvor detaljert kortbildet er.

`icons_and_text` inneholder ressursikoner, styrkeikoner, piler, korttekst og annen spillinformasjon. Element- og power-SVG-er er glyph-only under `assets/icons/`; renderer tegner medallionfelt og outline rundt glyphen. Ikoner er deck-scoped: standalone-kort viser solide farge-placeholders, mens deck previews og exports bruker deckens valgte element- og power-glyphs.

Monsterets eksplisitte element vises øverst til høyre. Terrengets eksplisitte kjerneelement vises stort i sentrum, separat fra ressursmedaljongene i de faste hjørnene. Detaljert plassering og visuell stil følger [felles spesifikasjon for kortutseende](../../shared/docs/card-appearance.md).

`print_guides` er kun for fysisk produksjon, for eksempel kuttemerker, bleed eller hjelpelinjer. Slike lag skal ikke være del av vanlig preview med mindre brukeren eksplisitt velger det.

Baksiden bruker samme renderer-pipeline, korttypebaserte basefarge, ramme, indre `598x898`-bildeområde og avrundingsverdier som forsiden. Baksidebildet er et kantløst artwork-lag som skaleres med `Fit`, `Stretch` eller `Cover` innenfor den felles rammen. Baksiden skal ikke avsløre kortspesifikke data som element, krav, styrke eller effekt. Printark bruker korttypen til hvert enkelt kort, siden én ferdig deck kan inneholde både terreng og monstre.

Default-artwork for kortbaksidene ligger som kantløse SVG-er under `assets/card_backs/`, med dokumentasjonskilder under `shared/docs/images/svg/`. Monsterbaksiden er tonet litt ned i rødfargen for å fungere bedre med gress- og vannmonstre.

## Printkrav

Første printstandard ved print-master-kvalitet:

```text
Ferdig kortstørrelse: 63 x 88 mm
Bleed: 3 mm på alle sider
Eksportert kortstørrelse: 69 x 94 mm
DPI: 600
Eksportert pikselstørrelse: 1630 x 2220 px
```

Begreper:

* Ferdig kortstørrelse er størrelsen etter kutting.
* Bleed er ekstra bildeområde utenfor kuttekanten, slik at små kutteavvik ikke gir hvite kanter.
* Eksportert kortstørrelse er ferdig kortstørrelse pluss bleed på alle sider.
* Godot jobber i piksler, ikke ekte DPI. `600 DPI` betyr her at `1630 x 2220 px` printes som `69 x 94 mm`.

DPI skal velges fra faste normalverdier i eksportmenyen:

* `150 DPI`: rask draft/preview.
* `300 DPI`: standard print.
* `600 DPI`: print-master og default.
* `1200 DPI`: ekstra høy detaljgrad.

Baksidearket kan eksporteres med speiling hvis skriveroppsettet trenger det for tosidig print:

* `none`: samme plassering som frontarket.
* `width`: speil hele baksidearket venstre/høyre.
* `height`: speil hele baksidearket opp/ned.
* `both`: speil begge veier.

Front- og baksideark bruker samme kortstørrelse og slot-beregning. For best fysisk treff må utskrift normalt kjøres uten skalering, og skriveren må ha god nok mating/duplex-presisjon.

Printark kan få en valgfri `10 cm` målelinje med `1 cm`-ticks nederst på arket. Den brukes til å sjekke med linjal etter utskrift at skriver/PDF-viewer ikke har skalert arket feil.

Verktøyet bør støtte:

* Arkstørrelser som A4 og A3.
* Kortstørrelse i millimeter.
* Valgbar DPI for draft, standard print og print-mastere.
* Marginer og avstand mellom kort.
* Bleed for klipping.
* Kuttemerker.
* Forside- og baksideark.

## Godot-Rendering

Kortgeneratoren bør bruke Godot som hovedverktøy for layout og rendering.

Anbefalt første oppsett:

```text
SubViewport.size = 1630 x 2220
Trim area = 63 x 88 mm
Bleed area = 3 mm per side
Export area = 69 x 94 mm
```

Kortscenen bør bygges i lag som matcher lagmodellen over. Bakgrunn, rammer og illustrasjoner skal gå helt ut i bleed-området. Viktig tekst, ikoner og tall skal holdes innenfor en trygg sone innenfor ferdig kuttekant.

Anbefalte soner:

```text
Bleed: 3 mm utenfor kuttkant
Safe margin: minst 4 mm innenfor kuttkant
```

Godot kan eksportere PNG direkte fra `SubViewport`. Printark kan bygges senere enten i Godot eller med et eget PDF-/arkverktøy.

Renderer-retningen er at samme renderlogikk brukes av både GUI-preview og CLI/headless eksport. GUI-preview ligger i en gjenbrukbar packed scene, `scenes/card_preview/card_preview.tscn`, med `CardPreviewControl.cs` som script. Det skal hindre at kort ser riktig ut i appen, men eksporteres annerledes fra CLI.

Preview-rendering bruker samme koordinatsystem som full eksport, men renderer direkte til ønsket preview-størrelse. Små kortfliser trenger derfor ikke først å lage full `750x1050`-render og deretter skalere ned.

## CLI Og Headless

CLI skal kunne kjøre uten GUI med Godot headless:

```sh
godot --headless --path godot/godot_cardgeneration -- --command validate-cards
```

Planlagte kommandoer:

* `list-cards`
* `list-decks`
* `show-config`
* `set-config`
* `reset-config`
* `reset-content`
* `import-card`
* `import-deck`
* `delete-card`
* `duplicate-card`
* `delete-deck`
* `duplicate-deck`
* `validate-cards`
* `validate-deck`
* `export-deck`
* `export-sheet`
* `export-diy`
* `export-showcase`

CLI-kommandoene skal bare parse argumenter og kalle samme service-lag som GUI bruker. CLI skal dekke batch/data/export-funksjoner, men trenger ikke å ha full interaktiv redigering av kort- og deckinnhold i denne fasen.

Første implementerte CLI-funksjoner:

* `list-cards` laster lagrede kortresources.
* `list-decks` laster lagrede deckresources.
* `show-config` viser lagrede CLI-/eksportdefaults.
* `set-config` lagrer nye CLI-/eksportdefaults.
* `reset-config` nullstiller settings/config til fabrikkverdier.
* `reset-content` sletter lagrede kort- og deckresources og regenererer defaultkort og decken `default_deck`.
* `import-card` importerer en ekstern `.tres` kortresource til brukerressursene.
* `import-deck` importerer en ekstern `.tres` deckresource til brukerressursene.
* `delete-card` sletter vanlige user card resources og genererte defaultkort; packaged `res://` defaults kan ikke slettes.
* `duplicate-card` kopierer et kort til en vanlig user card resource.
* `delete-deck` sletter vanlige user deck resources og genererte defaultdecks; packaged `res://` defaults kan ikke slettes.
* `duplicate-deck` kopierer en deck til en vanlig user deck resource.
* `validate-cards` validerer lagrede kortresources.
* `validate-deck` validerer en lagret eller innebygd deck.
* `export-deck` renderer en kortstokk til PNG som enkeltbilder, samlet grid eller lang strip.
* `export-sheet` renderer A4/A3-printark med egne front- og baksideark, valgbar DPI, valgfri bakside-speiling og valgfri 10 cm målelinje.
* `export-diy` renderer både A4- og A3-printark for en kortstokk, med samme DPI, bakside-speiling og målelinjevalg.
* `export-showcase` renderer en showcase-grid for en kortstokk.

`export-showcase` bruker foreløpig samme grid-output som `export-deck --layout grid`.

Langvarige innstillinger ligger i `resources/config/card_tool_config.tres`. GUI bruker denne configen til å fylle inn startverdier når Export åpnes, men valg gjort i Export er bare for den ene eksporten. CLI bruker configen som defaults når flagg ikke oppgis. Dette gjør repeterende kommandoer korte, for eksempel kan `--command export-sheet` bruke lagret deck, output, papir, DPI og layout. Den samme configen redigeres i GUI under `Settings`.

## Mappestruktur

```text
godot/godot_cardgeneration/
  assets/
    icons/
      elements/
      symbols/
    placeholders/
  docs/
  resources/
    cards/
      monsters/
      terrain/
    config/
    decks/
    elements/
  scenes/
    main_menu/
  scripts/
    app/
    cli/
    resources/
    services/
    ui/
      CardToolScreen.cs
      CardEditorScreen.cs
      DeckEditorScreen.cs
      ExportCenterScreen.cs
      SavedCardsScreen.cs
      SavedDecksScreen.cs
      SettingsPanel.cs
```

`assets/icons/` inneholder SVG-ikoner som brukes på kort og i verktøyet. Nye spillikoner skal legges eller genereres her når kortgeneratoren trenger dem.

`assets/placeholders/` inneholder midlertidige kortbilder til faktiske monster- og terrengbilder finnes.

`resources/` skal inneholde lagrede Godot resources for elementer, kort og kortstokker.

Kuraterte/default resources ligger under `res://resources/cards/` og `res://resources/decks/` og lastes automatisk sammen med brukerbiblioteket. Repository-laget leser `.tres` og `.res` rekursivt fra både `res://` og `user://`. Kort og kortstokker som brukeren importerer eller lagrer fra GUI/CLI skrives under `user://resources/`, slik at det fungerer også i eksportert app.

`resources/config/card_tool_config.tres` inneholder langvarige defaults for CLI og eksport. Brukerinnhold ligger i Godot `user://resources/...`.

Første sample-data ligger i:

* `resources/elements/`
* `resources/config/card_tool_config.tres`
* `resources/decks/sample_monster_deck.tres`

I tillegg genererer `DefaultDeckFactory` decken `default_deck` og de 52 tilhørende kortene med `default_`-prefix ved oppstart hvis de mangler.

`scripts/resources/` inneholder `Resource`-modellene som kortverktøyet lagrer og leser.

`scripts/services/` inneholder felles funksjonskall som både GUI og CLI skal bruke.

`scripts/cli/` inneholder bare argumenttolking og headless entrypoint.

`scripts/ui/` inneholder GUI-kontrollere.

## Output

Mulig fremtidig outputstruktur:

```text
godot/godot_cardgeneration/output/
  cards/
    600dpi/
    preview/
  decks/
  sheets/
    sample_monster_deck_a4_600dpi_front_001.png
    sample_monster_deck_a4_600dpi_back_001.png
  diy/
  showcase/
```

`output/` er lokal generert output og skal ikke committes.

## Teknisk Retning

Kortgeneratoren bygges som Godot C#-prosjekt. C# brukes for resource-modeller, validering, repositories, rendering services, eksport, CLI og UI-kontrollere.

Godot-scener brukes til layout og preview. Første versjon bruker ikke GDScript.

Det viktigste er at kortdata og grafiske kilder holdes delt, slik at fysisk print, Tabletop Simulator og senere digital visning ikke divergerer.

SVG-kilder og ikoner brukes som grafiske assets. Spill- og kortikoner skal ligge under `assets/icons/`; kortgeneratoren bør ikke være avhengig av å manipulere SVG/XML direkte for første versjon.

## Framdriftsstatus

Første skeleton inneholder Godot-prosjekt, C# project file, hovedmeny, resource-modell, service-lag, CLI-runner, placeholder-assets, egne SVG-ikoner, sample resources, PNG-rendering, deck export, printarkexport og første GUI-skjermer for lagrede kort, lagrede kortstokker, korteditor, deckeditor, export center og settings.

Videre plan ligger i [Framdriftsplan](docs/progress-plan.md).

## Åpne Punkter

* Skal kortdata opprettes manuelt som Godot resources først, eller importeres fra eksisterende Markdown-tabeller først?
* Skal flere type-spesifikke editorfelt for monster og terreng prioriteres før importflyt?
* Skal safe margin, bleed og kuttemerker inn i printark før DIY-eksporten?
