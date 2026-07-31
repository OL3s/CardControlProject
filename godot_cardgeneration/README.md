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
3. Render kortet i Godot med en `SubViewport` i riktig pikselstørrelse.
4. Eksporter individuelle kortbilder for preview, testing og print.
5. Plasser kortene på ark i riktig fysisk størrelse.
6. Eksporter printark som PDF, og eventuelt PNG for rask preview.

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

## Output

Mulig fremtidig outputstruktur:

```text
physical/output/cards/
  600dpi/
    monster_flame_1_a.png
    terrain_water_2_a.png
  preview/
    monster_flame_1_a.png
    terrain_water_2_a.png

physical/output/sheets/
  monster_cards_a4_front.pdf
  monster_cards_a4_back.pdf
  terrain_cards_a4_front.pdf
  terrain_cards_a4_back.pdf
```

## Teknisk Retning

Godot-spillet skal bygges med C#, og kortgeneratoren bør også bygges i Godot/C# for å holde teknologistacken samlet. Godot brukes da til layout, preview og PNG-rendering av kort.

Det viktigste er at kortdata og grafiske kilder holdes delt, slik at fysisk print, Tabletop Simulator og senere digital visning ikke divergerer.

SVG-kilder og ikoner kan fortsatt brukes som grafiske assets, men generatoren bør ikke være avhengig av å manipulere SVG/XML direkte for første versjon.

## Åpne Punkter

* Skal verktøyet være et Godot editor-verktøy, en egen Godot-scene eller et C# console-verktøy som starter Godot-rendering?
* Skal kortdata først ligge i JSON, CSV, Godot resources eller et annet format?
* Skal SVG-kildene importeres som assets i Godot, eller skal verktøyet bruke ferdige PNG-lag?
* Skal A4 være eneste arkformat i første versjon?
* Skal preview eksporteres i lavere oppløsning enn print-masteren?
