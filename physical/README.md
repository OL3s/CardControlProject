[Back](../README.md)

# Fysisk Versjon

Denne mappen dokumenterer den fysiske bordspillversjonen av spillet.

## Status

Den fysiske versjonen er i prototypefase. Felles regler og kortdata ligger i `../shared/docs/`, mens denne mappen skal samle det som gjelder fysisk produksjon, testing og komponenter.

## Navigasjon

* [Tilbake til prosjektoversikt](../README.md)
* [Godot-versjon](../godot/README.md)
* [Kortgenerering](../godot_cardgeneration/README.md)
* [Aktivt regelutkast](../shared/docs/gameidea-working.md)

## Relaterte Områder

* [Felles dokumenter](../shared/docs/gameidea-working.md) er designkilden for regler og kort.
* [Godot-versjon](../godot/README.md) dekker digital implementering, C#, AI og spill mot andre spillere.
* [Kortgenerering](../godot_cardgeneration/README.md) dekker verktøyet som skal bygge kortbilder og printbare ark.

## Formål

Den fysiske versjonen skal gjøre spillet mulig å teste og spille rundt et bord. Den skal bruke samme kjerne som Godot-versjonen, men dokumentere praktiske valg som bare gjelder fysisk bruk.

## Hva Hører Hjemme Her?

* Papirark for print-and-play.
* Notater om kortprint, komponentark og prototypeark.
* 3D-modeller og filer for 3D-printing.
* Spesifikasjoner for brikker, markører, konger og andre fysiske komponenter.
* Bordoppsett, lesbarhet og fysisk ergonomi.
* Playtest-notater som gjelder fysisk gjennomføring.

## Fysisk Omfang

Den fysiske versjonen skal etter hvert dekke:

* Kortark for konge-, terreng- og monsterkort.
* Papirark for regler, referanser og playtest.
* 3D-printbare modeller for bønder, konger, markører og eventuelle spesialkomponenter.
* Praktiske anbefalinger for kortstørrelse, utskrift, klipping og bordplass.
* Testoppsett for 2-4 spillere.

## Produksjonsretning

Fysiske filer bør holdes ryddig adskilt etter formål når de legges til senere. Filnavn og mappenavn holdes på engelsk, selv om dokumentasjonen forklarer innholdet på norsk.

Kort og printark bør genereres fra felles kortdata og grafiske lag, ikke tegnes manuelt på nytt for hvert format. Målet er at samme kilde kan brukes til både fysisk print og digital visning.

## Kortgenerering Og Printark

Kortgenerering dokumenteres i [`../godot_cardgeneration/`](../godot_cardgeneration/README.md). Verktøyet skal kunne bygge ferdige kortbilder fra lag og plassere dem på ark for fysisk print.

Verktøyet bør støtte:

* Individuelle kortbilder for konge-, terreng- og monsterkort.
* Kort som bygges av lag som bakgrunn, ramme, illustrasjon, ikoner, tekst og effekter.
* Printbare ark som A4 og eventuelt andre A-formater senere.
* Riktig fysisk kortstørrelse målt i millimeter.
* Marginer, mellomrom, bleed og kuttemerker.
* Separate filer for forside og bakside dersom det trengs for print.
* Eksport til PDF for print og PNG for preview/testing.

Fysisk versjon bør bare dokumentere kravene til utskrift og produksjon her. Selve verktøyets struktur, dataflyt og tekniske valg bør ligge i `../godot_cardgeneration/`.

Mulig fremtidig struktur:

```text
physical/
  README.md
  sheets/
  models/
  print-notes/
  output/
```

## Åpne Punkter

* Hvilket format skal papirarkene bruke først: A4, Letter eller begge?
* Skal kortark genereres fra SVG-kildene i `../shared/docs/images/svg/`?
* Hvilke komponenter bør 3D-printes først?
* Hvor små kan brikker og ikoner være før bordlesbarheten blir dårlig?
* Trenger fysisk versjon egne referanseark for kamp, kontroll og ressurser?
