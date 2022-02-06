using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

[BurstCompile]
public struct BuildChecker : IJob
{
    [ReadOnly]
    public Vector2Int placeLocalPos;

    [ReadOnly]
    public Vector2Int placeGlobalPos;

    [ReadOnly]
    public NativeHashMap<Vector2Int, int> building;

    [ReadOnly]
    public NativeHashMap<Vector2Int, int> field;

    [ReadOnly]
    public NativeHashMap<Vector2Int, bool> containsStructure;

    public NativeArray<bool> answer;

    public void Execute()
    {
        var success = true;
        var localPositions = building.GetKeyArray(Allocator.Temp);
        for (var i = 0; i < localPositions.Length; i++)
        {
            if (localPositions[i] == placeLocalPos)
                continue;
            var realPos = placeGlobalPos - placeLocalPos + localPositions[i];
            if (containsStructure.ContainsKey(realPos) && containsStructure[realPos])
            {
                success = false;
                break;
            }
            if (building[localPositions[i]] < 0)
            {
                if (field.ContainsKey(realPos))
                {
                    success = false;
                    break;
                }
            }
            else
            {
                if (!field.ContainsKey(realPos) || field[realPos] != building[localPositions[i]])
                {
                    success = false;
                    break;
                }
            }
        }
        localPositions.Dispose();
        answer[0] = success;
    }
}
