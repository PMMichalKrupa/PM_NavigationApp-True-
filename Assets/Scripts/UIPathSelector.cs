using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static SimplePathfinder;
using UnityEngine.SceneManagement;

public class UIPathSelector : MonoBehaviour
{
    public Dropdown startDropdown;
    public Dropdown endDropdown;
    public SimplePathfinder pathfinder;
    private List<NodeData> allNodes = new List<NodeData>();
    private NodeData chosenStart;
    NodeData chosenEnd;
    bool useEmergencyExit;
    private static UIPathSelector instance;


    void Start()
    {
        if (instance != null && instance != this)
        {
            Destroy(transform.root.gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(transform.root.gameObject);

        Debug.Log("UIPathSelector Start()");
        bool fromSceneTransition = PlayerPrefs.GetInt("SceneTransition", 0) == 1;
        PlayerPrefs.DeleteKey("SceneTransition");

        // Pobierz wszystkie node'y z bazy danych
        NodeDatabase db = Resources.Load<NodeDatabase>("NodeDatabase");

        if (db != null)
        {
            allNodes = new List<NodeData>(db.nodes);

            // filtr "_o"
            allNodes = allNodes.FindAll(n => !n.nodeName.EndsWith("_o"));

            // sortowanie
            allNodes.Sort((a, b) => (a.sceneName + a.nodeName).CompareTo(b.sceneName + b.nodeName));
        }
        else
        {
            Debug.LogError("Nie znaleziono NodeDatabase w Resources!");
        }

        // Przygotuj nazwy do dropdownów
        List<string> nodeNames = new List<string>();

        // CUSTOM tylko dla startu
        nodeNames.Insert(0, "Wybierz salę");
        nodeNames.Insert(1, "Wybierz z planu");

        foreach (var node in allNodes)
        {
            string displayName = node.nodeName + " (" + node.sceneName + ")";
            nodeNames.Add(displayName);
        }

        startDropdown.ClearOptions();
        startDropdown.AddOptions(nodeNames);
        endDropdown.ClearOptions();

        List<string> endNames = new List<string>();

        endNames.Insert(0, "Wybierz salę");
        endNames.Insert(1, "Wybierz z planu");
        endNames.Insert(2, "Wyjście awaryjne");
        foreach (var node in allNodes)
        {
            string displayName = node.nodeName + " (" + node.sceneName + ")";
            endNames.Add(displayName);
        }
        endDropdown.AddOptions(endNames);
        if (!fromSceneTransition)
        {
            startDropdown.SetValueWithoutNotify(0);
            endDropdown.SetValueWithoutNotify(0);

            chosenStart = null;
            chosenEnd = null;
            useEmergencyExit = false;

            pathfinder.ClearPath();
            return;
        }
        string startName = PlayerPrefs.GetString("NextStartNode");
        string targetName = PlayerPrefs.GetString("FinalTargetNode");

        Node startNodeReal = pathfinder.FindNodeByName(startName);
        Node targetNodeReal = pathfinder.FindNodeByName(targetName);

        if (startNodeReal != null && targetNodeReal != null)
        {
            pathfinder.startNode = startNodeReal;
            pathfinder.targetNode = targetNodeReal;

            string savedUIStart = PlayerPrefs.GetString("SavedUIStartNode");
            string savedTargetScene = PlayerPrefs.GetString("FinalTargetScene");

            chosenStart = allNodes.Find(n =>
                n.nodeName == savedUIStart);

            chosenEnd = allNodes.Find(n =>
                n.nodeName == targetName &&
                n.sceneName == savedTargetScene);

            if (chosenStart != null)
            {
                int startIndex = allNodes.IndexOf(chosenStart) + 2;
                startDropdown.SetValueWithoutNotify(startIndex);
                startDropdown.RefreshShownValue();
            }

            if (chosenEnd != null)
            {
                int endIndex = allNodes.IndexOf(chosenEnd) + 3;
                endDropdown.SetValueWithoutNotify(endIndex);
                endDropdown.RefreshShownValue();
            }

            pathfinder.DrawPath();
        }
        SceneManager.sceneLoaded += OnSceneLoaded;
    }
    IEnumerator RestoreAfterFrame()
    {
        yield return null; // czekamy aż UI się ustabilizuje

    }
    public void ToggleVisualMode()
    {
        if (pathfinder.visualMode == PathVisualMode.LineWithMarker)
            pathfinder.visualMode = PathVisualMode.ThickLine;
        else
            pathfinder.visualMode = PathVisualMode.LineWithMarker;

        pathfinder.DrawPath();
    }
    public void ChooseStart(int index)
    {
        EnsurePathfinder();
        PlayerPrefs.SetInt("SavedStartDropdown", index);
        PlayerPrefs.Save();
        if (index == 0)
        {
            chosenStart = null;
            pathfinder.ClearPath();
            return;
        }

        if (index == 1)
        {
            APIRoute route = pathfinder.GetCurrentRouteFromAPI();

            if (route == null)
                return;

            chosenStart = pathfinder.FindNodeData(
                route.startNode,
                route.startScene
            );

            if (chosenStart == null)
            {
                Debug.LogError("Nie znaleziono START z planu");
                return;
            }

            Debug.Log("Start ustawiony z planu: " + route.startNode);

            TryUpdatePath();
            return;
        }

        chosenStart = allNodes[index - 2];
        Debug.Log("Wybrano start: " + chosenStart.nodeName);
        PlayerPrefs.DeleteKey("IsMultiFloor");
        PlayerPrefs.DeleteKey("NextStartNode");
        PlayerPrefs.DeleteKey("SceneTransition");
        TryUpdatePath();
    }

    // Wywoływane przez End Dropdown (OnValueChanged)
    public void ChooseEnd(int index)
    {
        EnsurePathfinder();
        if (index == 0)
        {
            // brak wyboru
            chosenEnd = null;
            useEmergencyExit = false;
            pathfinder.ClearPath();
            return;
        }

        if (index == 1)
        {
            useEmergencyExit = false;

            APIRoute route = pathfinder.GetCurrentRouteFromAPI();

            if (route == null)
                return;

            chosenEnd = pathfinder.FindNodeData(
                route.endNode,
                route.endScene
            );

            if (chosenEnd == null)
            {
                Debug.LogError("Nie znaleziono END z planu");
                return;
            }

            Debug.Log("End ustawiony z planu: " + route.endNode);

            TryUpdatePath();
            return;
        }

        if (index == 2)
        {
            // WYJŚCIE AWARYJNE
            useEmergencyExit = true;
            chosenEnd = null; // ALE to już nie znaczy "brak wyboru"

            Debug.Log("Wyjście awaryjne");

            TryUpdatePath();
            return;
        }

        // normalny node
        useEmergencyExit = false;
        chosenEnd = allNodes[index - 3];
        PlayerPrefs.SetString("SavedUIEndNode", chosenEnd.nodeName);
        Debug.Log("Wybrano cel: " + chosenEnd.nodeName);

        TryUpdatePath();
        PlayerPrefs.SetInt("SavedEndDropdown", index);
        PlayerPrefs.Save();
    }
    void TryUpdatePath()
    {
        EnsurePathfinder();
        FloorChangeUI ui = FindObjectOfType<FloorChangeUI>();
        if (chosenStart == null)
        {
            pathfinder.ClearPath();
            return;
        }

        Node startNodeReal = pathfinder.FindNodeByName(chosenStart.nodeName, chosenStart.sceneName);

        if (startNodeReal == null)
        {
            Debug.LogWarning("Start node nie istnieje w tej scenie!");
            pathfinder.ClearPath();
            return;
        }

        string currentScene = startNodeReal.sceneName;

        Node finalTarget = null;

        if (useEmergencyExit)
        {
            Node exit = startNodeReal.emergencyExit;
            PlayerPrefs.SetString("SavedRealStartNode", startNodeReal.name);

            if (exit == null)
            {
                Debug.LogWarning("Brak emergencyExit w tym Node!");
                pathfinder.ClearPath();
                return;
            }

            pathfinder.startNode = startNodeReal;
            pathfinder.targetNode = exit;

            pathfinder.DrawPath();
            return;
        }
        else
        {
            if (chosenEnd == null)
            {
                pathfinder.ClearPath();
                return;
            }

            //  SPRAWDZENIE CZY INNA SCENA
            NodeData endNodeData = chosenEnd;

            if (endNodeData == null)
            {
                Debug.LogWarning("Brak danych końcowych (NodeData)");
                pathfinder.ClearPath();
                return;
            }

            bool sameBuilding = startNodeReal.buildingID == endNodeData.buildingID;
            bool sameScene = string.Equals(
            startNodeReal.sceneName,
            endNodeData.sceneName,
            System.StringComparison.OrdinalIgnoreCase);

            if (!sameScene || !sameBuilding)
            {
                Node transitionNode;

                if (!sameBuilding)
                {
                    transitionNode = startNodeReal.emergencyExit;
                    PlayerPrefs.SetString("TransitionType", "Building");
                }
                else
                {
                    transitionNode = pathfinder.FindBestStairs(
                        startNodeReal,
                        endNodeData.sceneName
                    );

                    PlayerPrefs.SetString("TransitionType", "Stairs");
                    PlayerPrefs.SetString("TransitionStaircaseID", transitionNode.staircaseID);
                }

                if (transitionNode == null)
                {
                    Debug.LogWarning("Brak node przejściowego!");
                    pathfinder.ClearPath();
                    return;
                }

                pathfinder.startNode = startNodeReal;
                pathfinder.targetNode = transitionNode;
                pathfinder.DrawPath();

                ui = FindObjectOfType<FloorChangeUI>();
                if (ui != null)
                {
                    ui.Show(endNodeData.sceneName, transitionNode.name);
                }

                FloorConnection connection = transitionNode.connections
                .Find(c => c.targetScene == endNodeData.sceneName);

                if (connection != null)
                {
                    PlayerPrefs.SetString("NextStartNode", connection.targetNodeName);
                    PlayerPrefs.SetString("NextStartScene", connection.targetScene);
                }
                else
                {
                    Debug.LogWarning("Brak connection dla schodów!");
                }
                PlayerPrefs.SetString("SavedUIStartNode", chosenStart.nodeName);
                PlayerPrefs.SetString("FinalTargetNode", endNodeData.nodeName);
                PlayerPrefs.SetString("FinalTargetScene", endNodeData.sceneName);
                PlayerPrefs.SetInt("IsMultiFloor", 1);
                PlayerPrefs.SetInt("SceneTransition", 1);

                PlayerPrefs.Save();

                return;
            }
            // normalny przypadek (ta sama scena)
            finalTarget = pathfinder.FindNodeByName(
                endNodeData.nodeName,
                endNodeData.sceneName
            );

            if (finalTarget == null)
            {
                Debug.LogWarning($"Target node NIE ISTNIEJE: {endNodeData.nodeName} w scenie {endNodeData.sceneName}");

                // KLUCZOWA ZMIANA: nie przerywamy całego flow
                // tylko kończymy aktualizację ścieżki
                pathfinder.ClearPath();
                return;
            }
        }

        if (finalTarget == null || startNodeReal == finalTarget)
        {
            pathfinder.ClearPath();
            return;
        }

        pathfinder.startNode = startNodeReal;
        pathfinder.targetNode = finalTarget;
        pathfinder.DrawPath();
        ui = FindObjectOfType<FloorChangeUI>();
        if (ui != null)
        {
            ui.UpdateVisibility(pathfinder.startNode, pathfinder.targetNode);
        }
    }
    public void SyncWithPathfinder()
    {

    }
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Canvas[] canvases = FindObjectsOfType<Canvas>(true);

        foreach (Canvas c in canvases)
        {
            if (c.transform.root.gameObject != transform.root.gameObject)
            {
                Destroy(c.transform.root.gameObject);
            }
        }

        pathfinder = FindObjectOfType<SimplePathfinder>();

        if (pathfinder == null)
            return;

        // START = node zapisany podczas przejścia między piętrami
        string transitionType = PlayerPrefs.GetString("TransitionType", "");

        Node nextStart = null;

        if (transitionType == "Building")
        {
            nextStart = FindClosestEntranceNode();
        }
        else if (transitionType == "Stairs")
        {
            string staircaseID = PlayerPrefs.GetString("TransitionStaircaseID", "");
            nextStart = pathfinder.FindStairsByID(staircaseID);
        }
        else
        {
            string nextStartName = PlayerPrefs.GetString("NextStartNode", "");
            string nextStartScene = PlayerPrefs.GetString("NextStartScene", "");

            if (!string.IsNullOrEmpty(nextStartName))
            {
                nextStart = pathfinder.FindNodeByName(nextStartName, nextStartScene);
            }
        }

        if (nextStart != null)
        {
            pathfinder.startNode = nextStart;
        }

        // END = finalny cel użytkownika
        string finalTarget = PlayerPrefs.GetString("FinalTargetNode", "");
        string finalTargetScene = PlayerPrefs.GetString("FinalTargetScene", "");

        if (!string.IsNullOrEmpty(finalTarget))
        {
            Node target = pathfinder.FindNodeByName(finalTarget, finalTargetScene);

            if (target != null)
            {
                pathfinder.targetNode = target;
            }
        }

        pathfinder.DrawPath();
        FloorChangeUI ui = FindObjectOfType<FloorChangeUI>();
        if (ui != null)
        {
            ui.UpdateVisibility(pathfinder.startNode, pathfinder.targetNode);
        }
    }
    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
    void EnsurePathfinder()
    {
        if (pathfinder == null || pathfinder.Equals(null))
        {
            pathfinder = FindObjectOfType<SimplePathfinder>();

            if (pathfinder == null)
            {
                Debug.LogWarning("Brak SimplePathfinder w scenie!");
            }
        }
    }
    Node FindClosestEntranceNode()
    {
        Node[] nodes = FindObjectsOfType<Node>();

        Node best = null;
        float bestDist = Mathf.Infinity;

        foreach (var n in nodes)
        {
            if (!n.name.EndsWith("_o"))
                continue;

            float d = Vector3.Distance(Vector3.zero, n.transform.position);

            if (d < bestDist)
            {
                bestDist = d;
                best = n;
            }
        }

        return best;
    }
}
