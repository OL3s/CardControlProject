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

`CardResource` er felles base for alle kort. Den inneholder felles identitet, korttype, element, intern tier og teksturer for kortbilde og baksidebilde.

`MonsterCardResource` har krav, grunnstyrke, bonuslinjer og eventuell effekt.

`TerrainCardResource` har produserte ressurser.

`KingCardResource` har liv, oppdragstekst og eventuelle oppdragskrav.

`CardDeckResource` lagrer en kortstokk som en liste med `CardDeckEntryResource`, slik at samme kort kan ha antall kopier uten å duplisere kortdata.

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

Validators skal sjekke at kort og kortstokker er gyldige før preview, lagring og eksport.

Render- og export-services skal eie all outputlogikk.

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
base_background
card_image
panels
icons_and_text
print_guides
```

Kortbakside bygges slik:

```text
base_background_or_border
card_type_back_image
optional_print_guides
```

Basebakgrunnens farge styres av korttype. Elementet skal påvirke ikonbruk og eventuelt kortbilde, men ikke erstatte korttypens grunnidentitet.

Faktiske monster-, terreng- og kongebilder finnes ikke ennå. `assets/placeholders/` brukes som midlertidige bilder fram til de endelige bildene finnes.
