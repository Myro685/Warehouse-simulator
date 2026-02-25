using UnityEngine;
using System.IO;
using System.Text;
namespace Warehouse.Core
{
    public static class CsvExporter
    {
        private static string _filePath = "SimulationResults.csv";
        public static void Initialize()
        {
            if (!File.Exists(GetPath()))
            {
                string header = "OrderID;Algorithm;Distance;Duration;WaitingCount;CreatedTime;FinishedTime";
                File.WriteAllText(GetPath(), header + "\n");
                Debug.Log($"CSV soubor vytvořen: {GetPath()}");
            }
        }
        public static void WriteRow(int orderId, string algo, float distance, float duration, int waitCount, float created, float finished)
        {
            string row = $"{orderId};{algo};{distance:F2};{duration:F2};{waitCount};{created:F2};{finished:F2}";
            File.AppendAllText(GetPath(), row + "\n");
        }
        private static string GetPath()
        {
            #if UNITY_EDITOR
                return Path.Combine(Application.dataPath, "../", _filePath);
            #else
                return Path.Combine(Application.persistentDataPath, _filePath);
            #endif
        }
    }
}