using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class NodeDatabaseEditor : EditorWindow
{
    public NodeDatabase database;

    [MenuItem("Tools/Node Database Builder")]
    public static void ShowWindow()
    {
        GetWindow<NodeDatabaseEditor>("Node Database Builder");
    }
    string GetBuildingID(Node n)
    {
        // NAJPROSTSZA WERSJA:
        // scena = budynek (na start)
        return n.gameObject.scene.name;
    }
    private void OnGUI()
    {
        GUILayout.Label("Node Database Builder", EditorStyles.boldLabel);

        database = (NodeDatabase)EditorGUILayout.ObjectField("Node Database", database, typeof(NodeDatabase), false);

        if (GUILayout.Button("Scan Scene Nodes (replace all)"))
        {
            if (database == null) return;
            ScanNodes(true); // usuwa stare
        }

        if (GUILayout.Button("Scan Scene Nodes (add to existing)"))
        {
            if (database == null) return;
            ScanNodes(false); // dodaje do istniejącej listy
        }
    }

    void ScanNodes(bool clearBeforeScan = false)
    {
        if (clearBeforeScan)
            database.nodes.Clear();

        Node[] allNodes = GameObject.FindObjectsOfType<Node>();

        foreach (var n in allNodes)
        {
            // sprawdź czy już istnieje
            if (database.nodes.Exists(nd => nd.nodeName == n.name && nd.sceneName == n.gameObject.scene.name))
                continue;

            NodeData data = new NodeData();
            data.nodeName = n.name;
            data.sceneName = n.gameObject.scene.name;
            data.buildingID = n.buildingID;

            database.nodes.Add(data);
        }

        EditorUtility.SetDirty(database);
        AssetDatabase.SaveAssets();

        Debug.Log($"Zeskanowano {allNodes.Length} node'ów i zapisano do bazy.");
    }
}