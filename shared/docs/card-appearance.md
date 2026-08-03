[Back](../../README.md)

# Kortutseende

## Navigasjon

* [Monsterkort](monster-cards.md)
* [Terrengkort](terrain-cards.md)
* [Aktivt regelutkast](gameidea-working.md)

---

Dette dokumentet er felles visuell source of truth for kort i **Elements: Conquora**. Kortdata og kortverdier ligger fortsatt i de separate kortlistene.

## Felles Visuelt Språk

Kortbaksidene etablerer det visuelle hovedspråket: mørke, lagdelte flater med tydelig dybde og avgrensning. Forsidene skal videreføre dette språket i rammer, paneler og bakgrunner uten å la elementfargene dominere hele kortet.

Elementikoner vises i lyse, svakt elementtonede medaljonger. Medaljongene skal ha tydelig outline, slik at ikonene beholder kontrast mot både illustrasjon og mørke flater.

Monstertier er eksplisitt kortdata og vises som én til tre små kobberdiamanter ved elementmedaljongen øverst til høyre. Terrengtier er fortsatt intern produksjons- og balansedata og vises ikke på kortflaten.

## Eksplisitt Element

Alle monster- og terrengkort har ett eksplisitt element: nøytral, gress, flamme eller vann.

Monsterets element er uavhengig av ressurskravene. Kravene avgjør om monsteret kan brukes; de skal ikke brukes til å utlede elementet. Monsterets elementmedaljong plasseres øverst til høyre.

Terrengets element er uavhengig av ressursene terrenget produserer. Det vises som en stor elementmedaljong sentrert på kortet. Elementet er autoritativ gameplay-metadata og reserveres for regler som for eksempel «kontroller et terreng med kjerneelement flamme». Det finnes foreløpig ingen aktiv terrengbonus, elementmatchup eller annen effekt knyttet til dette elementet.

## Terrengressurser

Terrengressurser har faste hjørner:

* Nøytral: øverst til venstre
* Gress: øverst til høyre
* Flamme: nederst til venstre
* Vann: nederst til høyre

En produsert ressurs vises som én medaljong per ressursenhet. Når et hjørne har flere like ikoner, skal medaljongene overlappe delvis i en matematisk jevn rekke som vokser innover mot kortets sentrum. Forskyvningen beregnes som den minste av normal ikonforskyvning og `(tilgjengelig bredde - ikonbredde) / (antall - 1)`, slik at rekken alltid holder seg i hjørnets område. Outline skal fortsatt skille hver medaljong i overlappingen.

## Format Og Printsoner

Felles printformat er:

```text
Ferdig kortstørrelse: 63 x 88 mm
Bleed: 3 mm på alle sider
Eksportert kortstørrelse: 69 x 94 mm
Safe margin: minst 4 mm innenfor kuttkant
Print-master: 600 DPI / 1630 x 2220 px
```

Bakgrunner, rammer og illustrasjoner skal gå helt ut i bleed-området. Viktig tekst, ikoner og tall skal ligge innenfor safe margin. Bleed regnes utenfor ferdig kuttkant; safe margin regnes innenfor den.
