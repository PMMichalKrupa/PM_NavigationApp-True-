using UnityEngine;
using UnityEngine.SceneManagement;

public class GoToMenu : MonoBehaviour
{
    [SerializeField] private string sceneName = "MainMenu";

    public void GoToMainMenu()
    {
        HidePersistentUI();
        SceneManager.LoadScene(sceneName);
    }

    void HidePersistentUI()
    {
        UIPathSelector selector = FindObjectOfType<UIPathSelector>();

        if (selector != null)
        {
            Destroy(selector.transform.root.gameObject);
        }
    }
}