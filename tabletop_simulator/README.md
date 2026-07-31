[Back](../README.md)

# Tabletop Simulator

Denne mappen dokumenterer Tabletop Simulator-versjonen av spillet. Formålet er å gjøre spillet raskt spillbart og testbart digitalt uten å vente på full Godot-implementering eller fysisk print.

## Navigasjon

* [Tilbake til prosjektoversikt](../README.md)
* [Aktivt regelutkast](../shared/docs/gameidea-working.md)
* [Kortgenerering](../godot_cardgeneration/README.md)
* [Fysisk versjon](../physical/README.md)

## Formål

Tabletop Simulator-versjonen skal brukes til playtesting av regler, kortbalanse, tempo og bordflyt. Den skal ligge nær den fysiske versjonen, men kan bruke digitale hjelpemidler der det gjør testing enklere.

## Hva Hører Hjemme Her?

* Notater om Tabletop Simulator-oppsett.
* Importerte kortbilder og kortstokker.
* Mod- og save-struktur.
* Playtestnotater fra Tabletop Simulator.
* Avklaringer som gjelder digital bordspillflyt, men ikke full Godot-implementering.

## Forventet Bruk

Tabletop Simulator-versjonen bør prioritere rask testing fremfor teknisk perfeksjon. Den skal gjøre det enkelt å:

* Teste 2-4 spillere.
* Dele kortstokker og komponenter digitalt.
* Iterere raskt på kortverdier og regler.
* Sammenligne digital bordflyt med fysisk prototype.

## Forhold Til Kortgenerering

Kortbilder bør helst komme fra [`../godot_cardgeneration/`](../godot_cardgeneration/README.md), slik at Tabletop Simulator, fysisk print og senere Godot-visning kan bruke samme kilder.

## Mulig Fremtidig Struktur

```text
tabletop_simulator/
  README.md
  decks/
  saves/
  mod-notes/
  playtest-notes/
```

## Åpne Punkter

* Skal Tabletop Simulator bruke samme kortbilder som printarkene, eller egne eksportformater?
* Hvordan skal kortstokker organiseres for rask import?
* Hvilke komponenter må finnes i første spillbare mod?
* Skal playtestnotater her oppsummeres tilbake i felles regelutkast?
