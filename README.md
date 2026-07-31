# Card Control Project

Dette repositoryet samler felles regler, kortdesign og prototypearbeid for et kort-, strategi- og områdekontrollspill.

Prosjektet deles i tre hovedområder:

* `shared/` inneholder felles regelverk, kortlister og grafiske kilder.
* `godot/` dokumenterer og skal etter hvert inneholde den digitale Godot-versjonen.
* `godot_cardgeneration/` dokumenterer verktøy for kortgenerering, bildebygging og print-sheets.
* `physical/` dokumenterer og skal etter hvert inneholde den fysiske bordspillversjonen.
* `tabletop_simulator/` dokumenterer Tabletop Simulator-versjonen for digital playtesting.

Dokumentasjonen skrives på norsk. Kode, filnavn, mappenavn, kort-ID-er og tekniske navn holdes på engelsk.

## Navigasjon

* [Godot-versjon](godot/README.md)
* [Kortgenerering](godot_cardgeneration/README.md)
* [Fysisk versjon](physical/README.md)
* [Tabletop Simulator](tabletop_simulator/README.md)
* [Aktivt regelutkast](shared/docs/gameidea-working.md)
* [Original spillide](shared/docs/gameidea.md)
* [Kongekort](shared/docs/king-cards.md)
* [Terrengkort](shared/docs/terrain-cards.md)
* [Monsterkort](shared/docs/monster-cards.md)

## Versjoner

* [Godot-versjon](godot/README.md) dokumenterer den digitale versjonen, C#-retning, AI og spill mot andre spillere.
* [Kortgenerering](godot_cardgeneration/README.md) dokumenterer verktøyet for å bygge kortbilder og printbare ark.
* [Fysisk versjon](physical/README.md) dokumenterer papirark, print-and-play, 3D-print og fysisk prototyping.
* [Tabletop Simulator](tabletop_simulator/README.md) dokumenterer digital bordspill-playtesting i Tabletop Simulator.

## Felles Dokumenter

* `shared/docs/gameidea.md` er originalt idedokument og skal normalt ikke redigeres.
* `shared/docs/gameidea-working.md` er arbeidsdokumentet for regelavklaringer, revisjoner og åpne designvalg.
* `shared/docs/king-cards.md` definerer kongekortene.
* `shared/docs/terrain-cards.md` definerer terrengkortene.
* `shared/docs/monster-cards.md` definerer monsterkortene.
* `shared/docs/images/svg/` inneholder SVG-kilder for kort.
* `shared/docs/images/svg/icons/` inneholder SVG-ikoner brukt av kortkildene.
* `shared/docs/images/png/` inneholder genererte PNG-previewbilder.

## Direkte Dokumentlenker

* [Aktivt regelutkast](shared/docs/gameidea-working.md)
* [Original spillide](shared/docs/gameidea.md)
* [Kongekort](shared/docs/king-cards.md)
* [Terrengkort](shared/docs/terrain-cards.md)
* [Monsterkort](shared/docs/monster-cards.md)
* [SVG-kilder](shared/docs/images/svg/)
* [SVG-ikoner](shared/docs/images/svg/icons/)
* [PNG-previewbilder](shared/docs/images/png/)

## Dokumentasjonsprinsipp

Felles regler og kortdata skal ligge i `shared/docs/`. Godot- og physical-mappene skal bare dokumentere det som er spesifikt for hver versjon, slik at regelverket ikke må vedlikeholdes flere steder.
