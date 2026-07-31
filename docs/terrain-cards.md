# Terrengkort

Terrengkort bygger kartet og gir ressurskapasitet. Korttypen leses av terrengrammen og baksiden. Elementfokus styrer fargestemning og hvilke ressursikoner som dominerer kortflaten. Intern tier brukes bare i kortlisten og ID-en, ikke på selve kortet.

Frontmal: [`images/terrain_card_front.svg`](images/terrain_card_front.svg)  
Bakside: [`images/terrain_card_back.svg`](images/terrain_card_back.svg)

## Ikoner

* Nøytral: stein/sirkel
* Gress: blad
* Flamme: flamme
* Vann: dråpe
* Effekt: lite stjerne- eller terrengikon

## Kortliste

| kort_id | elementfokus | intern_tier | nøytral | gress | flamme | vann | effekt | synlige_ikoner |
|---|---|---:|---:|---:|---:|---:|---|---|
| `terrain_neutral_1_a` | nøytral | 1 | 1 | 0 | 0 | 0 | Ingen | 1 stein |
| `terrain_neutral_1_b` | nøytral | 1 | 1 | 1 | 0 | 0 | Ingen | 1 stein, 1 blad |
| `terrain_neutral_1_c` | nøytral | 1 | 1 | 0 | 1 | 0 | Ingen | 1 stein, 1 flamme |
| `terrain_neutral_1_d` | nøytral | 1 | 1 | 0 | 0 | 1 | Ingen | 1 stein, 1 dråpe |
| `terrain_neutral_2_a` | nøytral | 2 | 2 | 1 | 0 | 0 | Ingen | 2 stein, 1 blad |
| `terrain_neutral_2_b` | nøytral | 2 | 2 | 0 | 1 | 0 | Ingen | 2 stein, 1 flamme |
| `terrain_neutral_2_c` | nøytral | 2 | 2 | 0 | 0 | 1 | Ingen | 2 stein, 1 dråpe |
| `terrain_neutral_3_a` | nøytral | 3 | 3 | 1 | 1 | 1 | Når du kontrollerer dette terrenget, teller 1 bonde her som valgfritt element for monsterkrav. | 3 stein, 1 blad, 1 flamme, 1 dråpe, stjerne |
| `terrain_grass_1_a` | gress | 1 | 0 | 1 | 0 | 0 | Ingen | 1 blad |
| `terrain_grass_1_b` | gress | 1 | 1 | 1 | 0 | 0 | Ingen | 1 stein, 1 blad |
| `terrain_grass_2_a` | gress | 2 | 1 | 2 | 0 | 0 | Ingen | 1 stein, 2 blad |
| `terrain_grass_3_a` | gress | 3 | 2 | 2 | 0 | 1 | Før kamp kan du flytte 1 egen bonde fra dette terrenget til et kontrollert naboterreng. | 2 stein, 2 blad, 1 dråpe, stjerne |
| `terrain_flame_1_a` | flamme | 1 | 0 | 0 | 1 | 0 | Ingen | 1 flamme |
| `terrain_flame_1_b` | flamme | 1 | 1 | 0 | 1 | 0 | Ingen | 1 stein, 1 flamme |
| `terrain_flame_2_a` | flamme | 2 | 1 | 0 | 2 | 0 | Ingen | 1 stein, 2 flamme |
| `terrain_flame_3_a` | flamme | 3 | 2 | 1 | 2 | 0 | Før du angriper fra dette terrenget, kan du gi ett brukt monster +1 maksimal skade. | 2 stein, 1 blad, 2 flamme, stjerne |
| `terrain_water_1_a` | vann | 1 | 0 | 0 | 0 | 1 | Ingen | 1 dråpe |
| `terrain_water_1_b` | vann | 1 | 1 | 0 | 0 | 1 | Ingen | 1 stein, 1 dråpe |
| `terrain_water_2_a` | vann | 2 | 1 | 0 | 0 | 2 | Ingen | 1 stein, 2 dråpe |
| `terrain_water_3_a` | vann | 3 | 2 | 0 | 1 | 2 | Når du forsvarer på dette terrenget, kan du redusere mottatt bondetap med 1. | 2 stein, 1 flamme, 2 dråpe, stjerne |
