[Back](../README.md)

# Arkitektur

Kortverktøyet er et Godot C#-prosjekt som skal fungere både som GUI-app og CLI/headless-verktøy. Begge innganger skal bruke samme service-lag.

## Prinsipper

* Dokumentasjon skrives på norsk.
* Kode, klassenavn, filnavn, mappenavn og tekniske ID-er skrives på engelsk.
* GUI og CLI skal ikke ha hver sin eksportlogikk.
* Kortdata lagres som Godot `Resource` der det er praktisk.
* Renderer skal kunne brukes av både live preview og batch-eksport.
* Generert output skal ligge i `output/` og ikke committes.
* Spill- og kortikoner skal ligge som SVG under `assets/icons/` når verktøyet trenger dem.

## Resource-Modell

Kortdata bygges rundt denne arvestrukturen:

```text
Godot Resource
  ElementResource
  CardResource
    TerrainCardResource
    KingCardResource
    MonsterCardResource
  ResourceAmount
  PowerBonusResource
  CardEffectResource
  CardDeckResource
  CardDeckEntryResource
```

`ElementResource` representerer både elementdata og elementvisning for kortverktøyet. Den har `ElementType`, `DisplayName`, `IconTexture` og enkle metoder for å sjekke styrke, svakhet og nøytralitet mot andre elementer.

Elementikonene skal normalt peke til importerte textures fra SVG-er under `assets/icons/elements/`. Første sample-resources lar `IconTexture` være tom slik at headless CLI kan laste og rendere uten å være avhengig av import-cache. Rendereren tegner fallback-symboler når texture mangler.

`CardResource` er felles base for alle kort. Den inneholder felles identitet, korttype, element, intern tier og teksturer for kortbilde og baksidebilde.

`MonsterCardResource` har krav, grunnstyrke, bonuslinjer og eventuell effekt.

`TerrainCardResource` har produserte ressurser.

`KingCardResource` har liv, oppdragstekst og eventuelle oppdragskrav.

`CardDeckResource` lagrer en kortstokk som en liste med `CardDeckEntryResource`, slik at samme kort kan ha antall kopier uten å duplisere kortdata.

`CardDeckResource` har også `DeckCardType` og `BackImageTexture`. Baksiden bestemmes på deck-nivå, ikke per kort, slik at en monsterstokk får fast monsterbakside, en terrengstokk får fast terrengbakside og en kongestokk får fast kongebakside.

## Service-Lag

Felles funksjoner ligger i `scripts/services/`.

```text
CardToolService
  CardRepository
  DeckRepository
  CardValidator
  DeckValidator
  CardRenderService
  DeckExportService
  SheetExportService
  DiyExportService
```

`CardToolService` er fasaden som GUI og CLI skal kalle. Den skal samle vanlige operasjoner som lasting, lagring, validering, rendering og eksport.

Repositories skal eie lasting og lagring av kort og kortstokker.

Første repository-implementasjon laster `.tres` og `.res` rekursivt fra:

* `res://resources/cards`
* `res://resources/decks`

Validators skal sjekke at kort og kortstokker er gyldige før preview, lagring og eksport.

Render- og export-services skal eie all outputlogikk.

Første `CardRenderService` renderer PNG direkte fra `CardResource`.

`DeckExportService` støtter tre PNG-layouts for vanlig kortstokkeksport:

* `individual`: ett PNG-bilde per kort i egen mappe.
* `grid`: ett samlet PNG-bilde med kortene i rutenett.
* `strip`: ett langt vertikalt PNG-bilde.

`SheetExportService` støtter A4 og A3 som printark. Den lager nummererte front- og baksideark. Kortene plasseres i pokerkortstørrelse, `63 x 88 mm`, beregnet ved `600 DPI`. Dersom kortstokken ikke får plass på ett ark, genereres flere ark rekursivt som `front_001`, `back_001`, `front_002`, `back_002` osv.

## Ikoner

Ikoner er egne SVG-assets under `assets/icons/`.

```text
assets/icons/
  elements/
    neutral.svg
    grass.svg
    flame.svg
    water.svg
  symbols/
    arrow_right.svg
    power.svg
```

Elementikonene brukes for ressurser, krav og elementvisning. Symbolikonene brukes for generelle kortmarkører som styrke og bonuspiler.

Nye ikoner skal legges eller genereres her når de trengs av kortgeneratoren. Kortdata bør referere til ikonene via `Texture2D`-felter, ikke ved å hardkode SVG-paths i rendering-logikken.

Eksisterende SVG-er kan erstattes med mer polerte versjoner senere uten å endre resource-modellen.

## GUI

Første GUI er en enkel hovedmeny med disse valgene:

* `Saved Cards`
* `Saved Decks`
* `New Card`
* `New Deck`
* `Export`

GUI-kode skal ligge i `scripts/ui/`. UI-kontrollere kan bygge scener, men skal ikke eie kortlogikk eller eksportlogikk.

## CLI

CLI-kode ligger i `scripts/cli/`. CLI skal parse argumenter, kalle `CardToolService` og returnere exit code.

CLI skal ikke ha egne varianter av validering, rendering eller eksport.

## Kortlag

Kortfront bygges nedenfra og opp:

```text
transparent_canvas
base_background
card_image
panels
icons_and_text
print_guides
```

`transparent_canvas` er selve PNG-flaten utenfor kortet. Den skal være transparent, slik at kortet kan legges på andre bakgrunner uten en fast firkant rundt seg.

Kortbakside bygges slik:

```text
base_background_or_border
card_type_back_image
optional_print_guides
```

Basebakgrunnens farge styres av korttype. Elementet skal påvirke ikonbruk og eventuelt kortbilde, men ikke erstatte korttypens grunnidentitet.

Faktiske monster-, terreng- og kongebilder finnes ikke ennå. `assets/placeholders/` brukes som midlertidige bilder fram til de endelige bildene finnes.

Første renderer kan tegne en enkel fallback dersom `CardImageTexture` ikke er satt. Dette gjør at sample-kort kan rendres i headless-modus før de faktiske kortbildene og importerte textures er klare.
