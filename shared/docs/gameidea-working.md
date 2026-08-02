[Back](../../README.md)

# Elements: Conquora - Prototype v0.12

## Navigasjon

* [Terrengkort](terrain-cards.md)
* [Monsterkort](monster-cards.md)

---

Et kompakt kort-, strategi- og områdekontrollspill for **2-4 spillere**.

Spillerne bygger et felles kart, plasserer bønder for å kontrollere territorier og elementressurser, og bruker monsterkort til å avgjøre kampene.

Spillet kombinerer:

* Områdekontroll
* Ressursspesialisering
* Elementbasert kamp
* Tilfeldige kampterninger
* Direkte angrep mot motstandernes konger

## Gameplay-loop

> Sjekk det felles kontrollmålet -> få én bonde -> trekk/bytt eventuelt kort -> legg eventuelt ett terreng -> flytt bønder og konge -> løs konflikter -> kast eventuelt kort for å redusere tap -> omplasser kampbønder -> beregn kontroll og ressurser -> flytt startspillerbrikken.

---

# 1. Spillinnhold

Spillet har totalt **52 kort**.

## Terrengkort

Spillet har **20 terrengkort**.

Fordeling:

* 8 nøytrale
* 4 gress
* 4 flamme
* 4 vann

Foreløpig tierfordeling:

* 8 nøytrale terreng: 5 Tier 1, 3 Tier 2
* 4 gressterreng: 3 Tier 1, 1 Tier 2
* 4 flammeterreng: 3 Tier 1, 1 Tier 2
* 4 vannterreng: 3 Tier 1, 1 Tier 2

## Monsterkort

Spillet har **32 monsterkort**.

Fordeling:

* 8 nøytrale
* 8 gress
* 8 flamme
* 8 vann

Foreløpig tierfordeling per element:

* 4 Tier 1-monstre
* 3 Tier 2-monstre
* 1 Tier 3-monster

## Andre komponenter

Hver spiller har:

* 8 bondebrikker/kampterninger i egen farge
* 1 kongebrikke
* 1 markør for kongens liv

Felles:

* 1 startspillerbrikke
* 1 timeglass på 10 minutter

---

# 2. Grunnbegreper

## Elementene

Elementene følger et stein-saks-papir-system:

* **Vann slår flamme**
* **Flamme slår gress**
* **Gress slår vann**
* **Nøytral** har ingen elementfordel eller svakhet

Elementene har ikke faste roller. Flamme er derfor ikke automatisk offensivt, og vann er ikke automatisk defensivt. Elementene handler om hvilke andre elementer de er sterke eller svake mot.

## Elementfordel

Elementfordel gir:

> `+1 styrke`

I kamp betyr dette `+1` på spillerens kampverdi.

Nøytrale monstre får ingen elementfordel eller elementsvakhet. De bør derfor ha enklere ressurskrav enn elementmonstre på samme tier.

## Nøytral ressurs

Nøytral er en egen ressurstype og erstatter ikke gress, flamme eller vann.

Sterke monsterkort krever vanligvis både nøytral kontroll og elementspesialisering.

Eksempel:

> Krav: 3 nøytrale ressurser og 2 flammeressurser.

Dette hindrer en spiller i å bruke sterke elementmonstre ved bare å kontrollere noen få spesialiserte ressurser.

Et terreng uten eier kalles **eierløst**.

---

# 3. Kort og tiers

## Terrengkort

Terrengverdier vises i faste hjørner for nøytral, gress, flamme og vann.

Eksempel:

* `2 nøytral`
* `1 flamme`

Et terrengkort trenger ikke ha verdier i alle fire hjørnene.

## Terrengtiers

Tier 1:

* Vanlige terreng
* Lave eller fleksible ressursverdier
* Enkle ressurskombinasjoner
* Foreløpig totalt 1-2 trykte ressurser

Tier 2:

* Bedre ressurskombinasjoner
* Mer attraktive kontrollpunkter
* Kan støtte sterkere monstre
* Foreløpig totalt 3-4 trykte ressurser

Terrengkort har ikke spesialeffekter i første prototype.

## Monsterkort

Monsterkort er spillets viktigste kampverktøy.

Hvert monsterkort har:

* Ett element
* Ressurskrav som ikoner
* Baseverdi med styrkeikon
* Eventuelle kumulative ressursbonuser
* Et tier
* Eventuelt en enkel spesialeffekt

For å bruke et monster må spilleren oppfylle kravikonene øverst på kortet.

Monsterets styrke er grunnstyrken pluss alle bonuslinjer spilleren oppfyller.

Bonuslinjer er kumulative.

Bonuslinjer leses som:

> Ressursikoner -> styrkeikon

Eksempel:

> Styrkeikon 1  
> 2 flammeikoner -> 1 styrkeikon  
> 3 flammeikoner -> 1 styrkeikon

En spiller med 3 flammeressurser får styrke 3 før eventuell elementfordel.

Styrken legges til spillerens kampverdi sammen med terningresultatet fra valgte kampbønder.

## Monstertiers

Tier 1:

* Lav styrke
* Enkle krav
* Fleksible og tilgjengelige monstre
* Foreløpig krav på 1-2 ressurser
* Foreløpig maks styrke 1-2 før elementfordel

Tier 2:

* Middels styrke
* Krever mer generell kontroll
* Krever gjerne nøytral og én elementtype
* Foreløpig krav på 3-4 ressurser, ofte minst 1 nøytral
* Foreløpig maks styrke 2-3 før elementfordel

Tier 3:

* Høy styrke
* Krever bred kontroll og elementspesialisering
* Kan ha en enkel spesialeffekt
* Foreløpig krav på 5 ressurser, vanligvis 3 nøytral og 2 av monsterets element
* Foreløpig maks styrke 3-4 før elementfordel

Eksempel:

> Tier 3 flammemonster  
> Krav: 3 nøytral og 2 flamme  
> Styrkeikon 2  
> 3 flammeikoner -> 1 styrkeikon  
> 3 nøytralikoner -> 1 styrkeikon

Tier 3-spesialeffekter skal være enkle og små. De skal ikke gi ekstra full kamp, ekstra monsterbruk eller direkte skade på en konge uten kampresultat.

Eksempler på tillatte Tier 3-effekter:

* Rull én kampterning på nytt
* Reduser mottatt bondetap med 1
* Tell én bestemt ressurs som én annen ressurs bare for dette monsterets krav

---

# 4. Oppsett

## Felles oppsett

1. Bland terreng- og monsterbunken hver for seg.
2. Trekk ett tilfeldig terrengkort og legg det midt på bordet. Det starter eierløst.
3. Alle spillerne ruller én kampterning. Høyeste resultat får startspillerbrikken. Ved likt resultat rulles det igjen.

## Spilleroppsett

Hver spiller gjør deretter følgende:

1. Trekk 3 terrengkort, velg 1 og legg resten nederst i terrengbunken.
2. Trekk 3 monsterkort, velg 1 og legg resten nederst i monsterbunken.
3. Legg det valgte terrengkortet inntil sentrumsterrenget.
4. Plasser kongebrikken og 2 bønder på startterrenget.
5. Sett kongens livmarkør til 6.

Uvalgte kort vises ikke til de andre spillerne.

## Startspillerbrikken

Startspilleren starter runden. Turene går i fast turrekkefølge, og startspillerbrikken flyttes til neste spiller etter hver runde.

---

# 5. Rundestruktur og kortflyt

## Rundestruktur

Hver runde består av tre faser.

Fase 1: Plassering

* Spillerne tar hver sin tur fra startspilleren og videre i turrekkefølge.

Fase 2: Kamp

* Konflikter løses.

Fase 3: Avslutning

* Tap, kontroll og ressurser oppdateres.
* Startspillerbrikken flyttes til neste spiller.

## Starten av spillerens tur

Ved starten av turen:

1. Sjekk det felles kontrollmålet.
2. Spilleren vinner dersom spilleren kontrollerer minst 6 terreng.
3. Spilleren får 1 ledig bondebrikke.
4. Spilleren kan trekke 1 terrengkort eller monsterkort dersom spilleren har plass på hånden.

Kort er også en begrenset beskyttelse i kamp. Siden en spiller vanligvis får 1 bonde og mulighet til 1 nytt kort hver tur, men bare kan ha 3 kort på hånden, kan kort bremse tap uten at en spiller kan samle opp ubegrenset forsvar.

## Hånden

En spiller kan ha maksimalt **3 kort på hånden totalt**.

Spilleren bestemmer selv blandingen av:

* Terrengkort
* Monsterkort

Monsterkort er tilgjengelige så lenge spilleren har dem på hånden.

Monsterkort kastes ikke etter kamp og kan brukes på nytt så lenge kravene er oppfylt.

Å kaste et kort betyr å legge kortet nederst i riktig trekkbunke. Det finnes ingen egen kastebunke.

Kort på hånden kan også kastes for å redusere bondetap eller kongeskade fra kamp.

## Trekking

På sin tur kan spilleren trekke:

* 1 terrengkort
* eller
* 1 monsterkort

Spilleren kan bare trekke dersom spilleren har færre enn 3 kort på hånden.

## Bytte to kort mot ett

For å redusere uflaks kan spilleren bytte:

* 2 terrengkort mot 1 nytt terrengkort
* eller
* 2 monsterkort mot 1 nytt monsterkort

De 2 gamle kortene legges nederst i den aktuelle bunken. Deretter trekkes 1 nytt kort fra samme bunke.

Terrengkort og monsterkort kan ikke blandes i samme bytte.

En spiller kan både trekke og bruke regelen `to kort mot ett` på samme tur. Byttet kan brukes flere ganger samme tur så lenge spilleren har nok kort.

Dersom en bunke er tom, kan spilleren ikke trekke eller bytte fra den bunken.

---

# 6. Kart, terreng og ekspansjon

## Legge terrengkort

På sin egen tur kan spilleren legge maksimalt **1 terrengkort**.

Det nye terrenget må:

* Plasseres inntil et terreng spilleren kontrollerer
* Dele minst én hel side med et eksisterende terreng

Terrengkort plasseres som sekskanter, med opptil seks naboterreng.

Det nye terrenget starter eierløst.

Spilleren kan plassere bønder der dersom terrenget er et av spillerens målterreng for plasseringen.

Kartet har ingen fast form eller størrelse.

Dersom en levende spiller ikke kontrollerer noe terreng, kan spilleren plassere nytt terreng inntil terrenget der kongen står.

## Ekspansjon

Spilleren ekspanderer ved å flytte bønder til opptil 3 målterreng i sin plasseringstur.

For hver flytting må det finnes en sammenhengende sti gjennom terreng spilleren kontrollerer og som ikke er blokkert.

Målterrenget kan være kontrollert, eierløst, omstridt eller fiendtlig.

Et terreng er blokkert dersom det inneholder brikker fra mer enn én spiller. Bønder kan flyttes inn på et blokkert terreng, men ingen brikker kan flyttes ut fra det før konflikten er løst.

Dersom et nytt terreng blir kontrollert i løpet av plasseringen og ikke inneholder fiendtlige brikker, kan det brukes som del av stien til senere målterreng samme tur.

Alle bønder blir stående frem til kampfasen.

Kongens område kan bli avskåret fra resten av territoriet.

Avskårne områder fungerer som separate kontrollnettverk.

---

# 7. Bønder, kontroll og ressurser

## Bønder

Bønder brukes til:

* Terrengkontroll
* Ressurser
* Monsterkrav
* Ekspansjon
* Maksimal skade i kamp

Bønder som velges til kamp er kampterninger. Bønder som ikke velges, blir stående og kan fortsatt bidra til kontroll og ressurser.

Hver spiller starter med 2 bønder.

## Kontroll over terreng

En spiller kontrollerer et terreng dersom spilleren har flere bønder der enn alle motstanderne til sammen.

Kongen teller som 1 kontrollstyrke på terrenget den står på.

Kontrollverdi:

> Egne bønder + egen kongekontroll - alle fiendtlige bønder - eventuell fiendtlig kongekontroll

Eksempel: Rød har 3 bønder og Blå har 1. Røds kontrollverdi er 2, så Rød kontrollerer terrenget.

## Omstridt terreng

Dersom kontrollverdien er 0, er terrenget omstridt. Ingen kontrollerer det, det gir ingen ressurser og det kan ikke brukes som ekspansjonsvei.

## Ressurser

Bare bønder på terreng spilleren kontrollerer gir ressurser.

Ressurser brukes til:

* Monsterkrav
* Elementrelaterte effekter

En bonde på et terreng spilleren ikke kontrollerer:

* Gir ingen ressurser
* Deltar fortsatt i konflikten på terrenget

Et terrengs trykte ressursverdier er kapasitet for hvert ressursområde.

Hver bonde kan aktivere 1 ressurs fra terrenget den står på.

Kontrollverdien begrenser hvor mange ressurser spilleren totalt kan hente fra terrenget.

Spilleren velger selv hvilke trykte ressursområder bøndene bruker, innenfor terrengets trykte kapasitet.

Flere bønder kan bruke samme ressursområde dersom den trykte verdien tillater det.

Eksempel:

* Terrenget har `2 nøytral` og `1 flamme`
* Spilleren har 3 bønder der
* En motstander har 1 bonde der
* Spilleren har kontrollverdi 2
* Spilleren kan hente maksimalt 2 ressurser fra terrenget
* Spilleren kan velge `2 nøytral` eller `1 nøytral` og `1 flamme`

---

# 8. Kongen

## Kongens rolle

Kongen representeres av:

* Én fysisk kongebrikke
* Én markør for kongens liv

Kongen:

* Starter på spillerens første terreng
* Har 6 liv
* Kan ikke helbredes
* Teller som 1 kontrollstyrke
* Kan flyttes til ett av spillerens målterreng under plasseringen
* Kan flyttes etter samme regler som bønder gjennom kontrollert, ublokkert territorium
* Kan ikke gå inn på et terreng som inneholder en annen konge

To konger kan stå på naboterreng.

## Kongens blokkering

Fiendtlige bønder kan plasseres på kongens terreng. Kongen blokkerer ekspansjonsveien gjennom terrenget så lenge den lever.

## Kongens liv

Kongens liv spores med livmarkøren ved kongebrikken.

Når kongen når 0 liv, ødelegges kongen og spilleren taper.

---

# 9. Kamp

## Kampfase

Når alle spillere har fullført plasseringen sin, starter kampfasen.

Fra startspilleren og videre i turrekkefølge får hver spiller én konfliktfase.

I sin konfliktfase løser spilleren én samlet kamp mot hver motstander spilleren deler konfliktterreng med.

Et konfliktterreng er et terreng der begge spillerne har minst én bonde eller konge.

Alle konfliktterreng mellom de to spillerne inngår i én samlet kamp. Det er ikke én kamp per terreng.

Monsterkrav bruker spillerens samlede ressurser fra hele kartet etter at kampbønder er valgt og ikke lenger krever ressurser.

Eksempel:

* Rød og Blå deler konflikt på 3 terreng
* Dette løses som én samlet Rød-mot-Blå-kamp
* Alle 3 konfliktterreng brukes til å velge kampbønder
* Selve terningkastet og kampresultatet skjer én gang

## Velge kampbønder

Før terninger rulles velger begge spillerne hvilke bønder fra hvert konfliktterreng som skal delta i kampen.

Valget gjøres per konfliktterreng:

* Dersom spillerne har like mange bønder på terrenget, må begge velge alle sine bønder der.
* Spilleren med færrest bønder på terrenget må velge alle sine bønder der.
* Spilleren med flest bønder på terrenget må velge minst like mange bønder som motstanderen har valgt fra samme terreng.
* Overskytende bønder på terrenget er valgfrie.

Eksempel 1:

* Rød har 1 bonde på et konfliktterreng.
* Blå har 3 bønder på samme terreng.
* Rød må velge 1 bonde.
* Blå må velge minst 1 bonde, men kan velge 1, 2 eller 3.

Eksempel 2:

* Rød har 2 bønder på et annet konfliktterreng.
* Blå har 1 bonde på samme terreng.
* Blå må velge 1 bonde.
* Rød må velge minst 1 bonde, men kan velge 1 eller 2.

Alle valgte bønder fra alle konfliktterreng legges sammen til én kamppool for hver spiller. Deretter løses én samlet kamp.

Valgte kampbønder slutter midlertidig å kreve ressurser fra terrenget de kom fra.

Bønder som ikke velges, blir stående på terrenget og kan fortsatt bidra til kontroll og ressurser.

## Monsterbruk i kamp

I hver samlet kamp kan hver side bruke maksimalt ett monsterkort.

Monsteret må:

* Være på hånden
* Oppfylle ressurskravene

Monsterkort velges i startspillerrekkefølge.

Spilleren med startspillerbrikken, eller spilleren nærmest startspillerbrikken i turrekkefølge, velger først om de bruker monsterkort og viser valget. Deretter velger den andre spilleren om de bruker monsterkort.

Dette gir spilleren som velger senere mulighet til å svare med et element som passer mot det første monsteret.

Ingen spiller kan endre monster etter at neste spiller har valgt.

Monsterkortet beholdes etter kampen og kan brukes igjen.

## Kamp uten monster

Dersom en side ikke bruker et monsterkort, brukes grunnverdi.

Vanlig kamp uten monster:

* Statisk bonus: 0

Spilleren får fortsatt terninger fra valgte kampbønder.

Kongens terreng uten monster:

* Statisk bonus: 1 dersom kongen er på et av de involverte konfliktterrengene

Kongens grunnstyrke brukes bare uten monsterkort.

## Kampterningene

Valgte kampbønder er kampterninger.

Hver valgt kampbonde brukes som én kampterning.

Hver kampterning har sidene:

> `0, 0, 1, 1, 2, 2`

Begge spillere ruller én terning per valgt kampbonde.

Terningresultatene summeres.

## Elementfordel i kamp

Dersom begge sider bruker monsterkort, sammenlignes elementene.

* Vann slår flamme
* Flamme slår gress
* Gress slår vann
* Nøytral har ingen fordel

Monsteret med elementfordel får `+1 styrke`.

Dersom bare én side bruker monster, finnes det ingen elementfordel.

## Kampresultat

Hver spillers kampverdi er:

> Terningresultat + monsterstyrke + eventuell elementfordel + eventuell grunnverdi

Spilleren med høyest kampverdi vinner kampen. Differansen er tapet taperen må ta.

Ved lik verdi er kampen uavgjort. Ingen vinner, og ingen tar tap fra kampresultatet.

Begge sider må fortsatt omplassere valgte kampbønder etter kampen, også ved uavgjort.

## Maksimal skade

En spiller kan aldri miste flere bønder enn spilleren har valgt som kampbønder.

Vinneren kan ikke påføre mer tap enn antall egne valgte kampbønder.

Når en spiller mottar tap, velger den spilleren selv hvilke egne valgte kampbønder som fjernes.

Eksempel:

* Rød har valgt 2 kampbønder.
* Blå har valgt 5 kampbønder.
* Rød vinner med forskjell 4.
* Rød påfører maksimalt 2 tap.
* Blå velger selv hvilke 2 egne kampbønder som fjernes.

Flere bønder gir flere terninger og øker maksimal skade, men setter også flere bønder i fare.

## Kort som tap

Når en spiller mottar bondetap eller kongeskade fra kamp, kan spilleren kaste kort fra hånden for å redusere tapet.

Hvert kastede kort reduserer totalt tap eller skade med 1.

Kort kan redusere:

* Bondetap
* Kongeskade
* En kombinasjon av bondetap og kongeskade

Eksempel:

* Blå mottar 3 tap.
* Blå kaster 1 monsterkort.
* Blå kaster 1 terrengkort.
* Tapet reduseres fra 3 til 1.

## Kamp med flere spillere

Dersom en spiller deler konfliktterreng med flere motstandere, løser spilleren én samlet kamp mot hver motstander.

Samme terreng kan inngå i flere samlede kamper dersom tre eller flere spillere har bønder der.

Tap fjernes og kampbønder omplasseres etter hver samlet kamp før neste samlet kamp løses.

Bare bønder som fortsatt står på relevante konfliktterreng kan velges i senere samlede kamper.

## Kongens terreng i kamp

Kongens terreng kan bare ta skade dersom det er et av de involverte konfliktterrengene i den samlede kampen.

Når kongens terreng mottar skade, velger kongens eier hvordan skaden fordeles mellom:

* Gjenværende valgte kampbønder
* Kongens liv

Dersom kongens terreng er involvert og ingen egne bønder står på kongens terreng, må skade som legges på kongens terreng tas fra kongens liv.

Eksempel:

Kongens terreng er involvert i en samlet kamp, og kongens eier mottar 3 skade.

Spilleren kan:

* Fjerne 3 gjenværende valgte kampbønder
* Fjerne 2 bønder og miste 1 kongeliv
* Fjerne 1 bonde og miste 2 kongeliv
* Miste 3 kongeliv dersom kongens terreng kan ta hele skaden

Spilleren kan kaste kort for å redusere denne skaden før bønder fjernes eller kongeliv mistes.

## Etter kamp

Etter hver samlet kamp fjernes tap og gjenværende valgte kampbønder omplasseres.

Omplassering:

* Vinneren omplasserer først.
* Taperen omplasserer etterpå.
* Ved uavgjort omplasserer spillerne i startspillerrekkefølge. Spilleren med startspillerbrikken, eller spilleren nærmest startspillerbrikken i turrekkefølge, omplasserer først.
* Hver spiller kan omplassere sine gjenværende kampbønder til opptil 3 terreng.
* Terrengene må være ikke-fiendtlige for spilleren som omplasserer.
* Et opprinnelig konfliktterreng er et lovlig mål dersom det ikke lenger er fiendtlig.
* Omplassering kan ikke skape en ny konflikt mellom de to spillerne som nettopp løste kampen.

Målet er at den samlede kampen rydder konflikten mellom de to spillerne. Når omplasseringen er ferdig, skal de to spillerne ikke lenger dele konfliktterreng fra denne kampen.

Etter omplassering:

* Kontroll og ressurser beregnes på nytt for de involverte terrengene

Et terreng uten bønder blir eierløst.

---

# 10. Seier og sluttspill

## Felles kontrollmål

Alle spillerne bruker det samme offentlige kontrollmålet:

* Kontroller 6 terreng.

Spilleren vinner dersom målet er oppfylt ved starten av spillerens tur. Målet er en felles regel og har ikke et eget kort.

## Vanlige vinnemåter

En spiller vinner ved å:

* Ødelegge motstandernes konger
* Oppfylle det felles kontrollmålet

Siste levende konge vinner.

## Sluttfase etter første utslåtte spiller

I spill med 2 spillere brukes ikke timeglasset. Spillet slutter straks én konge ødelegges.

I spill med 3-4 spillere starter timeglasset når den første kampfasen som ødelegger minst én konge er ferdig, dersom spillet ikke allerede har en vinner.

Når timeglasset starter:

1. Start et **10-minutters timeglass**.
2. Spillet fortsetter med vanlige regler.
3. Vanlige seierskrav gjelder fortsatt under nedtellingen.
4. Når tiden går ut, fullføres den aktive runden.
5. Dersom ingen har vunnet, vinner spilleren med mest gjenværende kongeliv.

Dersom spillerne har like mye kongeliv, ender spillet uavgjort.


## Samtidig ødeleggelse av konger

Dersom flere konger ødelegges samtidig og bare én konge står igjen, vinner spilleren med den siste levende kongen.

Dersom alle gjenværende konger ødelegges i samme kampfase, vinner spilleren som hadde mest kongeliv ved starten av kampfasen.

Dersom spillerne hadde like mye kongeliv ved starten av kampfasen, ender spillet uavgjort.

---

# 11. Playtestfokus og åpne punkter

Følgende punkter er ikke nødvendigvis uavklarte regler, men bør testes fordi de påvirker balanse, tempo eller lesbarhet.

## Bevegelse og ekspansjon

* Er opptil 3 målterreng per tur lett nok å administrere?
* Gir 3 målterreng riktig tempo for ekspansjon i tidlig spill og omgruppering i sluttspill?
* Er kravet om sammenhengende sti tydelig nok på bordet?
* Gjør blokkert terreng vegger taktisk interessante, eller blir kartet for låst?
* Er 8 bønder nok når spillerne trenger bønder til både ressurser, fronter, vegger og kongebeskyttelse?

## Kontroll og ressurser

* Er kontrollverdi som ressursbegrensning intuitivt nok?
* Gir kongens 1 kontrollstyrke riktig vekt uten å gjøre kongen for trygg?
* Blir ressursregningen for tung når flere terreng har flere ressursområder?

## Flerspillerkamp

* Er én samlet kamp per spillerpar rask nok i praksis?
* Blir det tydelig hvilke terreng som inngår i en samlet kamp mellom to spillere?
* Er valg av kampbønder per konfliktterreng lett å gjennomføre fysisk?
* Fungerer det at overskytende bønder kan bli stående og fortsatt kreve ressurser?
* Rydder omplassering etter kamp konflikt mellom spillerne uten å skape nye uklare situasjoner?
* Fungerer det at samme terreng kan inngå i flere samlede kamper når tre eller flere spillere står der?
* Skaper startspillerrekkefølge riktig og forståelig omplasseringsrekkefølge ved uavgjort?

## Monsterkamp

* Gir monsterchoice i startspillerrekkefølge nok motspill mot startspillerfordel?
* Blir elementcounter etter første monstervalg taktisk interessant uten å bli for sterkt?
* Fungerer monsterstyrke bedre som statisk bonus enn som antall terninger?
* Er nøytrale monstre balansert når de mangler elementfordel men har enklere krav?
* Hvor sterke kan Tier 3-spesialeffekter være før de tar over kampene?

## Kongen

* Er kongens blokkering for sterk i trange kart?
* Fungerer det at kongen bruker ett av spillerens målterreng når den flyttes?
* Skaper nabostående konger interessante fronter eller rare låser?

## Kortflyt

* Er håndgrensen på 3 kort riktig?
* Er fri bruk av `to kort mot ett` innenfor håndgrensen nok til å redusere uflaks uten å gi for mye kortfiltrering?
* Bremser kort som tapserstatning snowballing uten å gjøre kamp for ufarlig?
* Gir valget mellom å beholde monsterkort og bruke kort som beskyttelse interessant spenning?
* Bør tomme bunker ha en egen omstokking, eller holder det at kort legges nederst?

## Terrengplassering

* Gir fri plassering på ledig side av kontrollert terreng nok strategisk variasjon?
* Er blokkering med terreng morsomt eller frustrerende?
* Skaper avskårne kontrollnettverk interessante valg eller unødvendig kompleksitet?

## Tiers og kortdesign

* Endelig fordeling av terrengtiers
* Endelig fordeling av monstertiers
* Nøyaktige monsterstyrker
* Nøyaktige ressurskrav
* Nøyaktige terrengverdier
* Antall Tier 3-monstre med spesialeffekter
* Balansering av nøytrale monstre mot elementmonstre
