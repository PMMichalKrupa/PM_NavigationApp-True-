using UnityEngine;

public static class RouteLoader
{
    public static APIRoute GetRouteFromJson()
    {
        TextAsset jsonFile = Resources.Load<TextAsset>("routes");

        if (jsonFile == null)
        {
            Debug.LogError("Brak routes.json w Resources!");
            return null;
        }

        RouteWrapper wrapper = JsonUtility.FromJson<RouteWrapper>(jsonFile.text);

        if (wrapper.routes == null || wrapper.routes.Length == 0)
            return null;

        int current = TimeParser.CurrentMinutes();

        foreach (var r in wrapper.routes)
        {
            int start = TimeParser.ToMinutes(r.startTime);
            int end = TimeParser.ToMinutes(r.endTime);

            if (current >= start && current < end)
                return r;
        }

        return null;
    }
}

[System.Serializable]
public class RouteWrapper
{
    public APIRoute[] routes;
}