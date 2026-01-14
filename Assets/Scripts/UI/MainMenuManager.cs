using UnityEngine;
using UnityEngine.SceneManagement; 

namespace Warehouse.UI
{
    public class MainMenuManager : MonoBehaviour
    {
        // Název scény se hrou 
        [SerializeField] private string _simulationSceneName = "SampleScene"; 

        public void StartSimulation()
        {
            // Načte herní scénu
            SceneManager.LoadScene(_simulationSceneName);
        }

        public void QuitGame()
        {
            Debug.Log("Ukončuji aplikaci...");
            Application.Quit();
        }
    }
}