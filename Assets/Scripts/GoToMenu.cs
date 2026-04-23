using UnityEngine;
using UnityEngine.SceneManagement;

public class GoToMenu : MonoBehaviour
{

    [SerializeField] private string sceneName = "MainMenu";
    
    public void GoToMainMenu()
    {
        SceneManager.LoadScene(sceneName);
    }
}
