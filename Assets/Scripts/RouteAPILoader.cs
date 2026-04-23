using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class RouteAPILoader : MonoBehaviour
{
    public string apiURL = "https://twoje-api.pl/routes";
    public SimplePathfinder pathfinder;

    public void LoadRoutesFromAPI()
    {
        StartCoroutine(GetRoutes());
    }

    IEnumerator GetRoutes()
    {
        UnityWebRequest req = UnityWebRequest.Get(apiURL);
        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("API error: " + req.error);
            yield break;
        }

        string json = req.downloadHandler.text;

        APIRouteList data = JsonUtility.FromJson<APIRouteList>(json);

        if (data == null || data.routes == null)
        {
            Debug.LogError("Nieprawid³owy JSON");
            yield break;
        }

        pathfinder.LoadRoutesFromAPI(data.routes);
    }
}