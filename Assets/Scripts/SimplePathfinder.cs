using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class SimplePathfinder : MonoBehaviour
{
    public Node startNode; //Przechowuje node od którego rozpoczyna się trasa
    public Node targetNode; //Przechowuje node na którym kończy się trasa
    public PathMarker marker;
    public LineRenderer lineRenderer;
    private List<LineRenderer> extraLines = new List<LineRenderer>();
    public Node ResolveExit(Node start, Node target)
    {
        if (start.buildingID == target.buildingID)
            return FindNearestStairs(start, start.buildingID);

        return start.emergencyExit;
    }
    public bool autoLoadOnStart = true;
    APIRoute[] apiRoutes;
    public enum PathSourceMode
    {
        Manual,
        Database
    }
    public PathSourceMode sourceMode;
    public enum PathVisualMode
    {
        LineWithMarker,
        ThickLine
    }

    public PathVisualMode visualMode;
    //public RouteDatabase database;
    bool IsDifferentScene(Node a, Node b)
    {
        if (a == null || b == null) return false;

        return a.gameObject.scene.name != b.gameObject.scene.name;
    }
    Node FindNearestStairs(Node start)
    {
        Node[] all = FindObjectsOfType<Node>();

        Node best = null;
        float bestDist = Mathf.Infinity;

        foreach (var n in all)
        {
            if (!n.isStairs) continue;

            float d = Vector3.Distance(start.transform.position, n.transform.position);

            if (d < bestDist)
            {
                bestDist = d;
                best = n;
            }
        }

        return best;
    }
    public Node FindBestStairs(Node start, string targetScene)
    {
        Node[] all = FindObjectsOfType<Node>();

        List<Node> validStairs = new List<Node>();

        foreach (var n in all)
        {
            if (!n.isStairs) continue;

            foreach (var c in n.connections)
            {
                if (c.targetScene == targetScene)
                {
                    validStairs.Add(n);
                    break;
                }
            }
        }

        // fallback jeśli brak bezpośrednich połączeń
        if (validStairs.Count == 0)
            return FindNearestStairs(start);

        Node best = null;
        float bestDist = Mathf.Infinity;

        foreach (var s in validStairs)
        {
            float d = Vector3.Distance(start.transform.position, s.transform.position);
            if (d < bestDist)
            {
                bestDist = d;
                best = s;
            }
        }

        return best;
    }

    void Start()
    {
        if (PlayerPrefs.HasKey("NextStartNode"))
{
    string startName = PlayerPrefs.GetString("NextStartNode");
    string startScene = PlayerPrefs.GetString("NextStartScene", "");

    startNode = FindNodeByName(startName, startScene);
}
        if (PlayerPrefs.HasKey("NextScene"))
        {
            string scene = PlayerPrefs.GetString("NextScene");
            PlayerPrefs.DeleteKey("NextScene");

            UnityEngine.SceneManagement.SceneManager.LoadScene(scene);
            return;
        }
        if (!PlayerPrefs.HasKey("SceneTransition") && PlayerPrefs.HasKey("SavedRealStartNode"))
        {
            string realStart = PlayerPrefs.GetString("SavedRealStartNode");
            startNode = FindNodeByName(realStart);
        }
        string finalTargetName = null;

        if (PlayerPrefs.HasKey("FinalTargetNode"))
        {
            finalTargetName = PlayerPrefs.GetString("FinalTargetNode");
            targetNode = FindNodeByName(finalTargetName);
        }
        bool isMulti = PlayerPrefs.GetInt("IsMultiFloor", 0) == 1;

        if (isMulti && startNode != null && !string.IsNullOrEmpty(finalTargetName))
            {
            Debug.Log("Kontynuuję trasę między piętrami");

            if (IsDifferentScene(startNode, targetNode))
            {
                HandleMultiFloorPath();
                return;
            }
            else
            {
                // dotarliśmy na właściwe piętro
                PlayerPrefs.DeleteKey("IsMultiFloor");
            }
        }

        if (lineRenderer == null)
            lineRenderer = GetComponent<LineRenderer>();

        if (lineRenderer == null)
        {
            lineRenderer = gameObject.AddComponent<LineRenderer>();
            lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            lineRenderer.startColor = Color.red;
            lineRenderer.endColor = Color.red;
            if (visualMode == PathVisualMode.LineWithMarker)
            {
                lineRenderer.startWidth = 0.1f;
                lineRenderer.endWidth = 0.1f;
            }
            else if (visualMode == PathVisualMode.ThickLine)
            {
                lineRenderer.startWidth = 0.5f;
                lineRenderer.endWidth = 0.5f;
            }
            lineRenderer.positionCount = 0;
        }
        if (startNode != null && targetNode != null)
        {
            DrawPath();
        }

    }
    void EnsureLineRenderer()
    {
        if (lineRenderer != null)
            return;

        lineRenderer = GetComponent<LineRenderer>();

        if (lineRenderer == null)
        {
            lineRenderer = gameObject.AddComponent<LineRenderer>();
            lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            lineRenderer.startColor = Color.red;
            lineRenderer.endColor = Color.red;
            lineRenderer.startWidth = 0.1f;
            lineRenderer.endWidth = 0.1f;
            lineRenderer.positionCount = 0;
        }
    }

    public void ClearPath()
    {
        EnsureLineRenderer();
        lineRenderer.positionCount = 0;

        if (marker != null)
        {
            marker.gameObject.SetActive(false);
        }
    }
    public void HandleMultiFloorPath()
    {
        if (visualMode == PathVisualMode.LineWithMarker)
        {
            lineRenderer.startWidth = 0.1f;
            lineRenderer.endWidth = 0.1f;
        }
        else
        {
            lineRenderer.startWidth = 0.5f;
            lineRenderer.endWidth = 0.5f;
        }

        EnsureLineRenderer();

        string finalTargetScene = GetTargetSceneName();

        //  Wyznacz następne pośrednie piętro
        string nextScene = GetNextSceneTowardsTarget(startNode.sceneName, finalTargetScene);

        if (string.IsNullOrEmpty(nextScene))
        {
            Debug.LogError("Nie udało się znaleźć następnego pośredniego piętra!");
            return;
        }

        //  Znajdź klatkę schodową do nextScene
        Node stairsToNext = FindBestStairs(startNode, nextScene);
        if (stairsToNext == null)
        {
            Debug.LogError("Brak klatki schodowej do nextScene!");
            return;
        }

        //  Znajdź ścieżkę startNode -> klatka schodowa
        List<Node> pathToStairs = FindPath(startNode, stairsToNext);
        if (pathToStairs == null || pathToStairs.Count == 0)
        {
            Debug.LogWarning("Brak ścieżki do klatki schodowej!");
            return;
        }

        //  Rysujemy trasę od startNode do klatki schodowej
        DrawLine(pathToStairs);

        //  Marker
        if (marker != null)
        {
            if (visualMode == PathVisualMode.LineWithMarker)
            {
                marker.gameObject.SetActive(true);
                marker.StartMoving(lineRenderer, pathToStairs);
            }
            else
            {
                marker.gameObject.SetActive(false);
            }
        }

        //  Zapisujemy kolejny start od klatki schodowej
        FloorConnection connection = stairsToNext.connections.Find(c => c.targetScene == nextScene);
        if (connection == null && stairsToNext.connections.Count > 0)
            connection = stairsToNext.connections[0];

        if (connection != null)
        {
            PlayerPrefs.SetString("NextStartNode", connection.targetNodeName);
            PlayerPrefs.SetString("NextStartScene", connection.targetScene);
        }

        //  Zapisujemy informacje o finalnym celu
        if (targetNode != null)
            PlayerPrefs.SetString("FinalTargetNode", targetNode.name);
        PlayerPrefs.SetInt("IsMultiFloor", 1);
        PlayerPrefs.SetString("FinalTargetScene", finalTargetScene);

        //  UI zmiany piętra
        FloorChangeUI ui = FindObjectOfType<FloorChangeUI>();
        if (ui != null)
            ui.Show(nextScene, connection != null ? connection.targetNodeName : stairsToNext.name);
    }
    void DrawLine(List<Node> path)
    {
        ClearExtraLines();

        List<List<Node>> segments = SplitPathByGhosts(path);

        for (int s = 0; s < segments.Count; s++)
        {
            LineRenderer lr = (s == 0) ? lineRenderer : CreateExtraLine();

            List<Node> segment = segments[s];

            lr.positionCount = segment.Count;

            for (int i = 0; i < segment.Count; i++)
            {
                lr.SetPosition(i,
                    segment[i].transform.position + Vector3.up * 0.2f);
            }
        }
    }
    string GetTargetSceneName()
    {
        if (targetNode != null)
            return targetNode.sceneName;

        if (PlayerPrefs.HasKey("FinalTargetScene"))
            return PlayerPrefs.GetString("FinalTargetScene");

        if (PlayerPrefs.HasKey("FinalTargetNode"))
        {
            string name = PlayerPrefs.GetString("FinalTargetNode");
            Node n = FindNodeByName(name);
            if (n != null)
                return n.sceneName;
        }

        return null;
    }
    // Wywoływane z UI
    public void DrawPath()
    {
        if (startNode == null || targetNode == null)
        {
            ClearPath();
            return;
        }

        EnsureLineRenderer();

        if (IsDifferentScene(startNode, targetNode))
        {
            HandleMultiFloorPath();
            return;
        }
        if (visualMode == PathVisualMode.LineWithMarker)
        {
            lineRenderer.startWidth = 0.1f;
            lineRenderer.endWidth = 0.1f;
        }
        else
        {
            lineRenderer.startWidth = 0.5f;
            lineRenderer.endWidth = 0.5f;
        }

        List<Node> path = FindPath(startNode, targetNode);

        if (path == null || path.Count == 0)
        {
            Debug.LogWarning("Brak ścieżki!");
            lineRenderer.positionCount = 0;
            return;
        }

        DrawLine(path);

        // Kulka / marker

        if (marker != null)
        {
            if (visualMode == PathVisualMode.LineWithMarker)
            {
                marker.gameObject.SetActive(true);
                marker.StartMoving(lineRenderer, path);
            }
            else
            {
                marker.gameObject.SetActive(false);
            }
        }
    }

    // BFS
    List<Node> FindPath(Node start, Node goal)
    {
        Queue<Node> frontier = new Queue<Node>();
        frontier.Enqueue(start);

        Dictionary<Node, Node> cameFrom = new Dictionary<Node, Node>();
        cameFrom[start] = null;

        while (frontier.Count > 0)
        {
            Node current = frontier.Dequeue();

            if (current == goal)
                break;

            foreach (Node next in current.neighbors)
            {
                if (next == null)
                {
                    Debug.LogWarning($"Node {current.name} ma NULL w neighbors!");
                    continue;
                }

                if (!cameFrom.ContainsKey(next))
                {
                    frontier.Enqueue(next);
                    cameFrom[next] = current;
                }
            }

        }

        if (!cameFrom.ContainsKey(goal))
            return null;

        // Odtwarzanie ścieżki
        List<Node> path = new List<Node>();
        Node currentNode = goal;

        while (currentNode != null)
        {
            path.Insert(0, currentNode);
            cameFrom.TryGetValue(currentNode, out currentNode);
        }

        return path;
    }
    public Node FindNodeByName(string nodeName, string sceneName = null)
    {
        if (string.IsNullOrEmpty(nodeName))
            return null;

        Node[] nodes = FindObjectsOfType<Node>();

        foreach (var n in nodes)
        {
            if (n.name != nodeName)
                continue;

            if (!string.IsNullOrEmpty(sceneName) && n.sceneName != sceneName)
                continue;

            return n;
        }

        //Debug.LogWarning($"Node nie znaleziony: {nodeName} ({sceneName})");
        return null;
    }
    public APIRoute LoadRouteFromAPIByTime()
    {
        if (apiRoutes == null || apiRoutes.Length == 0)
        {
            Debug.LogWarning("Brak tras z API");
            return null;
        }

        int current = TimeParser.CurrentMinutes();
        APIRoute selected = null;

        // wybór trasy dla aktualnej godziny
        foreach (var r in apiRoutes)
        {
            int start = TimeParser.ToMinutes(r.startTime);
            int end = TimeParser.ToMinutes(r.endTime);

            if (current >= start && current < end)
            {
                selected = r;
                break;
            }
        }

        if (selected == null)
        {
            Debug.LogWarning("Brak trasy dla tej godziny");
            return null;
        }

        // Szukanie node'ów uwzględniając scenę
        if (PlayerPrefs.HasKey("NextStartNode") && PlayerPrefs.HasKey("NextStartScene"))
        {
            string savedStartName = PlayerPrefs.GetString("NextStartNode");
            string savedStartScene = PlayerPrefs.GetString("NextStartScene");

            startNode = FindNodeByName(savedStartName, savedStartScene);
        }
        else
        {
            startNode = FindNodeByName(selected.startNode, selected.startScene);
        }
        if (selected.startScene == selected.endScene)
        {
            targetNode = FindNodeByName(selected.endNode, selected.endScene);
        }
        else
        {
            targetNode = null; // ustawimy później przez system multi-floor
        }

        if (startNode == null)
        {
            Debug.LogWarning($"StartNode poza sceną: {selected.startNode} ({selected.startScene})");
        }

        if (selected.startScene == selected.endScene && targetNode == null)
        {
            Debug.LogError($"Nie znaleziono targetNode {selected.endNode} w scenie {selected.endScene}");
            return null;
        }

        DrawPath();

        UIPathSelector ui = FindObjectOfType<UIPathSelector>();
        if (ui != null)
            ui.SyncWithPathfinder();
        return selected;
    }
    public void LoadRoutesFromAPI(APIRoute[] routes)
    {
        apiRoutes = routes;
        Debug.Log("Załadowano trasy z API: " + routes.Length);
    }
    string GetNextSceneTowardsTarget(string currentScene, string targetScene)
    {
        Node[] allNodes = FindObjectsOfType<Node>();

        // 1. Zbuduj graf scen (połączenia między piętrami)
        Dictionary<string, List<string>> sceneGraph = new Dictionary<string, List<string>>();

        foreach (var node in allNodes)
        {
            if (!node.isStairs) continue;

            if (!sceneGraph.ContainsKey(node.sceneName))
                sceneGraph[node.sceneName] = new List<string>();

            foreach (var conn in node.connections)
            {
                // dodaj połączenie w jedną stronę
                if (!sceneGraph[node.sceneName].Contains(conn.targetScene))
                    sceneGraph[node.sceneName].Add(conn.targetScene);

                // dodaj połączenie w drugą stronę (symetrycznie)
                if (!sceneGraph.ContainsKey(conn.targetScene))
                    sceneGraph[conn.targetScene] = new List<string>();

                if (!sceneGraph[conn.targetScene].Contains(node.sceneName))
                    sceneGraph[conn.targetScene].Add(node.sceneName);
            }
        }

        // 2. BFS od currentScene do targetScene
        Queue<string> queue = new Queue<string>();
        Dictionary<string, string> cameFrom = new Dictionary<string, string>();

        queue.Enqueue(currentScene);
        cameFrom[currentScene] = null;

        while (queue.Count > 0)
        {
            string scene = queue.Dequeue();

            if (scene == targetScene)
                break;

            if (!sceneGraph.ContainsKey(scene))
                continue;

            foreach (var next in sceneGraph[scene])
            {
                if (!cameFrom.ContainsKey(next))
                {
                    queue.Enqueue(next);
                    cameFrom[next] = scene;
                }
            }
        }

        if (!cameFrom.ContainsKey(targetScene))
        {
            Debug.LogError($"Brak połączenia między piętrami: {currentScene} -> {targetScene}");
            return null;
        }

        // 3. Cofnij się, aby znaleźć pierwszy krok
        string step = targetScene;

        // Sprawdzenie, czy targetScene w ogóle zostało odwiedzone
        if (!cameFrom.ContainsKey(step))
        {
            Debug.LogError($"Nie znaleziono targetScene w grafie: {targetScene}");
            return null;
        }

        // Cofamy się aż do node’a bezpośrednio połączonego z currentScene
        while (cameFrom[step] != null && cameFrom[step] != currentScene)
        {
            step = cameFrom[step];
        }

        // Jeśli nie znaleziono bezpośredniego połączenia z currentScene
        if (cameFrom[step] == null)
        {
            Debug.LogError($"Nie udało się znaleźć pierwszego kroku do {targetScene} z {currentScene}");
            return null;
        }

        return step;
    }
    Node FindNearestStairs(Node start, string buildingID)
    {
        Node[] all = FindObjectsOfType<Node>();

        Node best = null;
        float bestDist = Mathf.Infinity;

        foreach (var n in all)
        {
            if (!n.isStairs) continue;
            if (n.buildingID != buildingID) continue;

            float d = Vector3.Distance(start.transform.position, n.transform.position);

            if (d < bestDist)
            {
                bestDist = d;
                best = n;
            }
        }

        return best;
    }
    public NodeData FindNodeData(string nodeName, string sceneName)
    {
        NodeDatabase db = Resources.Load<NodeDatabase>("NodeDatabase");

        return db.nodes.Find(n =>
            n.nodeName == nodeName &&
            n.sceneName == sceneName
        );
    }
    public APIRoute GetCurrentRouteFromAPI()
    {
        if (apiRoutes == null || apiRoutes.Length == 0)
        {
            Debug.LogWarning("Brak tras z API");
            return null;
        }

        int current = TimeParser.CurrentMinutes();

        foreach (var r in apiRoutes)
        {
            int start = TimeParser.ToMinutes(r.startTime);
            int end = TimeParser.ToMinutes(r.endTime);

            if (current >= start && current < end)
            {
                return r;
            }
        }

        Debug.LogWarning("Brak trasy dla tej godziny");
        return null;
    }
    public Node FindStairsByID(string staircaseID)
    {
        Node[] nodes = FindObjectsOfType<Node>();

        foreach (var n in nodes)
        {
            if (n.isStairs && n.staircaseID == staircaseID)
                return n;
        }

        return null;
    }
    List<List<Node>> SplitPathByGhosts(List<Node> path)
    {
        List<List<Node>> segments = new List<List<Node>>();
        List<Node> currentSegment = new List<Node>();

        currentSegment.Add(path[0]);

        for (int i = 1; i < path.Count; i++)
        {
            Node previous = path[i - 1];
            Node current = path[i];

            bool isGhost = previous.ghostNeighbors.Contains(current);

            if (isGhost)
            {
                segments.Add(new List<Node>(currentSegment));
                currentSegment.Clear();
            }

            currentSegment.Add(current);
        }

        if (currentSegment.Count > 0)
            segments.Add(currentSegment);

        return segments;
    }
    LineRenderer CreateExtraLine()
    {
        GameObject obj = new GameObject("PathSegment");
        obj.transform.SetParent(transform);

        LineRenderer lr = obj.AddComponent<LineRenderer>();

        lr.material = lineRenderer.material;
        lr.startWidth = lineRenderer.startWidth;
        lr.endWidth = lineRenderer.endWidth;
        lr.startColor = lineRenderer.startColor;
        lr.endColor = lineRenderer.endColor;

        extraLines.Add(lr);

        return lr;
    }
    void ClearExtraLines()
    {
        foreach (var lr in extraLines)
        {
            if (lr != null)
                Destroy(lr.gameObject);
        }

        extraLines.Clear();
    }
}
