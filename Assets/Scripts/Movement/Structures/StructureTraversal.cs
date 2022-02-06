using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Pathfinding;
using System.Linq;

public class StructureTraversal : ITraversalProvider
{
    private Structure structure;
    private Navigator navigator;

    public StructureTraversal(Structure blueprint, Navigator navigator)
    {
        this.structure = blueprint;
        this.navigator = navigator;
    }

    public bool CanTraverse(Path path, GraphNode node)
    {
        var pos = (Vector2)(Vector3)node.position;
        return structure.CanTraverse(pos, navigator);
    }
    public uint GetTraversalCost(Path path, GraphNode node)
    {
        var pos = (Vector2)(Vector3)node.position;
        var hasSideNaighbours = structure.Blueprint.GetFourNeighbours(pos)
            .Any(n => navigator.OccupiedCoordinates.ContainsKey(n));
        return hasSideNaighbours ? DefaultITraversalProvider.GetTraversalCost(path, node) :
            50;
    }
}
