# Monsterkort

Monsterkort er kampverktøy. Korttypen leses av monsterrammen og baksiden. Elementet styrer kortets ikon, farge og hvilke ressursbonuser det vanligvis bruker. Intern tier brukes bare i kortlisten og ID-en, ikke på selve kortet.

Frontmal: [`images/monster_card_front.svg`](images/monster_card_front.svg)  
Bakside: [`images/monster_card_back.svg`](images/monster_card_back.svg)

## Ikoner Og Styrke

Kravikonene øverst må oppfylles for å bruke monsteret. Baseverdien brukes alltid når monsteret brukes. Bonuslinjer er kumulative: hvis spilleren oppfyller flere linjer, legges alle bonusene til.

Elementfordel gir fortsatt `+1 styrke` i tillegg til monsterets base og bonuslinjer.

## Kortliste

| kort_id | element | intern_tier | krav | base_styrke | bonus_1 | bonus_2 | effekt | maks_styrke_før_elementfordel | synlige_ikoner |
|---|---|---:|---|---:|---|---|---|---:|---|
| `monster_neutral_1_a` | nøytral | 1 | `stein` | 1 | Ingen | Ingen | Ingen | 1 | stein, base 1 |
| `monster_neutral_1_b` | nøytral | 1 | `stein stein` | 1 | `3 stein: +1` | Ingen | Ingen | 2 | 2 stein krav, 3 stein bonus |
| `monster_neutral_1_c` | nøytral | 1 | `stein blad` | 1 | `2 stein: +1` | Ingen | Ingen | 2 | stein, blad, 2 stein bonus |
| `monster_neutral_2_a` | nøytral | 2 | `stein stein` | 2 | `3 stein: +1` | Ingen | Ingen | 3 | 2 stein krav, 3 stein bonus |
| `monster_neutral_2_b` | nøytral | 2 | `stein stein valgfritt_element` | 1 | `3 stein: +1` | `4 stein: +1` | Ingen | 3 | stein, valgfritt, to bonuslinjer |
| `monster_neutral_3_a` | nøytral | 3 | `stein stein stein` | 2 | `4 stein: +1` | `5 stein: +1` | Reduser mottatt bondetap med 1. | 4 | 3 stein krav, 4/5 stein bonus, skjold |
| `monster_grass_1_a` | gress | 1 | `blad` | 1 | Ingen | Ingen | Ingen | 1 | blad, base 1 |
| `monster_grass_1_b` | gress | 1 | `stein blad` | 1 | `2 blad: +1` | Ingen | Ingen | 2 | stein, blad, 2 blad bonus |
| `monster_grass_1_c` | gress | 1 | `blad blad` | 1 | `3 blad: +1` | Ingen | Ingen | 2 | 2 blad krav, 3 blad bonus |
| `monster_grass_2_a` | gress | 2 | `stein blad blad` | 1 | `2 blad: +1` | `3 blad: +1` | Ingen | 3 | stein, 2 blad krav, 2/3 blad bonus |
| `monster_grass_2_b` | gress | 2 | `stein stein blad` | 2 | `3 blad: +1` | Ingen | Ingen | 3 | 2 stein, blad, 3 blad bonus |
| `monster_grass_3_a` | gress | 3 | `stein stein stein blad blad` | 2 | `3 blad: +1` | `4 blad: +1` | Reduser mottatt bondetap med 1. | 4 | 3 stein, 2 blad krav, 3/4 blad bonus, skjold |
| `monster_flame_1_a` | flamme | 1 | `flamme` | 1 | Ingen | Ingen | Ingen | 1 | flamme, base 1 |
| `monster_flame_1_b` | flamme | 1 | `stein flamme` | 1 | `2 flamme: +1` | Ingen | Ingen | 2 | stein, flamme, 2 flamme bonus |
| `monster_flame_1_c` | flamme | 1 | `flamme flamme` | 1 | `3 flamme: +1` | Ingen | Ingen | 2 | 2 flamme krav, 3 flamme bonus |
| `monster_flame_2_a` | flamme | 2 | `stein flamme flamme` | 1 | `2 flamme: +1` | `3 flamme: +1` | Ingen | 3 | stein, 2 flamme krav, 2/3 flamme bonus |
| `monster_flame_2_b` | flamme | 2 | `stein stein flamme` | 2 | `3 flamme: +1` | Ingen | Ingen | 3 | 2 stein, flamme, 3 flamme bonus |
| `monster_flame_3_a` | flamme | 3 | `stein stein stein flamme flamme` | 2 | `3 flamme: +1` | `4 flamme: +1` | Rull én angrepsterning på nytt. | 4 | 3 stein, 2 flamme krav, 3/4 flamme bonus, omrull |
| `monster_water_1_a` | vann | 1 | `dråpe` | 1 | Ingen | Ingen | Ingen | 1 | dråpe, base 1 |
| `monster_water_1_b` | vann | 1 | `stein dråpe` | 1 | `2 dråpe: +1` | Ingen | Ingen | 2 | stein, dråpe, 2 dråpe bonus |
| `monster_water_1_c` | vann | 1 | `dråpe dråpe` | 1 | `3 dråpe: +1` | Ingen | Ingen | 2 | 2 dråpe krav, 3 dråpe bonus |
| `monster_water_2_a` | vann | 2 | `stein dråpe dråpe` | 1 | `2 dråpe: +1` | `3 dråpe: +1` | Ingen | 3 | stein, 2 dråpe krav, 2/3 dråpe bonus |
| `monster_water_2_b` | vann | 2 | `stein stein dråpe` | 2 | `3 dråpe: +1` | Ingen | Ingen | 3 | 2 stein, dråpe, 3 dråpe bonus |
| `monster_water_3_a` | vann | 3 | `stein stein stein dråpe dråpe` | 2 | `3 dråpe: +1` | `4 dråpe: +1` | Reduser mottatt skade mot kongens liv med 1. | 4 | 3 stein, 2 dråpe krav, 3/4 dråpe bonus, skjold |
