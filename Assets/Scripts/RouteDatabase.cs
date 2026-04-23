using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "Routing/Route Database")]
public class RouteDatabase : ScriptableObject
{
    public List<RouteData> routes = new List<RouteData>();
}