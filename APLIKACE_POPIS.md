# Popis aplikace Warehouse Simulator pro Gemini AI

## 📋 Obecný popis

**Warehouse Simulator** je Unity aplikace simulující sklad s automatizovanými vozíky (AGV - Automated Guided Vehicles). Aplikace je vytvořena jako bakalářská práce a demonstruje různé pathfinding algoritmy (A* a Dijkstra) pro navigaci vozíků ve skladu.

## 🏗️ Architektura aplikace

### Hlavní komponenty:

1. **GridManager** - Správa mřížky skladu (20x20 buněk)
2. **AgvManager** - Správa vozíků (spawning, tracking)
3. **OrderManager** - Správa objednávek (generování, přiřazování)
4. **Pathfinding** - Algoritmy A* a Dijkstra pro hledání cesty
5. **StatsManager** - Shromažďování statistik simulace
6. **SimulationManager** - Globální nastavení simulace (rychlost, pauza, algoritmus)
7. **LevelEditorManager** - Editor pro vytváření layoutu skladu
8. **UI komponenty** - SimulationUI, EditorUIManager, PerformanceGraph

### Namespaces:
- `Warehouse.Core` - Základní třídy (Order, TileType, CsvExporter)
- `Warehouse.Grid` - GridNode, OrderStatus
- `Warehouse.Managers` - Všechny manažery
- `Warehouse.Pathfinding` - Pathfinding algoritmy
- `Warehouse.Units` - AGVController
- `Warehouse.UI` - UI komponenty

## 🎮 Funkcionalita

### Hlavní funkce:

1. **Editor skladu:**
   - Vytváření zdí, regálů, loading/unloading docků, waiting areas
   - Spawnování vozíků
   - Ukládání/načítání layoutu (JSON)

2. **Simulace:**
   - LoadingDock automaticky generuje objednávky naskladnění (LoadingDock → Shelf)
   - UnloadingDock automaticky generuje objednávky vyskladnění (Shelf → UnloadingDock)
   - Vozíky automaticky přijímají objednávky a plní je
   - Po dokončení objednávky vozík jede na waiting area

3. **Pathfinding:**
   - A* algoritmus (s heuristikou)
   - Dijkstra algoritmus (bez heuristiky)
   - Kolizní detekce a rerouting při zablokování

4. **Statistiky:**
   - Počet dokončených objednávek
   - Průměrný čas dokončení
   - Celková ujetá vzdálenost
   - Počet kolizí
   - Vytížení vozíků (%)
   - Export do CSV

5. **Vizuální prvky:**
   - Barva vozíků podle stavu (Idle, MovingToPickup, Loading, MovingToDelivery, Unloading, MovingToWaiting)
   - Zobrazení cesty vozíku (LineRenderer)
   - Blikající efekty na dockách při vytvoření objednávky
   - Graf výkonnosti v reálném čase
   - Heatmapa návštěv

## 🔧 Technické detaily

### Singleton pattern:
Všechny manažery používají Singleton pattern:
- `GridManager.Instance`
- `AgvManager.Instance`
- `OrderManager.Instance`
- `StatsManager.Instance`
- `SimulationManager.Instance`
- `HeatmapManager.Instance`

### Stavy vozíku (AGVState):
- `Idle` - Čeká na odpočívadle
- `MovingToPickup` - Jede pro zboží
- `Loading` - Nakládá zboží (2 sekundy)
- `MovingToDelivery` - Veze zboží do cíle
- `Unloading` - Vykládá zboží (2 sekundy)
- `MovingToWaiting` - Jede na odpočívadlo

### Typy dlaždic (TileType):
- `Empty` - Prázdné místo
- `Wall` - Zeď (neprůchozí)
- `Shelf` - Regál (průchozí)
- `LoadingDock` - Příjem zboží (průchozí)
- `UnloadingDock` - Výdej zboží (průchozí)
- `WaitingArea` - Odpočívadlo pro vozíky (průchozí)

### Objednávky (Order):
- `OrderId` - Unikátní ID
- `PickupNode` - Místo vyzvednutí
- `DeliveryNode` - Místo doručení
- `Status` - Pending, Assigned, PickedUp, Completed
- `TotalDistance` - Celková ujetá vzdálenost pro tuto objednávku
- `CollisionCount` - Počet kolizí během této objednávky

## ⚠️ Možné problémy a errory

### 1. Null Reference Exceptions:

**Rizikové místa:**
- `AgvManager.SpawnAgv()` - `GridManager.Instance` může být null při špatném pořadí inicializace
- `OrderManager.AssignOrders()` - `AgvManager.Instance` může být null
- `AGVController.SetDestination()` - `CurrentNode` může být null při prvním volání
- `LevelEditorManager.HandleInput()` - `Camera.main` může být null
- `PerformanceGraph.Update()` - `StatsManager.Instance` může být null

**Řešení:** Většina míst má null checks, ale některé mohou chybět při edge cases.

### 2. Race Conditions:

**Problém:** Singleton inicializace může být problém při paralelním načítání scén.

**Rizikové místa:**
- Všechny `Awake()` metody singletonů
- `OrderManager.Update()` volá `AssignOrders()` každý frame

### 3. Memory Leaks:

**Potenciální problémy:**
- `PerformanceGraph` vytváří nové GameObjecty pro body grafu, ale může je špatně mazat
- `OrderManager.ShowDockIndicator()` vytváří nový Material, ale může ho špatně uvolnit při přerušení coroutine
- Event subscribers (`OnStatsChanged`, `OnQueueChanged`) se nemusí odhlásit při zničení objektů

### 4. Pathfinding Issues:

**Problémy:**
- `Pathfinding.FindPath()` resetuje GCost/HCost, ale může být problém pokud se volá paralelně
- Rerouting při kolizi může způsobit nekonečnou smyčku pokud jsou všechny cesty zablokované
- `GridNode.OccupiedBy` může být nastaveno, ale vozík může být zničen → "ghost" rezervace

### 5. UI Issues:

**Problémy:**
- `SimulationUI.UpdateOrderQueue()` může být volána když `_orderQueueContent` je null
- `PerformanceGraph` LineRenderer v UI může mít problémy s koordináty
- Prefaby mohou chybět v Inspectoru → runtime errors

### 6. CSV Export:

**Problémy:**
- `CsvExporter.GetPath()` používá `Application.dataPath` což může být problém v buildu
- Soubor může být zamčený pokud je otevřený v Excelu
- Chybí error handling pro write operace

### 7. Kolizní logika:

**Problémy:**
- Vozík čeká max 2 sekundy, pak reroute - ale může být problém pokud je cesta stále zablokovaná
- `IsAvailable()` kontroluje `OccupiedBy == null || OccupiedBy == asker`, ale může být race condition
- Více vozíků může současně rezervovat stejný uzel

### 8. Order Generation:

**Problémy:**
- `CreateInboundOrder()` a `CreateOutboundOrder()` mohou selhat pokud není dostatek regálů/docků
- Náhodný výběr může způsobit, že některé docky/regály nejsou nikdy použity
- Fronta může růst nekonečně pokud není dostatek vozíků

## 📝 Důležité poznámky pro Gemini

### Při práci s kódem:

1. **Vždy kontroluj null reference** před přístupem k singletonům
2. **Pozor na pořadí inicializace** - GridManager musí být inicializován před AgvManagerem
3. **Event unsubscription** - Vždy odhlas eventy v `OnDestroy()`
4. **Coroutines** - Ujisti se že jsou správně zastaveny při zničení objektu
5. **Material instances** - Vždy je uvolni pomocí `Destroy()` po použití
6. **UI updates** - Kontroluj zda UI elementy existují před aktualizací

### Konvence kódu:

- Private fields začínají `_`
- Public properties používají PascalCase
- Singleton pattern: `Instance` property
- Events používají `OnXxxChanged` naming
- SerializeField pro nastavení v Inspectoru

### Dependencies:

- Unity 2021.3+ (předpokládáno)
- TextMeshPro (pro UI texty)
- Universal Render Pipeline (URP) - podle materiálů

### Build settings:

- CSV export používá `Application.dataPath` - v buildu to může být problém
- Level storage používá `Application.persistentDataPath` - správně pro build

## 🎯 Aktuální stav implementace

### Dokončeno:
- ✅ Základní pathfinding (A*, Dijkstra)
- ✅ Správa vozíků a objednávek
- ✅ Editor skladu
- ✅ Statistiky a CSV export
- ✅ Vizuální indikátory stavu vozíků
- ✅ Zobrazení cesty vozíku
- ✅ Vizuální efekty na dockách
- ✅ Graf výkonnosti (základní implementace)
- ✅ Fronta objednávek v UI
- ✅ Indikátor vytížení vozíků

### Částečně dokončeno:
- ⚠️ Graf výkonnosti - funguje, ale LineRenderer v UI může mít problémy
- ⚠️ Fronta objednávek - funguje, ale vyžaduje správné UI setup

### TODO:
- Statistiky per vozík
- Vylepšení kolizní logiky
- Optimalizace pathfindingu (priority queue)
- Vizuální zvýraznění aktivní objednávky
- Vizuální indikátor kolizí

## 🔍 Debugging tips

1. **Console logs** - Většina důležitých akcí má Debug.Log
2. **Gizmos** - GridManager má OnDrawGizmos pro vizualizaci gridu
3. **Stats** - StatsManager shromažďuje všechny důležité metriky
4. **CSV export** - Všechny dokončené objednávky se ukládají do CSV

## 📚 Klíčové soubory

- `Assets/Scripts/Units/AGVController.cs` - Hlavní logika vozíku
- `Assets/Scripts/Managers/OrderManager.cs` - Správa objednávek
- `Assets/Scripts/Pathfinding/Pathfinding.cs` - Pathfinding algoritmy
- `Assets/Scripts/Managers/GridManager.cs` - Správa mřížky
- `Assets/Scripts/UI/SimulationUI.cs` - Hlavní UI
- `Assets/Scripts/Core/Order.cs` - Datová struktura objednávky
- `Assets/Scripts/Grid/GridNode.cs` - Uzel mřížky

---

**Poznámka:** Tento dokument popisuje aktuální stav aplikace k datu vytvoření. Při práci s kódem vždy ověř aktuální stav implementace.
