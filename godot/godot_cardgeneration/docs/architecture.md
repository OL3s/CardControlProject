[Back](../README.md)

# Arkitektur

Kortverktøyet er et Godot C#-prosjekt som skal fungere både som GUI-app og CLI/headless-verktøy. Begge innganger skal bruke samme service-lag for batch/data/export-funksjoner. Interaktiv redigering av kort og kortstokker kan være GUI-only i denne fasen.

## Prinsipper

* Dokumentasjon skrives på norsk.
* Kode, klassenavn, filnavn, mappenavn og tekniske ID-er skrives på engelsk.
* GUI og CLI skal ikke ha hver sin import-, validerings-, render- eller eksportlogikk.
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
    MonsterCardResource
  ResourceAmount
  PowerBonusResource
  CardEffectResource
  CardToolConfigResource
  CardDeckResource
  CardDeckEntryResource
```

`ElementResource` representerer både elementdata og elementvisning for kortverktøyet. Den har `ElementType`, `DisplayName`, `IconTexture` og enkle metoder for å sjekke styrke, svakhet og nøytralitet mot andre elementer.

Elementikonene peker til importerte textures fra SVG-er under `assets/icons/elements/`. Rendereren bruker disse når de kan lastes, og tegner fallback-symboler når texture mangler.

`CardResource` er felles base for alle kort. Den inneholder ID, korttype, teksturer for kortbilde og baksidebilde, samt `CardImageSourcePath` for bilde importert fra filsystemet. Rendereren bruker source path når `CardImageTexture` ikke er satt. Visningsnavn, notater, beskrivelser, interne designkategorier og generelt kortelement er bevisst utelatt fra kortdata.

`MonsterCardResource` har krav, grunnstyrke, bonuslinjer og eventuell effekt. Monsterets element lagres ikke separat; `CardElementResolver` utleder element fra kravlisten.

`TerrainCardResource` har produserte ressurser. Terreng har ikke elementfokus som metadata.

`CardDeckResource` lagrer en ferdig produktdeck som ID og en liste med `CardDeckEntryResource`, slik at samme kort kan ha antall kopier uten å duplisere kortdata. Deck-beskrivelse og displaynavn er bevisst utelatt.

`CardElementResolver.GetSingleNonNeutralElementType(ResourceAmount[])` er delt API for å utlede monster-element fra kost-/ressursliste. Nøytrale krav gir nøytralt monster; ett ikke-nøytralt krav gir dette elementet. Terreng bruker ikke resolveren som elementfokus.

En deck kan inneholde både terreng og monstre i samme 52-korts produkt. Printark renderer derfor bakside per korttype for hvert kort. `MonsterBackImageTexture` og `TerrainBackImageTexture` kan brukes som deck-spesifikke overrides per korttype.

`CardToolConfigResource` lagrer langvarige verktøyinnstillinger som default card, deck, output, format, papir, DPI, bakside-speiling, deck layout, gridkolonner og spacing. Default card er tom etter fabrikkreset; default deck er `default_deck`. Configen ligger som redigerbar Godot resource i `resources/config/card_tool_config.tres`.

Configen er felles for GUI og CLI. GUI Settings-panelet og CLI-kommandoene `show-config`/`set-config`/`reset-config` leser og skriver samme resource.

## Service-Lag

Felles funksjoner ligger i `scripts/services/`.

```text
CardToolService
  CardRepository
  DeckRepository
  CardValidator
  DeckValidator
  CardFactory
  CardRenderService
  DeckExportService
  SheetExportService
  DiyExportService
  DefaultDeckFactory
  ConfigRepository
```

`CardToolService` er fasaden som GUI og CLI skal kalle. Den skal samle vanlige operasjoner som lasting, lagring, validering, rendering og eksport.

`CardFactory` lager nye tomme kort basert på valgt korttype. Den setter startverdier som matcher modellforskjellene mellom monster og terreng.

Repositories skal eie lasting og lagring av kort og kortstokker. Vanlig GUI-listing leser curated/default resources under `res://resources/...` og brukerbiblioteket under `user://resources/...`, der user resources overstyrer default resources med samme ID.

Repository-implementasjonen kan lese `.tres` og `.res` rekursivt fra:

* `res://resources/cards`
* `res://resources/decks`
* `user://resources/cards`
* `user://resources/decks`

Cards/Decks-listene viser både default resources og `user://resources/...`. Ved oppstart sørger `CardToolService.EnsureDefaultResources()` for at decken `default_deck` og alle kortene fra den finnes. Manglende defaultkort lagres under `user://resources/cards/default/...` med `default_`-prefix, og manglende defaultdeck lagres under `user://resources/decks/default/default_deck.tres`.

Packaged default resources under `res://` er read-only. Repositoryene nekter å overskrive `res://` resources og genererte `user://resources/.../default` resources. Genererte defaultkort/decks under `user://resources/.../default` kan slettes og lages på nytt ved oppstart hvis de mangler. Endringer i defaults må lagres via `Save as new` eller duplicate, som skriver til vanlige user resource-mapper.

`ConfigRepository` laster og lagrer `res://resources/config/card_tool_config.tres`.

Validators skal sjekke at kort og kortstokker er gyldige før preview, lagring og eksport.

Render- og export-services skal eie all outputlogikk.

Første `CardRenderService` renderer PNG direkte fra `CardResource`.

`DeckExportService` støtter tre PNG-layouts for vanlig kortstokkeksport:

* `individual`: ett PNG-bilde per kort i egen mappe.
* `grid`: ett samlet PNG-bilde med kortene i rutenett.
* `strip`: ett langt vertikalt PNG-bilde.

`SheetExportService` støtter A4 og A3 som printark. Den lager nummererte front- og baksideark. Kortene plasseres i pokerkortstørrelse, `63 x 88 mm`, beregnet fra valgt DPI. Baksidearket kan speiles langs width, height eller begge etter at baksidene er plassert. Printark kan også reservere en bunnstripe med en `10 cm` målelinje med centimeter-ticks for utskriftsskalering.

`DiyExportService` bruker samme printarkexport og lager både A4- og A3-varianter i egne undermapper.

`shared/docs/terrain-cards.md` og `shared/docs/monster-cards.md` er source of truth for defaultkortene i **Elements: Conquora**. `DefaultDeckFactory` speiler disse tabellene og lager deck-startpunkter for GUI: tom deck og en default 52-korts deck med 20 terreng og 32 monstre. Hvert av de fire monsterelementene har 4 Tier 1-, 3 Tier 2- og 1 Tier 3-monster. Factoryen bruker eksisterende lagrede kortresources når `default_`-ID finnes, og lager ellers preset-kort med `default_`-prefix. De manglende preset-kortene lagres som card resources før decken `default_deck` lagres. En innholdsversjon rydder og regenererer bare genererte defaults når presetformatet endres.

DPI velges fra faste normalverdier: `150`, `300`, `600` og `1200`. `600 DPI` er default og print-master-kvalitet.

Arkdelingen regnes slik: service-laget regner ut hvor mange `63 x 88 mm`-kort som får plass på valgt arkstørrelse ved valgt DPI, setter `cardsPerSheet = columns * rows`, og bruker `sheetCount = ceil(cardCount / cardsPerSheet)`. Dersom kortstokken ikke får plass på ett ark, genereres flere ark som `front_001`, `back_001`, `front_002`, `back_002` osv.

`CardImageRenderer` bruker `750x1050` som kanonisk koordinatsystem, men kan rendere direkte til en mindre target-størrelse for GUI-preview. Lavnivåtegningen skalerer rektangler og radier mot faktisk bilde, slik at små preview-fliser ikke trenger full eksportoppløsning først.

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

Kortbaksider ligger under `assets/card_backs/`:

```text
assets/card_backs/
    monster_card_back.svg
    terrain_card_back.svg
```

Elementikonene brukes for ressurser, krav og elementvisning. `ElementResource`-filene peker til SVG-textures under `assets/icons/elements/`. Symbolikonene brukes for generelle kortmarkører som styrke og bonuspiler.

Nye ikoner skal legges eller genereres her når de trengs av kortgeneratoren. Kortdata bør referere til ikonene via `Texture2D`-felter, ikke ved å hardkode SVG-paths i rendering-logikken.

Eksisterende SVG-er kan erstattes med mer polerte versjoner senere uten å endre resource-modellen.

## GUI

GUI starter med en enkel hovedmeny med disse valgene:

* `Cards`
* `Decks`
* `Export`
* `Settings`

GUI-kode skal ligge i `scripts/ui/`. UI-kontrollere kan bygge scener, men skal ikke eie kortlogikk eller eksportlogikk.

`CardToolScreen` er felles base for vanlige appskjermer. Den bygger bakgrunn, header, back-knapp, scrollbart innhold og statuslinje. `MainMenu` eier navigasjon mellom skjermene og injiserer samme `CardToolService` i hver skjerm.

Opprettelse av nye kort og kortstokker skal ligge inne i `Cards` og `Decks` som en kompakt `+`-handling. Hovedmenyen skal være smidig og bare vise de øverste arbeidsområdene. I `Cards` går `+` først til valg av `Monster` eller `Terrain`. I `Decks` gir `+` valg mellom tom deck og default 52-korts preset.

Implementerte skjermer:

* `SavedCardsScreen`: driver `Cards`-skjermen, laster lagrede kort via `CardToolService`, viser preview, edit, duplicate og delete.
* `SavedDecksScreen`: driver `Decks`-skjermen, laster lagrede deckresources, viser korttelling, korttypesammensetning, full preview, edit, duplicate, delete og deckoppretting fra tom/preset.
* `CardTypePickerScreen`: velger korttype før nytt kort åpnes i editor.
* `CardEditorScreen`: lager eller redigerer kort med ID, image source path, front/back preview, fullscreen preview, save og PNG-export. Korttypen er låst etter typevalg.
* `DeckEditorScreen`: lager eller redigerer kortstokk med deck-ID, tilgjengelige kort, entries med count, `Save` og `Save New`. Tilgjengelige kort vises som horisontalt scrollbare preview-fliser med kompakte ikonknapper for add og select. Deckinnhold vises som preview-fliser med count-badge og ikonknapper for delete, duplicate og select. Multiselect brukes for batch add/remove. Skjermen eksporterer ikke.
* `ExportCenterScreen`: eneste eksportflate. Den tilbyr `Images` for enkeltkort, individuelle deckbilder, grid og strip, og `Print` for A4/A3-ark til fysisk utskrift og kutting. Begge typene har preview. Print kan bruke vanlig slot-alignment eller `Easy backs`, som grupperer forsider etter korttype og fyller hele det tilhørende baksidearket.

Første editor dekker fellesfeltene i kortmodellen og type-spesifikke felt for monsterkrav og terrengproduksjon.

`SettingsPanel` redigerer `CardToolConfigResource` via `CardToolService.SetConfig()`. Panelet skal ikke skrive config direkte utenom service-laget. Settings-innholdet ligger i en `ScrollContainer` slik at alle felt er tilgjengelige i små vinduer.

Hovedmenyen skal være navigasjon, ikke previewflate. Kortpreview skal ligge i kortliste, kortstokk, editor eller eksportflyt. `CardPreviewControl` er koblet til `scenes/card_preview/card_preview.tscn`, kan vise front eller bakside, og dobbelklikk åpner en større popup av valgt side.

## CLI

CLI-kode ligger i `scripts/cli/`. CLI skal parse argumenter, kalle `CardToolService` og returnere exit code.

CLI skal ikke ha egne varianter av validering, rendering eller eksport.

CLI bruker lagret config som defaults for repeterende arbeid. Hvis et flagg ikke er oppgitt, hentes verdien fra `CardToolConfigResource`. `set-config` endrer bare feltene som faktisk oppgis i kommandoen. `reset-content` sletter lagrede kort- og deckresources og regenererer defaultkort og decken `default_deck`.

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

Basebakgrunnen bruker separate, dempede nøytraltoner for monster og terreng. Kort uten image source path bruker samme grafittfargede placeholder for begge korttyper, slik at placeholderen ikke signaliserer et element. Et ikke-tomt path som ikke kan finnes eller lastes bruker en separat crossed-image-placeholder. Monsterets element utledes fra kost og påvirker ikonbruk. Terrengkort har ikke eget elementfokus.

Faktiske monster- og terrengbilder finnes ikke ennå. `assets/placeholders/` brukes som midlertidige bilder fram til de endelige bildene finnes.

Første renderer kan tegne en enkel fallback dersom `CardImageTexture` ikke er satt. Dette gjør at sample-kort kan rendres i headless-modus før de faktiske kortbildene og importerte textures er klare.
