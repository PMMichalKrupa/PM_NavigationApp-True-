using UnityEngine;
using UnityEngine.SceneManagement;

public static class SceneChangeManager
{
    public static void GoToScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }
}