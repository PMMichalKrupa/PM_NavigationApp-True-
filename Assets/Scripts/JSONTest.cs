using UnityEngine;

public class JSONTest : MonoBehaviour
{
    void Start()
    {
        string json =
        "{\"routes\":[{\"routeName\":\"TestRoute\",\"startNode\":\"A\",\"endNode\":\"B\",\"startHour\":1,\"endHour\":2}]}";

        APIRouteList data = JsonUtility.FromJson<APIRouteList>(json);

        Debug.Log(data.routes[0].routeName);
    }
}
