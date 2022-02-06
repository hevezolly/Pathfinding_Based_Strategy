using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class CoordinatesConfig
{
    public const float CellSize = 0.2f;

    public static Vector2 CellDimentions => new Vector2(CellSize, CellSize);

    public static Vector2Int ToCellCord(this Vector2 pos)
    {
        return Vector2Int.RoundToInt(pos / CellSize);
    }

    public static Vector2 ToWorldPosition(this Vector2Int cord)
    {
        return (Vector2)cord * CellSize;
    }

    public static IEnumerable<Vector2Int> GetEightNeighbours(this Vector2Int point)
    {
        yield return point + Vector2Int.right;
        yield return point + new Vector2Int(1, 1);
        yield return point + Vector2Int.up;
        yield return point + new Vector2Int(-1, 1);
        yield return point + Vector2Int.left;
        yield return point + new Vector2Int(-1, -1);
        yield return point + Vector2Int.down;
        yield return point + new Vector2Int(1, -1);
    }

    public static IEnumerable<Vector2Int> GetNeighbours(this Vector2Int point)
    {
        yield return point + Vector2Int.right;
        yield return point + Vector2Int.up;
        yield return point + Vector2Int.left;
        yield return point + Vector2Int.down;
    }
}
