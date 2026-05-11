using UnityEngine;
using TMPro;

public class UIController : MonoBehaviour
{
    [SerializeField] private GameObject tooltip; // obiekt z ca³¹ map¹ (Plane + œciany)
    [SerializeField] public TMP_Text MessageText;
    private float MessageTimer = 0;
    public void ToggleTooltip()
    {
        if (tooltip != null)
            tooltip.SetActive(!tooltip.activeSelf);
    }

    private void Start()
    {
        MessageText.text = " ";
    }
    private void Update()
    {
        if (MessageTimer >= 0)
        {
            MessageTimer -= Time.deltaTime;
        }
        else
        {
            MessageText.text = " ";
        }
    }
    public void ShowMessage(string message = "")
    {
        MessageTimer = 10f;
        MessageText.text = message;
    }
}
