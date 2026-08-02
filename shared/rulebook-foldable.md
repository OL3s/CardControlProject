# Elements: Conquora - Kompakt Regelark

Dette er et kort regelark for bruk under spilling. Fullt regelutkast ligger i [`docs/gameidea-working.md`](docs/gameidea-working.md).

Formatmål: fire sider/paneler på ett foldbart ark.

---

# Panel 1 - Mål, Oppsett og Tur

## Mål

Vinn ved å:

* Kontrollere minst 6 terreng ved starten av din tur.
* Ødelegge motstandernes konger.

Siste levende konge vinner.

Kontrollmålet er en felles regel og har ikke et eget kort.

## Elementer

* Vann slår flamme.
* Flamme slår gress.
* Gress slår vann.
* Nøytral har ingen elementfordel eller svakhet.

Elementfordel i kamp gir `+1` kampverdi.

## Oppsett

1. Bland terreng- og monsterbunken hver for seg.
2. Legg ett tilfeldig terreng i midten. Det starter eierløst.
3. Rull for startspiller. Høyest får startspillerbrikken.
4. Hver spiller trekker 3 terreng, velger 1 og legger resten nederst i terrengbunken.
5. Hver spiller trekker 3 monstre, velger 1 og legger resten nederst i monsterbunken.
6. Legg startterrenget inntil sentrumsterrenget.
7. Plasser kongen og 2 bønder på startterrenget.
8. Sett kongens livmarkør til 6.

## Runden

1. Plassering: hver spiller tar én tur fra startspilleren og videre i turrekkefølge.
2. Kamp: konflikter løses fra startspilleren og videre i turrekkefølge.
3. Avslutning: oppdater kontroll, ressurser og flytt startspillerbrikken til neste spiller.

## Starten Av Din Tur

1. Sjekk det felles kontrollmålet. Du vinner hvis du kontrollerer minst 6 terreng.
2. Få 1 ledig bondebrikke.
3. Trekk 1 terrengkort eller 1 monsterkort hvis du har færre enn 3 kort på hånden.

Håndgrense: maks 3 terreng-/monsterkort totalt.

---

# Panel 2 - Kart, Bønder og Ressurser

## Legge Terreng

På din tur kan du legge maks 1 terrengkort.

Nytt terreng må:

* Dele side med et eksisterende terreng.
* Ligge inntil et terreng du kontrollerer.

Terreng er sekskanter med opptil seks naboer. Nytt terreng starter eierløst.

Hvis du ikke kontrollerer noe terreng, kan du legge nytt terreng inntil kongens terreng.

## Flytting Og Ekspansjon

På din plasseringstur kan du flytte bønder til opptil 3 målterreng.

For hver flytting må det finnes en sammenhengende sti gjennom terreng du kontrollerer og som ikke er blokkert.

Målterreng kan være:

* Kontrollert av deg.
* Eierløst.
* Omstridt.
* Fiendtlig.

Et terreng er blokkert hvis det inneholder brikker fra mer enn én spiller. Du kan flytte inn på blokkert terreng, men ikke ut fra det før konflikten er løst.

Kongen kan flyttes til ett av de samme målterrengene og følger samme sti-regler. Kongen kan ikke flytte inn på et terreng med en annen konge.

## Kontroll

Du kontrollerer et terreng hvis din kontrollstyrke er høyere enn alle motstanderes samlede kontrollstyrke.

Kontrollverdi:

> Egne bønder + egen kongekontroll - fiendtlige bønder - fiendtlig kongekontroll

Kongen teller som 1 kontrollstyrke.

Ved kontrollverdi 0 er terrenget omstridt. Omstridt terreng gir ingen ressurser og kan ikke brukes som ekspansjonsvei.

## Ressurser

Bare bønder på terreng du kontrollerer gir ressurser.

Hver bonde kan aktivere 1 ressurs fra terrenget den står på.

Terrengets trykte ressursverdier er kapasitet. Kontrollverdien begrenser hvor mange ressurser du kan hente fra terrenget.

Nøytral er en egen ressurs. Den erstatter ikke gress, flamme eller vann.

---

# Panel 3 - Kamp

## Konfliktfase

Fra startspilleren og videre i turrekkefølge får hver spiller én konfliktfase.

I din konfliktfase løser du én samlet kamp mot hver motstander du deler konfliktterreng med.

Et konfliktterreng er et terreng der begge spillerne har minst én bonde eller konge.

Alle konfliktterreng mellom de to spillerne inngår i én samlet kamp. Det er ikke én kamp per terreng.

## Velge Kampbønder

Velg kampbønder per konfliktterreng:

* Like mange bønder: begge velger alle.
* Færrest bønder: spilleren med færrest velger alle.
* Flest bønder: spilleren med flest må velge minst like mange som motstanderen.
* Overskytende bønder er valgfrie.

Alle valgte bønder fra alle konfliktterreng blir én kamppool for hver spiller.

Valgte kampbønder slutter midlertidig å kreve ressurser. Uvalgte bønder blir stående og kan fortsatt bidra til kontroll og ressurser.

Monsterkrav sjekkes etter at kampbønder er valgt.

## Monstre

Hver side kan bruke maks 1 monsterkort.

Monsteret må være på hånden og ressurskravet må være oppfylt.

Monsterkort velges i startspillerrekkefølge:

1. Spilleren med startspillerbrikken, eller nærmest den i turrekkefølge, velger og viser først.
2. Den andre spilleren velger etterpå.

Monsterkort beholdes etter kampen.

## Terninger Og Kampverdi

Hver valgt kampbonde brukes som 1 kampterning.

Kampterning: `0, 0, 1, 1, 2, 2`.

Begge spillere ruller og summerer.

Kampverdi:

> Terningresultat + monsterstyrke + eventuell elementfordel + eventuell grunnverdi

Uten monster: grunnverdi `0`.

Uten monster på kongens konfliktterreng: grunnverdi `1`.

Høyest kampverdi vinner. Differansen er tapet taperen må ta. Ved likt blir det uavgjort og ingen tar tap fra kampresultatet.

Maks tap kan ikke overstige vinnerens antall valgte kampbønder eller taperens antall valgte kampbønder.

## Kort Som Tap

Når du mottar bondetap eller kongeskade fra kamp, kan du kaste kort fra hånden.

Hvert kastede kort reduserer tap eller skade med 1.

Kort kan redusere bondetap, kongeskade eller en kombinasjon.

---

# Panel 4 - Etter Kamp, Konger og Huskeregler

## Omplassering Etter Kamp

Etter hver samlet kamp fjernes tap og gjenværende kampbønder omplasseres.

Rekkefølge:

* Vinneren omplasserer først.
* Taperen omplasserer etterpå.
* Ved uavgjort brukes startspillerrekkefølge.

Hver spiller kan omplassere gjenværende kampbønder til opptil 3 terreng.

Omplassering må være til ikke-fiendtlige terreng. Et opprinnelig konfliktterreng er lovlig hvis det ikke lenger er fiendtlig.

Omplassering kan ikke skape ny konflikt mellom de to spillerne som nettopp løste kampen.

Målet er at den samlede kampen rydder konflikten mellom de to spillerne.

Etter omplassering beregnes kontroll og ressurser på nytt. Terreng uten bønder blir eierløst.

## Kongen

Kongen:

* Starter med 6 liv.
* Kan ikke helbredes.
* Teller som 1 kontrollstyrke.
* Blokkerer ekspansjon gjennom sitt terreng så lenge den lever.
* Ødelegges ved 0 liv. Spilleren taper.

Fiendtlige bønder kan plasseres på kongens terreng.

Når kongens terreng tar skade, velger eieren hvordan skaden fordeles mellom gjenværende kampbønder og kongeliv. Kort kan kastes for å redusere skaden.

## Kortflyt

På hånden kan du ha maks 3 kort totalt.

Å kaste et kort betyr å legge kortet nederst i riktig trekkbunke. Det finnes ingen egen kastebunke.

På din tur kan du bytte:

* 2 terrengkort mot 1 nytt terrengkort.
* 2 monsterkort mot 1 nytt monsterkort.

De 2 gamle kortene legges nederst i riktig bunke. Trekk 1 nytt kort fra samme bunke.

Du kan både trekke og bytte samme tur hvis du har kort nok og følger håndgrensen.

## Sluttfase

I 2-spiller slutter spillet straks én konge ødelegges.

I 3-4 spiller starter 10-minutters timeglass etter den første kampfasen som ødelegger minst én konge, hvis ingen allerede har vunnet.

Når tiden går ut, fullføres aktiv runde. Hvis ingen har vunnet, vinner spilleren med mest gjenværende kongeliv.

Hvis spillerne har like mye kongeliv, blir det uavgjort.

## Rask Husk

* Håndgrense: 3 kort.
* Kongeliv: 6.
* Startbønder: 2.
* Få 1 bonde og mulighet til 1 kort hver tur.
* Flytt til opptil 3 målterreng i plassering.
* Omplasser kampbønder til opptil 3 terreng etter kamp.
* Kast 1 kort = reduser bondetap eller kongeskade med 1.
