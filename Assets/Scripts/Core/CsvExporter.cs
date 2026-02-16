using UnityEngine;
using System.IO;
using System.Text;

namespace Warehouse.Core
{
    public static class CsvExporter
    {
        // Soubor se uloží do složky projektu (nad Assets)
        private static string _filePath = "SimulationResults.csv";

        public static void Initialize()
        {
            // Pokud soubor neexistuje, vytvoříme ho a napíšeme hlavičku
            if (!File.Exists(GetPath()))
            {
                string header = "OrderID;Algorithm;Distance;Duration;WaitingCount;CreatedTime;FinishedTime";
                File.WriteAllText(GetPath(), header + "\n");
                Debug.Log($"CSV soubor vytvořen: {GetPath()}");
            }
        }

        public static void WriteRow(int orderId, string algo, float distance, float duration, int waitCount, float created, float finished)
        {
            // Nahradíme tečky čárkami, pokud máš český Excel, nebo naopak. 
            // InvariantCulture (tečka) je bezpečnější pro zpracování v Pythonu/Google Sheets.
            string row = $"{orderId};{algo};{distance:F2};{duration:F2};{waitCount};{created:F2};{finished:F2}";
            
            File.AppendAllText(GetPath(), row + "\n");
        }

        private static string GetPath()
        {
            #if UNITY_EDITOR
                // V editoru ukládáme vedle Assets složky
                return Path.Combine(Application.dataPath, "../", _filePath);
            #else
                // V buildu ukládáme do persistent data path
                return Path.Combine(Application.persistentDataPath, _filePath);
            #endif
        }
    }
}