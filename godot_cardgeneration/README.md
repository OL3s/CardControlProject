[Back](../README.md)

# Kortgenerering

Denne mappen dokumenterer verktøyet som skal generere kortbilder og printbare ark for den fysiske prototypen.

## Navigasjon

* [Tilbake til prosjektoversikt](../README.md)
* [Fysisk versjon](../physical/README.md)
* [Godot-versjon](../godot/README.md)
* [Aktivt regelutkast](../shared/docs/gameidea-working.md)

## Formål

Kortgenereringen skal gjøre det mulig å lage kort én gang fra felles data og grafiske lag, og deretter eksportere dem til både enkeltbilder og printbare ark.

## Forventet Flyt

1. Les kortdata fra felles kilde.
2. Bygg kortet i lag: bakgrunn, ramme, illustrasjon, ikoner, tekst og eventuelle effekter.
3. Eksporter individuelle kortbilder for preview og testing.
4. Plasser kortene på ark i riktig fysisk størrelse.
5. Eksporter printark som PDF, og eventuelt PNG for rask preview.

## Lagmodell

Et kort bør kunne bygges omtrent slik:

```text
card
  background
  frame
  art
  element_icon
  requirement_icons
  power_icon
  text
  effects
  print_guides
```

`print_guides` er kun for fysisk produksjon, for eksempel kuttemerker, bleed eller hjelpelinjer.

## Printkrav

Verktøyet bør støtte:

* Arkstørrelser som A4 først, og andre A-formater senere ved behov.
* Kortstørrelse i millimeter.
* DPI for rastereksport, for eksempel 300 DPI.
* Marginer og avstand mellom kort.
* Bleed for klipping.
* Kuttemerker.
* Forside- og baksideark.

## Output

Mulig fremtidig outputstruktur:

```text
physical/output/cards/
  monster_flame_1_a.png
  terrain_water_2_a.png

physical/output/sheets/
  monster_cards_a4_front.pdf
  monster_cards_a4_back.pdf
  terrain_cards_a4_front.pdf
  terrain_cards_a4_back.pdf
```

## Teknisk Retning

Godot-spillet skal bygges med C#, og dette verktøyet kan også bygges i C# for å holde teknologistacken samlet. Det viktigste er likevel at kortdata og grafiske kilder holdes delt, slik at fysisk print og digital visning ikke divergerer.

## Åpne Punkter

* Skal verktøyet være et C# console-verktøy, et Godot editor-verktøy eller noe annet?
* Skal kortdata først ligge i JSON, CSV, Godot resources eller et annet format?
* Skal SVG-kildene rendres direkte, eller skal verktøyet bruke ferdige PNG-lag?
* Hvilken kortstørrelse skal brukes i første fysiske prototype?
* Skal A4 være eneste arkformat i første versjon?
