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

Første GUI-retning bruker denne hovedmenyen:

* `Saved Cards`
* `Saved Decks`
* `New Card`
* `New Deck`
* `Export`

`Saved Cards` skal brukes til liste, filter, preview, showcase og eksport av ett eller flere kort.

`Saved Decks` skal brukes til liste, redigering, preview, showcase, kortstokkeksport, printark og DIY-eksport.

`New Card` skal lage nye `CardResource`-baserte kort.

`New Deck` skal lage nye `CardDeckResource`-baserte kortstokker.

`Export` skal være en samlet batch-side for eksportjobber, men de samme eksportfunksjonene skal også kunne startes fra lagrede kort og lagrede kortstokker.

## Forventet Flyt

1. Les kortdata fra felles kilde.
2. Bygg kortet i lag: bakgrunn, ramme, illustrasjon, ikoner, tekst og eventuelle effekter.
3. Render kortet i Godot med en `SubViewport` i riktig pikselstørrelse.
4. Eksporter individuelle kortbilder for preview, testing og print.
5. Plasser kortene på ark i riktig fysisk størrelse.
6. Eksporter printark som PDF, og eventuelt PNG for rask preview.

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

`icons_and_text` inneholder ressursikoner, styrkeikoner, piler, korttekst og annen spillinformasjon.

`print_guides` er kun for fysisk produksjon, for eksempel kuttemerker, bleed eller hjelpelinjer. Slike lag skal ikke være del av vanlig preview med mindre brukeren eksplisitt velger det.

Baksiden bygges også i lag. Nederst ligger samme korttypebaserte basefarge/kant som skiller korttypen. Oppå den ligger baksidebildet for korttypen. Baksiden skal ikke avsløre kortspesifikke data som element, krav, styrke, tier eller effekt.

## Printkrav

Første printstandard:

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

Verktøyet bør støtte:

* Arkstørrelser som A4 først, og andre A-formater senere ved behov.
* Kortstørrelse i millimeter.
* 600 DPI rastereksport for print-mastere.
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

Renderer-retningen er at samme kortscene brukes av både GUI-preview og CLI/headless eksport. Det skal hindre at kort ser riktig ut i appen, men eksporteres annerledes fra CLI.

## CLI Og Headless

CLI skal kunne kjøre uten GUI med Godot headless:

```sh
godot --headless --path godot/godot_cardgeneration -- --command validate-cards
```

Planlagte kommandoer:

* `list-cards`
* `list-decks`
* `validate-cards`
* `validate-deck`
* `render-card`
* `export-deck`
* `export-sheet`
* `export-diy`
* `export-showcase`

CLI-kommandoene skal bare parse argumenter og kalle samme service-lag som GUI bruker.

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
```

`assets/icons/` inneholder SVG-ikoner som brukes på kort og i verktøyet. Nye spillikoner skal legges eller genereres her når kortgeneratoren trenger dem.

`assets/placeholders/` inneholder midlertidige kortbilder til faktiske monster-, terreng- og kongebilder finnes.

`resources/` skal inneholde lagrede Godot resources for elementer, kort og kortstokker.

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

Første skeleton inneholder Godot-prosjekt, C# project file, hovedmeny, resource-modell, service-stubber, CLI-runner og placeholder-assets.

Videre plan ligger i [Framdriftsplan](docs/progress-plan.md).

## Åpne Punkter

* Skal kortdata opprettes manuelt som Godot resources først, eller importeres fra eksisterende Markdown-tabeller først?
* Skal SVG-kildene importeres som assets i Godot, eller skal verktøyet bruke ferdige PNG-lag i første renderer?
* Skal A4 være eneste arkformat i første versjon?
* Skal preview eksporteres i lavere oppløsning enn print-masteren?
