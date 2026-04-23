using System.IO;
using UnityEngine;

public class LocalRouteLoader : MonoBehaviour
{
    public SimplePathfinder pathfinder;

    void Start()
    {
        LoadLocalRoutes();
    }

    public void LoadLocalRoutes()
    {
        string path = Path.Combine(Application.streamingAssetsPath, "routes.json");

        Debug.Log("Œcie¿ka: " + path);

        if (!File.Exists(path))
        {
            Debug.LogError("Brak pliku routes.json");
            return;
        }

        string json = File.ReadAllText(path);
        Debug.Log("JSON RAW: " + json);

        APIRouteList data = JsonUtility.FromJson<APIRouteList>(json);

        Debug.Log("Parsed routes: " + (data.routes == null ? "NULL" : data.routes.Length.ToString()));

        if (data == null || data.routes == null)
        {
            Debug.LogError("JSON niepoprawny");
            return;
        }

        pathfinder.LoadRoutesFromAPI(data.routes);
        pathfinder.LoadRouteFromAPIByTime();
    }
}