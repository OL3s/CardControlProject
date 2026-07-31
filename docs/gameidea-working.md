# Elemental Dominion - Prototype v0.11

Et kompakt kort-, strategi- og områdekontrollspill for **2-4 spillere**.

Spillerne bygger et felles kart, plasserer bønder for å kontrollere territorier og elementressurser, og bruker monsterkort til å avgjøre kampene.

Spillet kombinerer:

* Områdekontroll
* Ressursspesialisering
* Elementbasert kamp
* Tilfeldige kampterninger
* Offentlige kongeoppdrag
* Direkte angrep mot motstandernes konger

## Gameplay-loop

> Sjekk kongeoppdrag -> få én bonde -> trekk/bytt eventuelt kort -> legg eventuelt ett terreng -> flytt bønder og konge -> løs konflikter -> fjern tap og fordel eventuell kongeskade -> beregn kontroll og ressurser -> flytt startspillerbrikken.

---

# 1. Spillinnhold

Spillet har totalt **52 kort**.

## Kongekort

Spillet har **8 kongekort**.

Hver konge har:

* 6 liv
* En unik evne
* Et offentlig oppdrag
* Eventuell elementtilknytning

## Terrengkort

Spillet har **20 terrengkort**.

Fordeling:

* 8 nøytrale
* 4 gress
* 4 flamme
* 4 vann

Foreløpig tierfordeling:

* 8 nøytrale terreng: 4 Tier 1, 3 Tier 2, 1 Tier 3
* 4 gressterreng: 2 Tier 1, 1 Tier 2, 1 Tier 3
* 4 flammeterreng: 2 Tier 1, 1 Tier 2, 1 Tier 3
* 4 vannterreng: 2 Tier 1, 1 Tier 2, 1 Tier 3

## Monsterkort

Spillet har **24 monsterkort**.

Fordeling:

* 6 nøytrale
* 6 gress
* 6 flamme
* 6 vann

Foreløpig tierfordeling per element:

* 3 Tier 1-monstre
* 2 Tier 2-monstre
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

Ved angrep betyr dette `+1` på angriperens kampverdi.

Ved forsvar betyr dette ett ekstra statisk forsvar.

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
* Foreløpig totalt 2-3 trykte ressurser

Tier 3:

* Sjeldne eller sterke terreng
* Viktige strategiske mål
* Kan ha en enkel spesialeffekt
* Foreløpig totalt 3-4 trykte ressurser eller en enkel spesialeffekt

Bare Tier 3-kort bør ha spesialeffekter i første prototype.

## Monsterkort

Monsterkort er spillets viktigste kampverktøy.

Hvert monsterkort har:

* Ett element
* Ressurskrav som ikoner
* Baseverdi
* Eventuelle kumulative ressursbonuser
* Et tier
* Eventuelt en enkel spesialeffekt

For å bruke et monster må spilleren oppfylle kravikonene øverst på kortet.

Monsterets styrke er baseverdien pluss alle bonuslinjer spilleren oppfyller.

Bonuslinjer er kumulative.

Eksempel:

> Base 1  
> 2 flamme: +1  
> 3 flamme: +1

En spiller med 3 flammeressurser får styrke 3 før eventuell elementfordel.

Styrken brukes slik:

* Ved angrep: styrken legges til angriperens terningresultat
* Ved forsvar: styrken er statisk forsvar

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
> Base 2  
> 3 flamme: +1  
> 3 nøytral: +1

Tier 3-spesialeffekter skal være enkle og små. De skal ikke gi ekstra full kamp, ekstra monsterbruk eller direkte skade på en konge uten kampresultat.

Eksempler på tillatte Tier 3-effekter:

* Rull én kampterning på nytt
* Reduser mottatt bondetap med 1
* Tell én bestemt ressurs som én annen ressurs bare for dette monsterets krav

---

# 4. Oppsett

## Felles oppsett

1. Bland konge-, terreng- og monsterbunken.
2. Trekk ett tilfeldig terrengkort og legg det midt på bordet. Det starter eierløst.
3. Alle spillerne ruller én kampterning. Høyeste resultat får startspillerbrikken. Ved likt resultat rulles det igjen.

## Spilleroppsett

Hver spiller gjør deretter følgende:

1. Trekk 2 kongekort, velg 1 og legg det andre nederst i kongebunken.
2. Trekk 3 terrengkort, velg 1 og legg resten nederst i terrengbunken.
3. Trekk 3 monsterkort, velg 1 og legg resten nederst i monsterbunken.
4. Legg det valgte terrengkortet inntil sentrumsterrenget.
5. Plasser kongebrikken og 2 bønder på startterrenget.
6. Sett kongens liv til 6 og legg kongekortet synlig foran deg.

Uvalgte kort vises ikke til de andre spillerne.

## Startspillerbrikken

Startspilleren starter runden. Turene går mot høyre, og startspillerbrikken flyttes én spiller mot høyre etter hver runde.

---

# 5. Rundestruktur og kortflyt

## Rundestruktur

Hver runde består av tre faser.

Fase 1: Plassering

* Spillerne tar hver sin tur fra startspilleren og videre mot høyre.

Fase 2: Kamp

* Konflikter løses.

Fase 3: Avslutning

* Tap, kontroll og ressurser oppdateres.
* Startspillerbrikken flyttes mot høyre.

## Starten av spillerens tur

Ved starten av turen:

1. Sjekk kongeoppdraget.
2. Spilleren vinner dersom alle kravene er oppfylt.
3. Spilleren får 1 ledig bondebrikke.
4. Spilleren kan trekke 1 terrengkort eller monsterkort dersom spilleren har plass på hånden.

## Hånden

En spiller kan ha maksimalt **3 kort på hånden totalt**.

Spilleren bestemmer selv blandingen av:

* Terrengkort
* Monsterkort

Kongekortet teller ikke som et håndkort.

Monsterkort er tilgjengelige så lenge spilleren har dem på hånden.

Monsterkort kastes ikke etter kamp og kan brukes på nytt så lenge kravene er oppfylt.

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
* Kongeoppdrag
* Ekspansjon
* Maksimal skade i kamp

Bønder gir kampterninger når de angriper, men gir ikke statisk forsvar.

Hver spiller starter med 2 bønder.

## Kontroll over terreng

En spiller kontrollerer et terreng dersom spilleren har flere bønder der enn alle motstanderne til sammen.

Kongen teller som 1 kontrollstyrke på terrenget den står på.

Kongen produserer ikke ressurser.

Kontrollverdi:

> Egne bønder + egen kongekontroll - alle fiendtlige bønder - eventuell fiendtlig kongekontroll

Eksempel: Rød har 3 bønder og Blå har 1. Røds kontrollverdi er 2, så Rød kontrollerer terrenget.

## Omstridt terreng

Dersom kontrollverdien er 0, er terrenget omstridt. Ingen kontrollerer det, det gir ingen ressurser og det kan ikke brukes som ekspansjonsvei.

## Ressurser

Bare bønder på terreng spilleren kontrollerer gir ressurser.

Ressurser brukes til:

* Monsterkrav
* Kongeoppdrag
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

* Ett kongekort
* Én fysisk kongebrikke

Kongen:

* Starter på spillerens første terreng
* Har 6 liv
* Kan ikke helbredes
* Teller som 1 kontrollstyrke
* Produserer ikke ressurser
* Kan flyttes til ett av spillerens målterreng under plasseringen
* Kan flyttes etter samme regler som bønder gjennom kontrollert, ublokkert territorium
* Kan ikke gå inn på et terreng som inneholder en annen konge

To konger kan stå på naboterreng.

## Kongens blokkering

Fiendtlige bønder kan plasseres på kongens terreng. Kongen blokkerer ekspansjonsveien gjennom terrenget så lenge den lever.

## Kongens liv

Kongens liv spores på kongekortet.

Når kongen når 0 liv, ødelegges kongen og spilleren taper.

---

# 9. Kamp

## Kampfase

Når alle spillere har fullført plasseringen sin, starter kampfasen.

Fra startspilleren og videre mot høyre får hver spiller én angrepsfase.

I sin angrepsfase løser spilleren én samlet kamp mot hver motstander spilleren deler konfliktterreng med.

Et konfliktterreng er et terreng der angriperen og forsvareren begge har minst én bonde eller konge.

Alle konfliktterreng mellom angriperen og den samme forsvareren inngår i én samlet kamp.

Monsterkrav bruker spillerens samlede ressurser fra hele kartet.

Eksempel:

* Rød har bønder på 3 terreng der Blå også har bønder
* Rød har totalt 7 bønder på disse terrengene
* Blå har 2, 2 og 1 bønder på de samme terrengene
* Dette løses som én samlet Rød-mot-Blå-kamp
* Tap kan bare tas fra disse 3 terrengene

## Monsterbruk i kamp

I hver samlet kamp kan hver side bruke maksimalt ett monsterkort.

Monsteret må:

* Være på hånden
* Oppfylle ressurskravene

Monsterkort velges samtidig og skjult.

Når begge sider har valgt monster, eller valgt å ikke bruke monster, avsløres valgene.

Ingen spiller kan endre monster etter avsløring.

Monsterkortet beholdes etter kampen og kan brukes igjen.

## Kamp uten monster

Dersom en side ikke bruker et monsterkort, brukes grunnverdi.

Vanlig kamp uten monster:

* Forsvar: 1 statisk forsvar
* Angrep: 0 statisk bonus

Angriperen får fortsatt terninger fra angripende bønder.

Kongens terreng uten monster:

* Forsvar: 2 statisk forsvar
* Angrep: 1 statisk bonus dersom kongen er på et av de involverte konfliktterrengene

Kongens grunnstyrke brukes bare uten monsterkort.

## Kampterningene

Angripende bønder er kampterninger.

Hver angripende bonde gir én kampterning.

Hver kampterning har sidene:

> `0, 0, 1, 1, 2, 2`

Angriperen ruller én terning per angripende bonde.

Terningresultatene summeres.

Forsvareren ruller ikke, men bruker statisk forsvar fra monsterkort, elementfordel og eventuell grunnverdi.

## Elementfordel i kamp

Dersom begge sider bruker monsterkort, sammenlignes elementene.

* Vann slår flamme
* Flamme slår gress
* Gress slår vann
* Nøytral har ingen fordel

Monsteret med elementfordel får `+1 styrke`.

Dersom bare én side bruker monster, finnes det ingen elementfordel.

## Kampresultat

Angriperens kampverdi er:

> Terningresultat + monsterstyrke + eventuell elementfordel + eventuell grunnverdi

Forsvarerens kampverdi er:

> Statisk forsvar fra monster + eventuell elementfordel + eventuell grunnverdi

Angriperens kampverdi sammenlignes med forsvarerens kampverdi. Høyeste verdi gir skade/tap tilsvarende differansen. Ved lik verdi skjer ingenting.

## Maksimal skade

En spiller kan aldri miste flere bønder enn spilleren har på de involverte konfliktterrengene.

Angriperen kan ikke påføre mer skade enn antall angripende bønder. Forsvareren kan ikke påføre mer tap enn antall forsvarende bønder.

Når en spiller mottar skade eller tap, velger den spilleren selv hvilke egne bønder som fjernes fra de involverte konfliktterrengene.

Eksempel:

* Angriperen har 2 bønder i den samlede kampen
* Forsvareren har 5 bønder i den samlede kampen
* Angriperen vinner med forskjell 4
* Angriperen påfører maksimalt 2 skade
* Forsvareren velger selv hvilke 2 egne bønder som fjernes fra de involverte terrengene

Flere bønder gir flere terninger og øker maksimal skade, men setter også flere bønder i fare.

## Kamp med flere spillere

Dersom en spiller deler konfliktterreng med flere motstandere, løser spilleren én samlet kamp mot hver motstander.

Samme terreng kan inngå i flere samlede kamper dersom tre eller flere spillere har bønder der.

Tap fjernes etter hver samlet kamp før neste samlet kamp løses.

Bare bønder som fortsatt står på de involverte konfliktterrengene kan brukes eller fjernes i senere samlede kamper.

## Kongens terreng i kamp

Kongens terreng kan bare ta skade dersom det er et av de involverte konfliktterrengene i den samlede kampen.

Når kongens terreng mottar skade, velger kongens eier hvordan skaden fordeles mellom:

* Bønder på involverte konfliktterreng
* Kongens liv

Dersom kongens terreng er involvert og ingen egne bønder står på kongens terreng, må skade som legges på kongens terreng tas fra kongens liv.

Eksempel:

Kongens terreng er involvert i en samlet kamp, og kongens eier mottar 3 skade.

Spilleren kan:

* Fjerne 3 bønder fra involverte konfliktterreng
* Fjerne 2 bønder og miste 1 kongeliv
* Fjerne 1 bonde og miste 2 kongeliv
* Miste 3 kongeliv dersom kongens terreng kan ta hele skaden

## Etter kamp

Etter hver samlet kamp:

* Tap fjernes umiddelbart
* Kontroll og ressurser beregnes på nytt for de involverte terrengene

Et terreng uten bønder blir eierløst.

---

# 10. Seier og sluttspill

## Kongeoppdrag

Hver konge har et offentlig oppdrag.

Eksempel:

> Kontroller 4 terreng, ha 3 nøytrale ressurser og 2 flammeressurser.

Spilleren vinner dersom alle oppdragskravene er oppfylt ved starten av spillerens tur.

Motstanderne får dermed resten av runden til å stoppe oppdraget.

Dersom flere spillere oppfyller kongeoppdraget i samme runde, vinner den spilleren som først starter sin tur med oppdraget oppfylt.

## Vanlige vinnemåter

En spiller vinner ved å:

* Ødelegge motstandernes konger
* Fullføre kongeoppdraget

Siste levende konge vinner.

## Sluttfase etter første utslåtte spiller

I spill med 2 spillere brukes ikke timeglasset. Spillet slutter straks én konge ødelegges.

I spill med 3-4 spillere starter timeglasset når den første kampfasen som ødelegger minst én konge er ferdig, dersom spillet ikke allerede har en vinner.

Når timeglasset starter:

1. Start et **10-minutters timeglass**.
2. Spillet fortsetter med vanlige regler.
3. Vanlige seierskrav gjelder fortsatt under nedtellingen.
4. Når tiden går ut, fullføres den aktive runden.
5. Dersom ingen har vunnet, avgjøres vinneren etter tiebreakere.

Tiebreakere:

1. Mest gjenværende kongeliv.
2. Flest kontrollerte terreng.
3. Flest bønder på kartet.

Dersom spillerne fortsatt står likt etter alle tre kriteriene, ender spillet uavgjort.

## Samtidig ødeleggelse av konger

Dersom flere konger ødelegges samtidig og bare én konge står igjen, vinner spilleren med den siste levende kongen.

Dersom alle gjenværende konger ødelegges i samme kampfase, avgjøres vinneren etter følgende rekkefølge:

1. Mest kongeliv ved starten av kampfasen.
2. Flest kontrollerte terreng etter kampfasen.
3. Flest bønder på kartet etter kampfasen.

Dersom spillerne fortsatt står likt, ender spillet uavgjort.

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

* Er én samlet kamp per angriper mot hver forsvarer rask nok i praksis?
* Blir det tydelig hvilke terreng som inngår i en samlet kamp?
* Fungerer det at samme terreng kan inngå i flere samlede kamper når tre eller flere spillere står der?
* Skaper umiddelbar fjerning av tap for mye fordel til spillere som angriper tidlig i kampfasen?

## Monsterkamp

* Er skjult monstervalg raskt nok i praksis?
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
* Antall Tier 3-kort med spesialeffekter
* Balansering av nøytrale monstre mot elementmonstre
