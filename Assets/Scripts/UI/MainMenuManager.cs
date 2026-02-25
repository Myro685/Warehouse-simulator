using UnityEngine;
using UnityEngine.SceneManagement; 
namespace Warehouse.UI
{
    public class MainMenuManager : MonoBehaviour
    {
        [SerializeField] private string _simulationSceneName = "SampleScene"; 
        public void StartSimulation()
        {
            SceneManager.LoadScene(_simulationSceneName);
        }
        public void QuitGame()
        {
            Debug.Log("Ukončuji aplikaci...");
            Application.Quit();
        }
    }
}