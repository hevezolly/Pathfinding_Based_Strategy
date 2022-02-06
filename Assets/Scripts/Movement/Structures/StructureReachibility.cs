using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public static class StructureReachibility
{
    private static Navigator _nav;

    public static void SetNavigator(Navigator navigator)
    {
        _nav = navigator;
    }

    private static bool CorrectIntersection(Structure structure, Vector2 point, Navigator nav)
    {
        return structure.Blueprint.GetOccupiedCords(point).All(c =>
            (!nav.OccupiedCoordinates.ContainsKey(c) || nav.OccupiedCoordinates[c].Structure == structure) &&
            (!nav.ReservedCoordinates.ContainsKey(c) || ReferenceEquals(nav.ReservedCoordinates[c], structure)));
    }

    public static bool CanTraverse(this Structure structure, Vector2 point, Navigator navigator = null)
    {
        var nav = _nav;
        if (navigator != null)
            nav = navigator;
        return structure.Blueprint.GetNeighbours(point).Any(n => nav.OccupiedCoordinates.ContainsKey(n)) &&
            CorrectIntersection(structure, point, nav);
    }

    public static bool CanReach(this Structure structure, Vector2 point, Navigator navigator = null)
    {
        var nav = _nav;
        if (navigator != null)
            nav = navigator;
        return structure.Blueprint.GetNeighbours(point).Any(n => 
        (nav.OccupiedCoordinates.ContainsKey(n) && nav.OccupiedCoordinates[n].Structure != structure) || 
        (nav.ReservedCoordinates.ContainsKey(n) && !ReferenceEquals(nav.ReservedCoordinates[n], structure))) &&
            CorrectIntersection(structure, point, nav);
    }
}
