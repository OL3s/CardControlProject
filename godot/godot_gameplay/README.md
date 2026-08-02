[Back](../README.md)

# Godot-versjon

Denne mappen dokumenterer den digitale versjonen av spillet. Den digitale versjonen skal bygges i Godot med C#.

## Status

Godot-versjonen er ikke implementert ennå. Denne dokumentasjonen beskriver forventninger, avgrensninger og åpne punkter før selve prosjektstrukturen og koden etableres.

## Navigasjon

* [Tilbake til prosjektoversikt](../README.md)
* [Fysisk versjon](../physical/README.md)
* [Aktivt regelutkast](../shared/docs/gameidea-working.md)

## Relaterte Områder

* [Felles dokumenter](../shared/docs/gameidea-working.md) er designkilden for regler og kort.
* [Fysisk versjon](../physical/README.md) dekker papirark, 3D-print og bordspillspesifikke valg.

## Formål

Godot-versjonen skal gjøre spillet spillbart digitalt med samme kjerne som den fysiske versjonen. Felles regler, kortlister og kortverdier skal hentes fra `../shared/docs/` som designkilde.

## Hva Hører Hjemme Her?

* Godot-prosjektfiler og C#-kode når implementeringen starter.
* Digital spillflyt, menyer, input og UI-avklaringer.
* Regeltolkninger som kun gjelder digital gjennomføring.
* AI-motstandere og logikk for solospill.
* Flerspillerflyt for spill mot andre spillere.
* Tekniske notater om scener, ressurser, lagring og testoppsett.

## Digitalt Omfang

Den digitale versjonen skal støtte:

* Spill mot AI.
* Spill mot andre spillere.
* Automatisert håndtering av regelsteg der det gir mening.
* Tydelig visualisering av kontroll, ressurser, konflikter og kongeliv.
* Kort og regler basert på de felles dokumentene i `../shared/docs/`.

## C#-Retning

Kode skal skrives på engelsk, selv om dokumentasjonen er norsk. Navn på klasser, metoder, scener, ressurser og datafiler bør være konsekvente og lesbare for et C#-basert Godot-prosjekt.

## Åpne Punkter

* Skal flerspiller først være lokal, nettbasert eller begge deler?
* Hvor mye av regelmotoren skal være ren C# uten direkte Godot-avhengigheter?
* Hvordan skal kortdata lagres digitalt når prototypen går fra dokumentasjon til implementering?
* Hvor avansert skal første AI-motstander være?
* Hvilke deler av fysisk informasjonsflyt skal automatiseres, og hvilke bør spilleren fortsatt velge manuelt?
