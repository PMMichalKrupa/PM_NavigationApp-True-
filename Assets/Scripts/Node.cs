using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class Node : MonoBehaviour
{
    [Tooltip("S¹siednie wêz³y, z którymi ten node jest po³¹czony")]
    public List<Node> neighbors = new List<Node>();
    [Tooltip("Po³¹czenia niewidoczne wizualnie")]
    public List<Node> ghostNeighbors = new List<Node>();
    [Tooltip("Przypisane wyjœcie awaryjne")]
    public Node emergencyExit;
    [Header("Building")]
    public string buildingID;
    [Header("Multi-floor")]
    public bool isStairs = false;

    // ID klatki schodowej (np. "A", "B", "C")
    public string staircaseID;

    // Lista po³¹czeñ z innymi piêtrami
    public List<FloorConnection> connections = new List<FloorConnection>();

    [HideInInspector]
    public string sceneName; // nazwa sceny lub piêtra
    void OnValidate()
    {
        sceneName = gameObject.scene.name;
    }
    void Awake()
    {
        sceneName = gameObject.scene.name;
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        Vector3 pos = transform.position + Vector3.up * 0.1f;

        // Kolor punktu (drzwi / zwyk³y node)
        Gizmos.color = gameObject.name.EndsWith("_o") ? Color.orange : Color.yellow;
        Gizmos.DrawSphere(pos, 0.1f);

        // Linie do s¹siadów
        Gizmos.color = Color.green;
        foreach (var neighbor in neighbors)
        {
            if (neighbor != null)
            {
                Gizmos.DrawLine(pos, neighbor.transform.position + Vector3.up * 0.1f);
            }
        }

        // NAZWA NODE'A
        Handles.Label(
            pos + Vector3.up * 0.25f,
            gameObject.name
        );
    }
#endif
}
[System.Serializable]
public class FloorConnection
{
    public string targetScene;     // np. "Floor2"
    public string targetNodeName;  // np. "Schody_A_2"
}