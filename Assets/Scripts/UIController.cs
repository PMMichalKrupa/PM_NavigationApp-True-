using UnityEngine;

public class UIController : MonoBehaviour
{
    public GameObject tooltip; // obiekt z ca³¹ map¹ (Plane + œciany)

    public void ToggleTooltip()
    {
        if (tooltip != null)
            tooltip.SetActive(!tooltip.activeSelf);
    }
}
