using UnityEngine;
using UnityEngine.SceneManagement;

public class StartSceneController : MonoBehaviour
{
    public float delay = 10f;
    private float timer = 0f;

    void Update()
    {
        timer += Time.deltaTime;

        if (timer >= delay || Input.GetKeyDown(KeyCode.Escape))
        {
            LoadNextScene();
        }
    }

    void LoadNextScene()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
    }
}