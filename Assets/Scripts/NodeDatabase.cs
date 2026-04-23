using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NodeDatabase", menuName = "Pathfinding/NodeDatabase")]
public class NodeDatabase : ScriptableObject
{
    public List<NodeData> nodes = new List<NodeData>();
}

[System.Serializable]
public class NodeData
{
    public string nodeName;     // nazwa punktu
    public string sceneName;    // nazwa sceny / piętra
    public string buildingID;
}