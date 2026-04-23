using UnityEngine;
using UnityEngine.SceneManagement;

public class FloorChangeUI : MonoBehaviour
{
    public GameObject panel;

    private string targetScene;
    private string targetNode;

    public void Show(string sceneName, string nodeName)
    {
        panel.SetActive(true);

        targetScene = sceneName;
        targetNode = nodeName;
    }
    public void UpdateVisibility(Node startNode, Node targetNode)
    {
        if (startNode == null || targetNode == null)
        {
            panel.SetActive(false);
            return;
        }

        bool sameScene = startNode.sceneName == targetNode.sceneName;

        panel.SetActive(!sameScene);
    }
    public void OnClickGo()
    {
        panel.SetActive(false); // ukryj od razu
        SceneManager.LoadScene(targetScene);
    }
}