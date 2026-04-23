using UnityEngine;
using UnityEngine.InputSystem;

public class MapClickHandler : MonoBehaviour
{
    public LayerMask mapLayer;
    public UIPathSelector selector;

    void Update()
    {
        //if (Mouse.current == null)
        //{
        //    Debug.Log("Brak myszy!");
        //    return;
        //}

        //if (Mouse.current.leftButton.wasPressedThisFrame)
        //{
        //    Debug.Log("Klik!");

        //    if (!selector.WaitingForCustomStart)
        //    {
        //        Debug.Log("CUSTOM NIEAKTYWNY");
        //        return;
        //    }

        //    Debug.Log("CUSTOM aktywny, robiê raycast");

        //    Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());

        //    if (Physics.Raycast(ray, out RaycastHit hit, 1000f, mapLayer))
        //    {
        //        Debug.Log("TRAFIONO W MAPÊ");
        //        selector.SetCustomStart(hit.point);
        //    }
        //    else
        //    {
        //        Debug.Log("Raycast nie trafi³ w mapê");
        //    }
        //}
    }

}
