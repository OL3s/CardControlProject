[Back](../../README.md)

# Kortgenerering

Denne mappen inneholder Godot-prosjektet for kortverktøyet. Verktøyet skal brukes til å lage, lagre, vise og eksportere kort og kortstokker for fysisk prototype, Tabletop Simulator og senere digital bruk.

## Navigasjon

* [Prosjektoversikt](../../README.md)
* [Fysisk versjon](../../physical/README.md)
* [Godot gameplay](../godot_gameplay/README.md)
* [Aktivt regelutkast](../../shared/docs/gameidea-working.md)
* [Arkitektur](docs/architecture.md)
* [CLI](docs/cli.md)
* [Framdriftsplan](docs/progress-plan.md)

## Formål

Kortgenereringen skal gjøre det mulig å lage kort én gang fra Godot `Resource`-data og grafiske lag, og deretter bruke samme data til preview, bilder, kortstokker, printark og DIY-pakker.

Verktøyet skal ha både GUI og CLI/headless-modus. GUI og CLI skal bruke samme service-lag for lasting, lagring, validering, rendering og eksport. Forskjellen skal primært være inputmetode: GUI bruker menyer og skjema, mens CLI bruker argumenter.

All kode, filnavn, mappenavn og tekniske navn holdes på engelsk. Dokumentasjonen holdes på norsk.

## Hovedflyt I Appen

Første GUI-retning bruker en smal hovedmeny:

* `Cards`
* `Decks`
* `Export`
* `Settings`

`Cards` viser lagrede kort, preview, enkel korteksport og hurtigvalg for å legge et kort til en valgt kortstokk. Nye kort opprettes med `+` inne på denne skjermen. Før editoren åpnes velges korttype, fordi monster-, terreng- og kongekort har ulik data og oppsett.

`Decks` viser lagrede kortstokker, korttelling, preview av første kort, redigering og kortstokkeksport med lagrede defaults. Nye kortstokker opprettes med `+` inne på denne skjermen. `+` kan starte en tom kortstokk eller en default 52-korts preset fra `shared/docs`.

Default deck er `default_52_card_deck`. Den lages av `DefaultDeckFactory` fra `shared/docs` og er tilgjengelig i GUI og CLI selv om den ikke er lagret som `.tres` ennå. `sample_monster_deck` er bare en liten smoke-test resource.

Hovedmenyen skal ikke vise et tilfeldig kortpreview og skal ikke ha egne `New Card`/`New Deck` valg. Kortpreview hører hjemme i `Cards`, `Decks`, editor- og eksportskjermene. Preview viser både front og bakside der det er relevant, og dobbelklikk på preview åpner større visning.

Korteditoren støtter felles kortfelt, image source path, preview, lagring og PNG-eksport. Korttypen er valgt før editoren åpnes. Monsterkort lagrer ikke element direkte; elementet utledes fra ikke-nøytralt ressurskrav. Terrengkort har ikke elementfokus og viser bare hvilke ressurser de produserer. Kongekort er eneste korttype som lagrer eksplisitt `ElementFocus`, fordi kongen skal vise elementikon øverst til venstre.

Deckeditoren støtter deck-ID, tilgjengelige kort på tvers av korttyper, deck entries med antall, `Save` og `Save New`. Venstre side viser lagrede kort som preview-fliser i en horisontal scrollrad med ikonknapper for å legge til én kopi eller velge flere kort. Høyre side viser deckinnhold som preview-fliser med ikonknapper for slett, dupliser og multiselect. Eksport gjøres bare fra `Export`-skjermen. En deck er et ferdig produkt med alle relevante korttyper, ikke en separat monster-/terreng-/kongebunke.

`Export` er eneste eksportflate. Den kan eksportere ett lagret kort som PNG, eller en lagret kortstokk som vanlige deck images eller printarkexport med A4/A3 og DPI-valg.

Export-skjermen har en output path-velger som åpner Godots file dialog i valgt/default exportmappe.

`Settings` redigerer den samme config-resourcen som CLI bruker. Endringer gjort i GUI skal derfor påvirke senere CLI-kjøringer, og `set-config` i CLI skal påvirke GUI-innstillingene.

## Nåværende Flyt

1. Les kortdata fra Godot resources.
2. Bygg kortet i lag med den felles `Image`-baserte rendereren.
3. Bruk samme renderer i GUI-preview og CLI/headless eksport.
4. Eksporter individuelle kortbilder, grid, strip eller printark som PNG.
5. Plasser kortene på A4/A3-ark i pokerkortstørrelse basert på valgt DPI.

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

`base_background` er et heldekkende fargelag nederst. Fargen styres av korttypen, ikke av elementet. Målet er at spilleren raskt skal se om kortet er monsterkort, terrengkort eller kongekort når kortet ligger med riktig side opp.

`card_image` ligger oppå basebakgrunnen. For monsterkort er dette monsterbildet. For terrengkort er det terrengbildet. For kongekort er det konge-/bakgrunnsbildet. De endelige bildene finnes ikke ennå, så prosjektet bruker placeholder-bilder i første versjon.

`panels` ligger oppå kortbildet og samler spillinformasjon. Panelene skal gjøre ikoner og eventuell tekst lesbare uavhengig av hvor detaljert kortbildet er.

`icons_and_text` inneholder ressursikoner, styrkeikoner, piler, korttekst og annen spillinformasjon. Elementresources peker til SVG-ikonene under `assets/icons/elements/`, og renderer faller tilbake til enkle symboler dersom en texture ikke kan lastes.

Monsterets elementvisning følger kravlisten: dersom kravlisten har ett ikke-nøytralt element, brukes dette som monsterets element. Hvis kravlisten bare har nøytral, blir monsteret nøytralt. Terrengkort har ingen egen elementvisning utover ressursikonene de produserer. Kongekort viser `ElementFocus` som ikon øverst til venstre.

`print_guides` er kun for fysisk produksjon, for eksempel kuttemerker, bleed eller hjelpelinjer. Slike lag skal ikke være del av vanlig preview med mindre brukeren eksplisitt velger det.

Baksiden bygges også i lag. Nederst ligger samme korttypebaserte basefarge/kant som skiller korttypen. Oppå den ligger baksidebildet for korttypen. Baksiden skal ikke avsløre kortspesifikke data som element, krav, styrke, tier eller effekt. Printark bruker korttypen til hvert enkelt kort, siden én ferdig deck kan inneholde konger, terreng og monstre.

Kortbaksidene ligger som SVG under `assets/card_backs/` og er kopiert fra `shared/docs/images/svg/`. Monsterbaksiden er tonet litt ned i rødfargen for å fungere bedre med gress- og vannmonstre.

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
* `validate-cards`
* `validate-deck`
* `render-card`
* `export-deck`
* `export-sheet`
* `export-diy`
* `export-showcase`

CLI-kommandoene skal bare parse argumenter og kalle samme service-lag som GUI bruker.

Første implementerte CLI-funksjoner:

* `list-cards` laster lagrede kortresources.
* `list-decks` laster lagrede deckresources.
* `show-config` viser lagrede CLI-/eksportdefaults.
* `set-config` lagrer nye CLI-/eksportdefaults.
* `validate-cards` validerer lagrede kortresources.
* `render-card` renderer ett kort til PNG.
* `export-deck` renderer en kortstokk til PNG som enkeltbilder, samlet grid eller lang strip.
* `export-sheet` renderer A4/A3-printark med egne front- og baksideark og valgbar DPI.

`export-diy` finnes foreløpig som service-/CLI-stub. `export-showcase` bruker foreløpig samme grid-output som `export-deck --layout grid`.

Langvarige innstillinger ligger i `resources/config/card_tool_config.tres`. CLI bruker denne configen som defaults når flagg ikke oppgis. Dette gjør repeterende kommandoer korte, for eksempel kan `--command export-sheet` bruke lagret deck, output, papir, DPI og layout. Den samme configen redigeres i GUI under `Settings`.

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
      kings/
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

`assets/placeholders/` inneholder midlertidige kortbilder til faktiske monster-, terreng- og kongebilder finnes.

`resources/` skal inneholde lagrede Godot resources for elementer, kort og kortstokker.

Kuraterte/default resources ligger under `resources/cards/` og `resources/decks/`. Kort og kortstokker som brukeren lagrer fra GUI/CLI skrives under `resources/user/`, som er ignorert av git.

`resources/config/card_tool_config.tres` inneholder langvarige defaults for CLI og eksport.

Første sample-data ligger i:

* `resources/elements/`
* `resources/config/card_tool_config.tres`
* `resources/cards/monsters/monster_flame_1_a.tres`
* `resources/decks/sample_monster_deck.tres`

I tillegg finnes `default_52_card_deck` som innebygd preset fra `DefaultDeckFactory`.

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
* Skal type-spesifikke editorfelt for monster, terreng og konge prioriteres før importflyt?
* Skal safe margin, bleed og kuttemerker inn i printark før DIY-eksporten?
