# TODO - Doporučená vylepšení pro Warehouse Simulator

## 🎨 Vizuální vylepšení

### Vysoká priorita
- [ ] **Vizuální indikátory stavu vozíků** - Změna barvy vozíku podle stavu (Idle=šedá, MovingToPickup=žlutá, Loading=modrá, MovingToDelivery=zelená, Unloading=červená, MovingToWaiting=oranžová)
- [ ] **Vizuální zobrazení cesty vozíku** - Zobrazit trasu jako čáru nad zemí (LineRenderer už existuje, jen zviditelnit)
- [ ] **Vizuální indikátory na LoadingDock/UnloadingDock** - Světelný efekt nebo animace když generují novou objednávku
- [ ] **Vizuální feedback při dokončení objednávky** - Partiklový efekt nebo zvuk při dokončení úkolu

### Střední priorita
- [ ] **Vizuální zobrazení fronty objednávek** - Panel se seznamem čekajících objednávek v UI
- [ ] **Vizuální indikátor vytížení vozíků** - Progress bar nebo procento vytížení každého vozíku
- [ ] **Vizuální zvýraznění aktivní objednávky** - Zvýraznit pickup a delivery místa aktuální objednávky vozíku
- [ ] **Vizuální indikátor kolizí** - Zobrazit místo kolize a dobu čekání

## 📊 Statistiky a analýza

### Vysoká priorita
- [ ] **Opravit TODO v OrderManager.cs** - Ukládat vzdálenost a počet kolizí do každé objednávky (řádky 147-149)
- [ ] **Graf výkonnosti v reálném čase** - Zobrazit graf dokončených objednávek v čase
- [ ] **Statistiky per vozík** - Kolik objednávek dokončil každý vozík, celková ujetá vzdálenost, průměrný čas

### Střední priorita
- [ ] **Export statistik do grafu** - Vytvořit Python skript pro vizualizaci CSV dat (grafy, histogramy)
- [ ] **Historické statistiky** - Ukládat statistiky po restartu simulace
- [ ] **Porovnání algoritmů** - Automatické porovnání A* vs Dijkstra s exportem výsledků
- [ ] **Statistiky vytížení regálů** - Které regály jsou nejvíce využívané
- [ ] **Statistiky čekacích dob** - Jak dlouho objednávky čekají ve frontě

## ⚡ Optimalizace a logika

### Vysoká priorita
- [ ] **Inteligentní výběr vozíku** - Vybrat nejbližší volný vozík k pickup místu místo prvního dostupného
- [ ] **Priorita objednávek** - Možnost nastavit prioritu objednávek (např. vyskladnění má vyšší prioritu)
- [ ] **Vylepšení výběru parkovacího místa** - Vozík by měl vybrat parkoviště blízko míst, kde jsou často objednávky

### Střední priorita
- [ ] **Dynamické přepočítávání cesty** - Pokud je vozík zablokován, přepočítat trasu dříve než po 2 sekundách
- [ ] **Více vozíků na jednom místě** - Umožnit více vozíkům stát na WaitingArea současně
- [ ] **Vylepšení kolizní logiky** - Lepší řešení deadlock situací (když se vozíky zablokují navzájem)
- [ ] **Optimalizace pathfindingu** - Použít priority queue místo List pro openSet (rychlejší A*)

## 🎮 UX vylepšení

### Vysoká priorita
- [ ] **Možnost pauzovat jednotlivé typy objednávek** - Tlačítka pro zapnutí/vypnutí LoadingDock a UnloadingDock generování
- [ ] **Vizuální zvýraznění aktivního tlačítka v editoru** - Změna barvy aktivního nástroje (TODO v EditorUIManager.cs řádek 47)
- [ ] **Tooltipy v UI** - Vysvětlivky k jednotlivým statistikám a tlačítkům

### Střední priorita
- [ ] **Možnost kliknout na vozík a zobrazit jeho informace** - Panel s detaily vozíku (stav, aktuální objednávka, statistiky)
- [ ] **Možnost ručně vytvořit objednávku** - Tlačítko v UI pro vytvoření objednávky mezi dvěma místy
- [ ] **Možnost smazat vozík** - Kliknutí pravým tlačítkem na vozík v editoru
- [ ] **Ukládání nastavení simulace** - Uložit rychlost, zapnuté/vypnuté docky do PlayerPrefs

## 🔧 Technické vylepšení

### Střední priorita
- [ ] **Odstranit nepoužívanou metodu GetRandomWalkableNode()** - Není nikde použita
- [ ] **Přidat validaci při načítání levelu** - Kontrola, zda má level všechny potřebné komponenty
- [ ] **Vylepšení error handlingu** - Lepší error messages a recovery při chybách
- [ ] **Unit testy** - Základní testy pro pathfinding a správu objednávek
- [ ] **Dokumentace kódu** - XML komentáře pro všechny public metody

## 📈 Analytické funkce

### Střední priorita
- [ ] **Analýza bottlenecků** - Identifikovat místa, kde vozíky nejčastěji čekají
- [ ] **Doporučení optimalizace** - Systém navrhne změny na základě statistik (např. přidat více vozíků)
- [ ] **Simulace různých scénářů** - Možnost spustit více simulací s různými parametry a porovnat výsledky
- [ ] **Heatmapa čekacích dob** - Zobrazit, kde vozíky nejvíce čekají

## 🎯 Pro bakalářskou práci (doporučeno)

### Nejdůležitější pro prezentaci:
1. ✅ **Vizuální indikátory stavu vozíků** - Učiní simulaci přehlednější
2. ✅ **Graf výkonnosti v reálném čase** - Skvělé pro prezentaci výsledků
3. ✅ **Opravit TODO v OrderManager** - Kompletní data pro analýzu
4. ✅ **Inteligentní výběr vozíku** - Ukáže optimalizaci systému
5. ✅ **Porovnání algoritmů s exportem** - Hlavní část bakalářky

### Bonus pro dojem:
- Vizuální efekty při dokončení objednávky
- Možnost pauzovat jednotlivé typy objednávek
- Tooltipy v UI

---

**Poznámka:** Priorita je subjektivní - zaměř se na to, co je nejdůležitější pro tvou bakalářskou práci a prezentaci výsledků.
