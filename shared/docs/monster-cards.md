[Back](../../README.md)

# Monsterkort

## Navigasjon

* [Aktivt regelutkast](gameidea-working.md)
* [Terrengkort](terrain-cards.md)

---

Denne kortlisten er source of truth for monsterkortene i defaultutgaven **Elements: Conquora**.

Monsterkort er kampverktøy. Korttypen leses av monsterrammen og baksiden. Elementet styrer kortets ikon, farge og hvilke ressursbonuser det vanligvis bruker. Intern tier brukes bare i kortlisten og ID-en, ikke på selve kortet. Standardstokken har 32 monstre: åtte per element, fordelt som 4 Tier 1, 3 Tier 2 og 1 Tier 3.

PNG-bildene under viser kortpreviewene. SVG-kildene ligger i [`images/svg/`](images/svg/), og ikonene ligger i [`images/svg/icons/`](images/svg/icons/).

![Monsterkort frontmal](images/png/monster_card_front.png) ![Monsterkort bakside](images/png/monster_card_back.png)

## Ikoner Og Styrke

Kravikonene øverst må oppfylles for å bruke monsteret. Styrkeikonet viser grunnstyrken. Bonuslinjer bruker formen `ressursikoner -> styrkeikon` og er kumulative.

Elementfordel gir fortsatt `+1 styrke` i tillegg til monsterets grunnstyrke og bonuslinjer.

## Kortliste

| kort_id | element | intern_tier | krav | styrkeikon | bonus_1 | bonus_2 | effekt | maks_styrke_før_elementfordel |
|---|---|---:|---|---:|---|---|---|---:|
| `monster_neutral_1_a` | nøytral | 1 | `stein` | 1 | Ingen | Ingen | Ingen | 1 |
| `monster_neutral_1_b` | nøytral | 1 | `2 stein` | 1 | `3 stein -> 1 styrke` | Ingen | Ingen | 2 |
| `monster_neutral_1_c` | nøytral | 1 | `stein` | 1 | `2 stein -> 1 styrke` | Ingen | Ingen | 2 |
| `monster_neutral_1_d` | nøytral | 1 | `2 stein` | 2 | Ingen | Ingen | Ingen | 2 |
| `monster_neutral_2_a` | nøytral | 2 | `2 stein` | 2 | `3 stein -> 1 styrke` | Ingen | Ingen | 3 |
| `monster_neutral_2_b` | nøytral | 2 | `2 stein` | 1 | `3 stein -> 1 styrke` | `4 stein -> 1 styrke` | Ingen | 3 |
| `monster_neutral_2_c` | nøytral | 2 | `3 stein` | 2 | `4 stein -> 1 styrke` | Ingen | Ingen | 3 |
| `monster_neutral_3_a` | nøytral | 3 | `3 stein` | 2 | `4 stein -> 1 styrke` | `5 stein -> 1 styrke` | Reduser mottatt bondetap med 1. | 4 |
| `monster_grass_1_a` | gress | 1 | `blad` | 1 | Ingen | Ingen | Ingen | 1 |
| `monster_grass_1_b` | gress | 1 | `1 stein, 1 blad` | 1 | `2 blad -> 1 styrke` | Ingen | Ingen | 2 |
| `monster_grass_1_c` | gress | 1 | `2 blad` | 1 | `3 blad -> 1 styrke` | Ingen | Ingen | 2 |
| `monster_grass_1_d` | gress | 1 | `1 stein, 1 blad` | 2 | Ingen | Ingen | Ingen | 2 |
| `monster_grass_2_a` | gress | 2 | `1 stein, 2 blad` | 1 | `2 blad -> 1 styrke` | `3 blad -> 1 styrke` | Ingen | 3 |
| `monster_grass_2_b` | gress | 2 | `2 stein, 1 blad` | 2 | `3 blad -> 1 styrke` | Ingen | Ingen | 3 |
| `monster_grass_2_c` | gress | 2 | `2 stein, 2 blad` | 2 | `3 blad -> 1 styrke` | Ingen | Ingen | 3 |
| `monster_grass_3_a` | gress | 3 | `3 stein, 2 blad` | 2 | `3 blad -> 1 styrke` | `4 blad -> 1 styrke` | Reduser mottatt bondetap med 1. | 4 |
| `monster_flame_1_a` | flamme | 1 | `flamme` | 1 | Ingen | Ingen | Ingen | 1 |
| `monster_flame_1_b` | flamme | 1 | `1 stein, 1 flamme` | 1 | `2 flamme -> 1 styrke` | Ingen | Ingen | 2 |
| `monster_flame_1_c` | flamme | 1 | `2 flamme` | 1 | `3 flamme -> 1 styrke` | Ingen | Ingen | 2 |
| `monster_flame_1_d` | flamme | 1 | `1 stein, 1 flamme` | 2 | Ingen | Ingen | Ingen | 2 |
| `monster_flame_2_a` | flamme | 2 | `1 stein, 2 flamme` | 1 | `2 flamme -> 1 styrke` | `3 flamme -> 1 styrke` | Ingen | 3 |
| `monster_flame_2_b` | flamme | 2 | `2 stein, 1 flamme` | 2 | `3 flamme -> 1 styrke` | Ingen | Ingen | 3 |
| `monster_flame_2_c` | flamme | 2 | `2 stein, 2 flamme` | 2 | `3 flamme -> 1 styrke` | Ingen | Ingen | 3 |
| `monster_flame_3_a` | flamme | 3 | `3 stein, 2 flamme` | 2 | `3 flamme -> 1 styrke` | `4 flamme -> 1 styrke` | Rull én angrepsterning på nytt. | 4 |
| `monster_water_1_a` | vann | 1 | `dråpe` | 1 | Ingen | Ingen | Ingen | 1 |
| `monster_water_1_b` | vann | 1 | `1 stein, 1 dråpe` | 1 | `2 dråpe -> 1 styrke` | Ingen | Ingen | 2 |
| `monster_water_1_c` | vann | 1 | `2 dråpe` | 1 | `3 dråpe -> 1 styrke` | Ingen | Ingen | 2 |
| `monster_water_1_d` | vann | 1 | `1 stein, 1 dråpe` | 2 | Ingen | Ingen | Ingen | 2 |
| `monster_water_2_a` | vann | 2 | `1 stein, 2 dråpe` | 1 | `2 dråpe -> 1 styrke` | `3 dråpe -> 1 styrke` | Ingen | 3 |
| `monster_water_2_b` | vann | 2 | `2 stein, 1 dråpe` | 2 | `3 dråpe -> 1 styrke` | Ingen | Ingen | 3 |
| `monster_water_2_c` | vann | 2 | `2 stein, 2 dråpe` | 2 | `3 dråpe -> 1 styrke` | Ingen | Ingen | 3 |
| `monster_water_3_a` | vann | 3 | `3 stein, 2 dråpe` | 2 | `3 dråpe -> 1 styrke` | `4 dråpe -> 1 styrke` | Reduser mottatt skade mot kongens liv med 1. | 4 |
