using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Pathfinding;
using System.Linq;

public class BotsTraversal : ITraversalProvider
{

    private HashSet<Vector2Int> occupiedPlaces;

    public BotsTraversal(HashSet<Vector2Int> occupiedPlaces)
    {
        this.occupiedPlaces = occupiedPlaces;
    }
    public bool CanTraverse(Path path, GraphNode node)
    {
        var pos = Vector2Int.RoundToInt((Vector3)node.position);
        return occupiedPlaces.Contains(pos); //|| pos.GetNeighbours().Any(n => occupiedPlaces.Contains(n));
    }

    public uint GetTraversalCost(Path path, GraphNode node)
    {
        return DefaultITraversalProvider.GetTraversalCost(path, node);
    }
}
