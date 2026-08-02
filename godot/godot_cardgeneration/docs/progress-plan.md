[Back](../README.md)

# Framdriftsplan

Dette dokumentet beskriver planlagt rekkefølge for kortverktøyet.

## Fase 1: Prosjektgrunnlag

Status: gjennomført som første skeleton.

Mål:

* Opprette Godot C#-prosjekt i `godot/godot_cardgeneration/`.
* Lage lokal `.gitignore` for Godot-cache, C# build output og generert output.
* Lage første hovedmeny.
* Lage grunnleggende resource-modell.
* Lage service-stubber som GUI og CLI kan dele.
* Lage CLI-runner som kan kjøres headless.
* Legge inn placeholder-bilder for konge, terreng og monster.
* Legge inn egen ikonmappe med første SVG-ikoner for elementer og kortsymboler.

## Fase 2: Lagring Og Lasting

Status: delvis gjennomført.

Mål:

* Implementere `CardRepository`. Gjort for rekursiv lasting og enkel saving.
* Implementere `DeckRepository`. Gjort for rekursiv lasting og enkel saving.
* Implementere config-lagring for verktøyinnstillinger. Gjort med `CardToolConfigResource` og `ConfigRepository`.
* Lage første `ElementResource`-filer. Gjort.
* Lage første eksempelressurser for monsterkort, terrengkort og kongekort. Monsterkort er gjort; terreng og konge gjenstår.
* Lage første eksempelressurs for kortstokk. Gjort med `sample_monster_deck`.

## Fase 3: Preview Og Editor

Status: delvis gjennomført.

Mål:

* Lage kortpreview-scene. Gjort med `CardPreviewControl`.
* Vise kort basert på `CardResource`. Gjort i kortliste, deckliste og korteditor med front/back preview og større popup ved dobbelklikk.
* Lage enkle skjermer for `Cards` og `Decks`. Gjort.
* Lage første kortopprettingsflyt via `+` i `Cards`. Gjort med korttypevalg før editor, felles kortfelt, type-spesifikke basisfelt, image source path, save og export.
* Lage første deckopprettingsflyt via `+` i `Decks`. Gjort med valg mellom tom deck og default 52-korts preset, deckfelt, tilgjengelige kort, entries, save og export.
* Bruke default 52-korts deck som standarddeck i stedet for én-korts placeholder. Gjort med innebygd `default_52_card_deck` i `CardToolService`.
* Legge til type-spesifikke editorfelt for monster, terreng og konge. Delvis gjort for monsterkrav/grunnstyrke, terrengressurser og kongeliv/oppdragstekst.

## Fase 4: Rendering

Status: delvis gjennomført.

Mål:

* Bygge kort i lag: basebakgrunn, kortbilde, paneler, ikoner/tekst og eventuelle print guides. Første basebakgrunn, kortbilde-fallback, paneler og ikoner er gjort.
* Bruke placeholder-bilder fram til ekte kortbilder finnes.
* Rendre ett kort til PNG. Gjort med første `Image`-baserte renderer.
* Sikre at GUI-preview og CLI-rendering bruker samme renderer. Gjort for første preview/render-service.

Merk: `SubViewport`-basert rendering er fortsatt mulig senere, men første renderer bruker direkte `Image`-bygging for å gjøre headless CLI stabil tidlig.

## Fase 5: Kortstokker Og Showcase

Status: delvis gjennomført.

Mål:

* Rendre alle kort i en kortstokk. Gjort for enkel PNG-eksport.
* Støtte ferdige mixed decks med flere korttyper. Gjort ved å fjerne deck-type-lås og rendre baksider per korttype.
* Eksportere kortstokk som enkeltbilder. Gjort med `--layout individual`.
* Eksportere kortstokk som samlet grid. Gjort med `--layout grid`.
* Eksportere kortstokk som lang strip. Gjort med `--layout strip`.
* Lage showcase-visning for kort og kortstokker. GUI-visning gjenstår.
* Eksportere showcase som bilde eller bildeserie. Første grid-output er koblet til `export-showcase` og GUI Showcase-export.
* Lage samlet GUI-exportside. Gjort med `ExportCenterScreen` for deck images, showcase og print sheets.

## Fase 6: Printark Og DIY

Status: delvis gjennomført.

Mål:

* Eksportere A4-printark med fronter. Gjort som PNG.
* Eksportere A4-printark med baksider. Gjort som PNG.
* Eksportere A3-printark med fronter og baksider. Gjort som PNG.
* Sikre pokerkortstørrelse `63 x 88 mm` ved print. Gjort ved valgbar DPI-pikselberegning.
* Støtte normale DPI-valg for printark. Gjort for `150`, `300`, `600` og `1200`.
* Lage nye nummererte ark automatisk når arket er fullt. Gjort.
* Legge inn safe margin, bleed og kuttemerker. Gjenstår.
* Lage DIY-eksport med kortbilder, printark og måleinformasjon. Gjenstår.

## Fase 7: Dataintegrasjon

Mål:

* Vurdere import fra Markdown-tabellene i `shared/docs/`.
* Sikre at kortdata ikke divergerer mellom docs og Godot resources.
* Legge til validering for ID-er, elementer, tiers, krav, styrker og kortstokker.

## Fase 8: Polering

Mål:

* Forbedre UI-flyt.
* Legge til filtrering, søk og duplisering.
* Legge til bedre feilmeldinger.
* Legge til testbare service-funksjoner der det er nyttig.
* Dokumentere eksportformat og produksjonsflyt tydeligere.

## Fase 9: Innstillinger

Status: delvis gjennomført.

Mål:

* Lagre langvarige CLI-/eksportinnstillinger i redigerbar Godot resource. Gjort med `resources/config/card_tool_config.tres`.
* La CLI bruke configverdier som defaults når flagg ikke oppgis. Gjort.
* La CLI endre config uten å åpne GUI. Gjort med `set-config`.
* Lage GUI for å redigere samme config. Gjort med `SettingsPanel`.
* Gjøre Settings-panelet scrollbart for små vinduer. Gjort.
* Koble Settings-panelet til endelige export-/preview-skjermer når de finnes. Delvis gjort ved at kort-, deck- og exportskjermene leser config-defaults via `CardToolService`.
