using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    // Nazwa sceny
    [SerializeField] private string HenrykPoboznyP1 = "HP1";
    [SerializeField] private string WalyChrobregoP0 = "WCh0";

    public void EnterHP()
    {
        SceneManager.LoadScene(HenrykPoboznyP1);
    }
    public void EnterWCh()
    {
        SceneManager.LoadScene(WalyChrobregoP0);
    }
}