using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using System.Linq;

public class BuildManager : MonoBehaviour
{
    [SerializeField]
    private Builder builder;

    [SerializeField]
    private Navigator navigator;

    public ParametrisedEvent<Structure> NewStructureCreatedEvent;

    private void Awake()
    {
        builder.Init();
    }

    private void OnEnable()
    {
        navigator.BotsPositionsChanged.AddListener(TryBuildAt);
    }

    private void OnDisable()
    {
        navigator.BotsPositionsChanged.RemoveListener(TryBuildAt);   
    }

    private void TryBuildAt(BotChangeData data)
    {
        Debug.Log("build");
        var possibleBuilds = builder.GetPossibleBuilds(data.bot, data.globlalCoordinate);
        if (possibleBuilds.Count == 0)
            return;
        BuildStructure(GetBuild(possibleBuilds), data.globlalCoordinate);
    }

    private PossibleBuild GetBuild(List<PossibleBuild> possibleBuilds)
    {
        return possibleBuilds.First();
    }

    private void BuildStructure(PossibleBuild build, Vector2Int globalCord)
    {
        var centerPosition = build.Blueprint.GetCenterByTwoCords(build.LocalCord, globalCord);
        var parts = new Dictionary<Vector2Int, BotController>();
        foreach (var currentLocal in build.Blueprint.Layout.Keys
            .Where(k => !build.Blueprint.Layout[k].IsEmpty))
        {
            var currentGlobal = build.Blueprint.LocalCordToGlobal(currentLocal, centerPosition);
            var bot = navigator.OccupiedCoordinates[currentGlobal].Controller;
            parts.Add(currentLocal, bot);
        }
        var structure = Instantiate(build.Blueprint.StructureTemplate, centerPosition,
            build.Blueprint.StructureTemplate.transform.rotation).GetComponent<Structure>();
        structure.SetBlueprint(build.Blueprint, parts);
        NewStructureCreatedEvent?.Invoke(structure);
    }
}
