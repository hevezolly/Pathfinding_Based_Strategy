using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Jobs;
using System;
using System.Linq;
using Unity.Collections;

[CreateAssetMenu(fileName = "new Builder", menuName = "Builder")]
public class Builder : ScriptableObject
{

    [SerializeField]
    private List<StructureBlueprint> recepies;

    [SerializeField]
    private Navigator navigator;

    public void Init()
    {
        foreach (var r in recepies)
            r.Init();
    }

    public List<PossibleBuild> GetPossibleBuilds(BotBlueprint bot, Vector2Int position)
    {
        if (bot == null)
            bot = BotBlueprint.Empty;
        var fieldStruct = new NativeHashMap<Vector2Int, int>(navigator.OccupiedCoordinates.Count, Allocator.TempJob);
        var structsStruct = new NativeHashMap<Vector2Int, bool>(
            navigator.OccupiedCoordinates.Values.Where(b => b.IsInStructure)
            .Count(), Allocator.TempJob);
        var handles = new List<JobHandle>();

        var answers = new Dictionary<Tuple<StructureBlueprint, Vector2Int>, NativeArray<bool>>();
        foreach (var pos in navigator.OccupiedCoordinates.Keys)
        {
            fieldStruct[pos] = navigator.OccupiedCoordinates[pos].Blueprint.Index;
            if (navigator.OccupiedCoordinates[pos].IsInStructure)
                structsStruct[pos] = true;
        }
        var buildings = new Dictionary<StructureBlueprint, NativeHashMap<Vector2Int, int>>();
        foreach (var recepy in recepies)
        {
            if (!recepy.ContainsBot(bot))
                continue;
            buildings[recepy] = new NativeHashMap<Vector2Int, int>(recepy.LayoutForBuilding.Count, Allocator.TempJob);
            foreach (var pos in recepy.LayoutForBuilding.Keys)
            {
                    buildings[recepy].TryAdd(pos, recepy.LayoutForBuilding[pos].Index);
            }

            foreach (var tryLocal in recepy.BotsPositions(bot))
            {
                var key = new Tuple<StructureBlueprint, Vector2Int>(recepy, tryLocal);
                answers[key] = new NativeArray<bool>(new[] { false }, Allocator.TempJob);
                var job = new BuildChecker()
                {
                    placeLocalPos = tryLocal,
                    placeGlobalPos = position,
                    containsStructure = structsStruct,
                    building = buildings[recepy],
                    field = fieldStruct,
                    answer = answers[key]
                };
                handles.Add(job.Schedule());
            }
        }
        foreach (var h in handles)
        {
            h.Complete();
        }
        fieldStruct.Dispose();
        structsStruct.Dispose();
        foreach (var building in buildings.Keys)
            buildings[building].Dispose();
        var results = new List<PossibleBuild>();
        foreach (var key in answers.Keys)
        {
            if (answers[key][0])
            {
                results.Add(new PossibleBuild(key));
            }
            answers[key].Dispose();
        }
        return results;
    }
}

public class PossibleBuild
{
    public readonly Vector2Int LocalCord;
    public readonly StructureBlueprint Blueprint;

    public PossibleBuild(Vector2Int cord, StructureBlueprint blueprint)
    {
        LocalCord = cord;
        Blueprint = blueprint;
    }

    public PossibleBuild(Tuple<StructureBlueprint, Vector2Int> pair) : this(pair.Item2, pair.Item1) { }
}


