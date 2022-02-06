using System.Collections;
using System.Collections.Generic;
using Pathfinding;
using UnityEngine;

public class BotsSpawner : MonoBehaviour
{
    [SerializeField]
    private GameObject botObj;

    [SerializeField]
    private Navigator navigator;

    public bool CanSpawn { get; private set; } = false;

    private BotsSelector selector;

    private void Awake()
    {
        selector = FindObjectOfType<BotsSelector>();
    }

    private void Start()
    {
        ReadyToSpawn();
    }

    public void ReadyToSpawn()
    {
        selector.AddSpawner(this);
        CanSpawn = true;
    }

    public void NotReadyToSpawn()
    {
        selector.RemoveSpawner(this);
        CanSpawn = false;
    }

    public void SpawnBot(BotBlueprint botData, Vector2 pos)
    {
        var destination = navigator.GetClosestCell(pos);
        if (destination == null)
            return;
        SpawnBot(botData, destination.Value);
    }

    public void SpawnBot(BotBlueprint botData, Vector2Int coordinate)
    {
        if (!CanSpawn)
            return;
        var bot = botData.Spawn(transform.position);
        var movement = bot.GetComponent<BotMovement>();
        
        movement.MoveTo(coordinate.ToWorldPosition());
    }
}
