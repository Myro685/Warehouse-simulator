# Seznam nalezených možných chyb a doporučené opravy

## 🔴 Kritické problémy

### 1. Race condition při inicializaci singletonů
**Lokace:** Všechny manažery (Awake metody)
**Problém:** Pokud jsou dva instance stejného manažera, jeden se zničí, ale může být problém s pořadím
**Doporučení:** Přidat `DontDestroyOnLoad` pro důležité manažery nebo použít ScriptableObject pro konfiguraci

### 2. Memory leak v OrderManager.ShowDockIndicator()
**Lokace:** `Assets/Scripts/Managers/OrderManager.cs:280-320`
**Problém:** Pokud se coroutine přeruší (např. při zničení objektu), Material se nemusí uvolnit
**Oprava:**
```csharp
private void OnDestroy()
{
    // Zastav všechny běžící coroutines
    StopAllCoroutines();
}
```

### 3. Ghost rezervace uzlů
**Lokace:** `Assets/Scripts/Units/AGVController.cs:OnDestroy()`
**Problém:** Pokud se vozík zničí během pohybu, může zůstat rezervace uzlu
**Aktuální řešení:** Existuje, ale může být problém při náhlém zničení
**Doporučení:** Přidat cleanup v `OnDisable()` také

## 🟡 Střední problémy

### 4. Null check chybí v AgvManager.SpawnAgv()
**Lokace:** `Assets/Scripts/Managers/AgvManager.cs:40`
**Problém:** `GridManager.Instance` může být null při špatném pořadí inicializace
**Oprava:**
```csharp
if (GridManager.Instance == null)
{
    Debug.LogError("GridManager není inicializován!");
    return;
}
GridNode node = GridManager.Instance.GetNode(x, y);
```

### 5. Event unsubscription může chybět
**Lokace:** `Assets/Scripts/UI/SimulationUI.cs:OnDestroy()`
**Problém:** Pokud se UI zničí před OrderManagerem, event zůstane přihlášený
**Aktuální řešení:** Existuje, ale mělo by být ověřeno
**Doporučení:** Přidat try-catch nebo kontrolu existence

### 6. CSV export path v buildu
**Lokace:** `Assets/Scripts/Core/CsvExporter.cs:34`
**Problém:** `Application.dataPath` není dostupný v buildu
**Oprava:**
```csharp
private static string GetPath()
{
    #if UNITY_EDITOR
        return Path.Combine(Application.dataPath, "../", _filePath);
    #else
        return Path.Combine(Application.persistentDataPath, _filePath);
    #endif
}
```

### 7. PerformanceGraph může vytvářet memory leak
**Lokace:** `Assets/Scripts/UI/PerformanceGraph.cs:UpdateGraph()`
**Problém:** Vytváří nové GameObjecty, ale může je špatně mazat při rychlých aktualizacích
**Doporučení:** Použít object pooling pro body grafu

### 8. Pathfinding reset může být problém při paralelních voláních
**Lokace:** `Assets/Scripts/Pathfinding/Pathfinding.cs:79-86`
**Problém:** Pokud se pathfinding volá paralelně pro více vozíků, může dojít k race condition
**Doporučení:** Přidat lock nebo použít thread-safe struktury (ale Unity není thread-safe, takže to není kritické)

## 🟢 Menší problémy

### 9. Chybí validace v OrderManager.CreateOrder()
**Lokace:** `Assets/Scripts/Managers/OrderManager.cs:111`
**Problém:** Kontroluje IsWalkable, ale ne kontroluje zda jsou uzly ve stejné mřížce
**Doporučení:** Přidat kontrolu

### 10. GetRandomWalkableNode() není použita
**Lokace:** `Assets/Scripts/Managers/OrderManager.cs:87`
**Problém:** Metoda existuje, ale není nikde volána
**Doporučení:** Smazat nebo použít

### 11. Hardcoded hodnoty v některých místech
**Lokace:** Různé
**Problém:** Některé hodnoty jsou hardcoded (např. čekací doba 2 sekundy)
**Doporučení:** Přesunout do SerializeField nebo ScriptableObject

### 12. Chybí error handling v LevelStorageManager
**Lokace:** `Assets/Scripts/Managers/LevelStorageManager.cs`
**Problém:** JSON deserializace může selhat při špatném formátu
**Doporučení:** Přidat try-catch s lepším error handlingem

## 📋 Doporučené opravy (prioritizované)

### Vysoká priorita:
1. ✅ Opravit CSV export path pro build
2. ✅ Přidat null check pro GridManager.Instance v AgvManager
3. ✅ Přidat cleanup v OrderManager.OnDestroy() pro coroutines

### Střední priorita:
4. Přidat object pooling pro PerformanceGraph body
5. Přidat validaci v CreateOrder()
6. Přidat lepší error handling v LevelStorageManager

### Nízká priorita:
7. Smazat nepoužívanou metodu GetRandomWalkableNode()
8. Přesunout hardcoded hodnoty do konfigurace
9. Přidat více debug informací

## 🔍 Jak testovat opravy:

1. **CSV export:** Spusť build a zkontroluj zda se CSV vytváří správně
2. **Null checks:** Zkus zničit GridManager během runtime a zkontroluj chyby
3. **Memory leaks:** Použij Unity Profiler pro kontrolu memory leaks
4. **Event unsubscription:** Zkontroluj Console při zničení UI objektů

## 📝 Poznámky:

- Většina problémů je edge cases a neovlivní běžné použití
- Aplikace je funkční pro bakalářskou práci
- Některé problémy jsou spíše optimalizace než chyby
- Všechny kritické problémy mají workaround nebo jsou řešitelné
