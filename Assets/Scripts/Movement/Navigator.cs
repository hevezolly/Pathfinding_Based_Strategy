using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Pathfinding;
using System.Linq;
using UnityEngine.Events;

[CreateAssetMenu(fileName = "new Navigator", menuName = "Navigator", order = 0)]
public class Navigator : ScriptableObject
{

    [SerializeField]
    private uint BotTravelCost;
    [SerializeField]
    private uint NonBotTravelCost;

    private Queue<PointNode> unusedNodes;

    private Dictionary<Vector2Int, PointNode> nodes;

    private Dictionary<Vector2Int, IReserverEntity> reservedDestinations;
    public Dictionary<Vector2Int, IReserverEntity> ReservedCoordinates => reservedDestinations;

    private Dictionary<Vector2Int, Occupant> _filledCells;
    public Dictionary<Vector2Int, Occupant> OccupiedCoordinates => _filledCells;

    private PointGraph graph;

    private GridGraph gridGraph;

    public ParametrisedEvent<FieldChangeData> AddNodesAtEvent;

    public ParametrisedEvent<FieldChangeData> RemoveNodesAtEvent;

    public ParametrisedEvent<BotChangeData> BotsPositionsChanged;

    public void Initiate(BotController baseBot)
    {
        unusedNodes = new Queue<PointNode>();
        _filledCells = new Dictionary<Vector2Int, Occupant>();
        nodes = new Dictionary<Vector2Int, PointNode>();
        graph = AstarPath.active.data.pointGraph;
        gridGraph = AstarPath.active.data.gridGraph;
        PlaceBot(Vector2.zero, baseBot);
        AstarPath.active.FlushWorkItems();
        reservedDestinations = new Dictionary<Vector2Int, IReserverEntity>();
        StructureReachibility.SetNavigator(this);
    }

    public Vector2Int? GetClosestCell(Vector2 position, bool traversable = false)
    {
        Vector2Int? closest = null;
        var distance = float.MaxValue;
        var iter = graph.nodes
            .Where(n => n != null)
            .Select(n => ((Vector2)(Vector3)n.position).ToCellCord());
        if (!traversable)
            iter = iter.Union(reservedDestinations.Keys
                .Where(k => reservedDestinations[k].IsSingleBot)
                .SelectMany(r => r.GetNeighbours()));
        foreach (var cord in iter)
        {
            if (OccupiedCoordinates.ContainsKey(cord) || reservedDestinations.ContainsKey(cord))
                continue;
            var newDist = Vector2.Distance(cord.ToWorldPosition(), position);
            if (newDist < distance)
            {
                distance = newDist;
                closest = cord;
            }
        }
        return closest;
    }

    public Nearest? GetNearestCellsForStructure(Structure structure, Vector2 initial)
    {
        var nearestNodeCord = GetClosestCell(initial, true);
        
        var startCord = structure.Blueprint.GetCenterCord(initial);
        var offset = Vector2Int.zero;
        var SearchArea = 15;
        var delta = new Vector2Int(0, -1);
        int maxI = SearchArea * SearchArea;
        var reachable = Vector2.zero;
        var traversable = Vector2.zero;
        var reachableFound = false;
        var traversableFound = false;
        for (int i = 0; i < maxI; i++)
        {
                
            var position = structure.Blueprint.GerCenterPosFromCenterCord(startCord + offset);
            if (!reachableFound && structure.CanReach(position, this))
            {
                reachableFound = true;
                reachable = position;
            }
            if (!traversableFound && structure.CanTraverse(position, this))
            {
                traversableFound = true;
                traversable = position;
            }
            if (traversableFound && reachableFound)
                break;

            if ((offset.x == offset.y) || 
                ((offset.x < 0) && (offset.x == -offset.y)) || 
                ((offset.x > 0) && (offset.x == 1 - offset.y)))
            {
                var temp = delta.x;
                delta.x = -delta.y;
                delta.y = temp;
            }
            offset += delta;
        }
        if (traversableFound && reachableFound)
        {
            return new Nearest() { reachable = reachable, traversable = traversable };
        }
        return null;
    }

    public void ReserveCell(Vector2Int cell, IReserverEntity reserver)
    {
        if (!reservedDestinations.ContainsKey(cell))
            reservedDestinations.Add(cell, reserver);
    }

    public void ReleaseCell(Vector2Int cell)
    {
        if (reservedDestinations.ContainsKey(cell))
            reservedDestinations.Remove(cell);
    }

    private void DeleteNode(PointNode node)
    {
        node.Walkable = false;
        node.position = new Int3(new Vector3(float.MaxValue, float.MaxValue, float.MaxValue));
        unusedNodes.Enqueue(node);
    }

    public bool CanBotStartFrom(BotController controller, Vector2Int cord)
    {
        if (OccupiedCoordinates.ContainsKey(cord) && OccupiedCoordinates[cord].Controller != controller)
            return true;
        return nodes.ContainsKey(cord) && cord.GetNeighbours().Any(n => OccupiedCoordinates.ContainsKey(n));
    }

    private FieldChangeData RemoveSingleBot(Vector2Int cord, out bool raise)
    {
        if (!OccupiedCoordinates.ContainsKey(cord) || !nodes.ContainsKey(cord))
        {
            raise = false;
            return new FieldChangeData();
        }
        var nodeToDelete = nodes[cord];
        var deletedCords = new HashSet<Vector2Int>();
        var bot = OccupiedCoordinates[cord];
        OccupiedCoordinates.Remove(cord);
        foreach (var n in cord.GetNeighbours())
        {
            if (!nodes.ContainsKey(n))
                continue;
            var neighbour = nodes[n];
            if (!OccupiedCoordinates.ContainsKey(n))
            {
                neighbour.RemoveConnection(nodeToDelete);
                nodeToDelete.RemoveConnection(neighbour);
                if (neighbour.connections.Length == 0)
                {
                    nodes.Remove(n);
                    DeleteNode(neighbour);
                    deletedCords.Add(n);
                }
            }
        }
        if (nodeToDelete.connections.Length == 0)
        {
            nodes.Remove(cord);
            DeleteNode(nodeToDelete);
            deletedCords.Add(cord);
        }
        raise = true;
        return new FieldChangeData(bot.Controller, deletedCords, deletedCords.Count > 0);
    }

    

    private PointNode AddNode(Int3 position)
    {
        if (unusedNodes.Count == 0)
            return graph.AddNode(position);
        var unused = unusedNodes.Dequeue();
        unused.position = position;
        unused.Walkable = true;
        return unused;
    }

    private HashSet<Vector2Int> PlaceSingleBot(Vector2Int cord, Occupant bot, out bool rise)
    {
        PointNode newNode;
        var addedCords = new HashSet<Vector2Int>();
        if (nodes.ContainsKey(cord))
            newNode = nodes[cord];
        else
        {
            newNode = AddNode((Int3)(Vector3)(cord.ToWorldPosition()));
            addedCords.Add(cord);
        }
        nodes[cord] = newNode;
        OccupiedCoordinates[cord] = bot;
        foreach (var n in cord.GetNeighbours())
        {
            if (!nodes.ContainsKey(n))
            {
                nodes[n] = AddNode((Int3)(Vector3)n.ToWorldPosition());
                addedCords.Add(n);
            }
            var neighbour = nodes[n];
            if (!newNode.ContainsConnection(neighbour))
            {
                newNode.AddConnection(neighbour, NonBotTravelCost);
                neighbour.AddConnection(newNode, NonBotTravelCost);
            }
            else
            {
                newNode.RemoveConnection(neighbour);
                neighbour.RemoveConnection(newNode);
                newNode.AddConnection(neighbour, BotTravelCost);
                neighbour.AddConnection(newNode, BotTravelCost);
            }
        }
        rise = true;
        return addedCords;
    }


    public void PlaceBot(Vector2 pos, BotController bot)
    {
        AstarPath.active.AddWorkItem(i =>
        {
            var cord = pos.ToCellCord();
            var addedCords = PlaceSingleBot(cord, new Occupant(bot), out var raise);
            if (raise)
            {
                AddNodesAtEvent?.Invoke(new FieldChangeData(bot, addedCords, addedCords.Count > 0));
                BotsPositionsChanged?.Invoke(new BotChangeData(bot.Blueprint, cord));
            }
        });
        AstarPath.active.FlushWorkItems();
    }

    public void ClearBot(Vector2Int cord)
    {
        AstarPath.active.AddWorkItem(i =>
        {
            if (!nodes.ContainsKey(cord))
                return;
            var data = RemoveSingleBot(cord, out var raise);
            if (raise)
            {
                RemoveNodesAtEvent?.Invoke(data);
                BotsPositionsChanged?.Invoke(new BotChangeData(BotBlueprint.Empty, cord));
            }
        });
        //AstarPath.active.FlushWorkItems();
    }

    public void ReserveStructureCells(Vector2 position, Structure structure)
    {
        foreach (var cell in structure.Blueprint.GetOccupiedCords(position))
        {
            if (reservedDestinations.ContainsKey(cell) && ReferenceEquals(reservedDestinations[cell], structure))
                continue;
            reservedDestinations.Add(cell, structure);
        }
    }

    public void ReleaseStructureCells(Vector2 position, Structure structure)
    {
        foreach (var cell in structure.Blueprint.GetOccupiedCords(position))
            ReleaseCell(cell);
    }

    public void ClearStructure(Vector2 pos, Structure structure)
    {
        AstarPath.active.AddWorkItem(i =>
        {
            var deletedCords = new HashSet<Vector2Int>();
            var raise = false;
            foreach (var localCord in structure.Blueprint.Layout.Keys)
            {
                var cord = structure.Blueprint.LocalCordToGlobal(localCord, pos);
                var result = RemoveSingleBot(cord, out var raiseThis);
                deletedCords.UnionWith(result.allChanges);
                raise |= raiseThis;
            }
            if (raise)
                AddNodesAtEvent?.Invoke(new FieldChangeData(structure, deletedCords, deletedCords.Count > 0));
        });
        AstarPath.active.FlushWorkItems();
    }

    public void PlaceStructure(Vector2 pos, Structure structure)
    {
        AstarPath.active.AddWorkItem(i =>
        {
            var addedCords = new HashSet<Vector2Int>();
            var raise = false;
            foreach (var localCord in structure.Blueprint.Layout.Keys)
            {
                var cord = structure.Blueprint.LocalCordToGlobal(localCord, pos);
                var bot = structure.Content[localCord];
                var result = PlaceSingleBot(cord, new Occupant(bot, structure), out var raiseThis);
                addedCords.UnionWith(result);
                raise |= raiseThis;
            }
            if (raise)
            { 
                AddNodesAtEvent?.Invoke(new FieldChangeData(structure, addedCords, addedCords.Count > 0));
            }
        });
    }

#if UNITY_EDITOR
    public void DrawGizmos()
    {
        if (OccupiedCoordinates == null)
            return;
        Gizmos.color = new Color(0, 1, 0, 0.1f);
        foreach (var o in OccupiedCoordinates.Keys)  
        {
            Gizmos.DrawCube(o.ToWorldPosition(), Vector3.one * CoordinatesConfig.CellSize);
        }
        Gizmos.color = new Color(1, 0, 0, 0.1f);
        foreach (var o in reservedDestinations.Keys)
        {
            Gizmos.DrawCube(o.ToWorldPosition(), Vector3.one * CoordinatesConfig.CellSize);
        }
    }
#endif
}

public struct Nearest
{
    public Vector2 reachable;
    public Vector2 traversable;
}

public class BotChangeData
{
    public readonly BotBlueprint bot;
    public readonly Vector2Int globlalCoordinate;

    public BotChangeData(BotBlueprint blueprint, Vector2Int position)
    {
        bot = blueprint;
        globlalCoordinate = position;
    }
}

public class FieldChangeData
{
    public readonly IReserverEntity mainCause;
    public readonly bool containtsGraphRewhire;
    public readonly HashSet<Vector2Int> allChanges;

    public FieldChangeData(IReserverEntity cause, HashSet<Vector2Int> allChanges, bool rewhire)
    {
        mainCause = cause;
        containtsGraphRewhire = rewhire;
        this.allChanges = allChanges;
    }

    public FieldChangeData()
    {
        mainCause = null;
        allChanges = new HashSet<Vector2Int>();
        containtsGraphRewhire = false;
    }
}
