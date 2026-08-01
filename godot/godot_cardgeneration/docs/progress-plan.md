[Back](../README.md)

# Framdriftsplan

Dette dokumentet beskriver planlagt rekkefølge for kortverktøyet.

## Fase 1: Prosjektgrunnlag

Status: startet.

Mål:

* Opprette Godot C#-prosjekt i `godot/godot_cardgeneration/`.
* Lage lokal `.gitignore` for Godot-cache, C# build output og generert output.
* Lage første hovedmeny.
* Lage grunnleggende resource-modell.
* Lage service-stubber som GUI og CLI kan dele.
* Lage CLI-runner som kan kjøres headless.
* Legge inn placeholder-bilder for konge, terreng og monster.

## Fase 2: Lagring Og Lasting

Mål:

* Implementere `CardRepository`.
* Implementere `DeckRepository`.
* Lage eller importere første `ElementResource`-filer.
* Lage første eksempelressurser for monsterkort, terrengkort og kongekort.
* Lage første eksempelressurs for kortstokk.

## Fase 3: Preview Og Editor

Mål:

* Lage kortpreview-scene.
* Vise kort basert på `CardResource`.
* Lage enkle skjermer for `Saved Cards` og `Saved Decks`.
* Lage første `New Card`-flyt.
* Lage første `New Deck`-flyt.

## Fase 4: Rendering

Mål:

* Bygge kort i lag: basebakgrunn, kortbilde, paneler, ikoner/tekst og eventuelle print guides.
* Bruke placeholder-bilder fram til ekte kortbilder finnes.
* Rendre ett kort til PNG via `SubViewport`.
* Sikre at GUI-preview og CLI-rendering bruker samme renderer.

## Fase 5: Kortstokker Og Showcase

Mål:

* Rendre alle kort i en kortstokk.
* Eksportere kortstokk som enkeltbilder.
* Lage showcase-visning for kort og kortstokker.
* Eksportere showcase som bilde eller bildeserie.

## Fase 6: Printark Og DIY

Mål:

* Eksportere A4-printark med fronter.
* Eksportere A4-printark med baksider.
* Legge inn safe margin, bleed og kuttemerker.
* Lage DIY-eksport med kortbilder, printark og måleinformasjon.

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
