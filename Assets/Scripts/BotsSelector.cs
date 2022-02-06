using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class BotsSelector : MonoBehaviour
{
    private HashSet<BotsSpawner> spawners = new HashSet<BotsSpawner>();

    [SerializeField]
    private BotBlueprint botToSpawn;

    [SerializeField]
    private Navigator navigator;

    private int count = 1;

    private Coroutine cor;

    public void AddSpawner(BotsSpawner spawner)
    {
        if (!spawners.Contains(spawner))
        {
            spawners.Add(spawner);
        }
    }

    public void RemoveSpawner(BotsSpawner spawner)
    {
        if (spawners.Contains(spawner))
        {
            spawners.Remove(spawner);
        }
    }

    private BotsSpawner SelectSpawner(Vector2Int destinationPoint, BotBlueprint botToSpawn)
    {
        return spawners.FirstOrDefault();
    }

    private BotBlueprint SelectBot()
    {
        return botToSpawn;
    }

    private void SpawnBot(Vector2 pos)
    {
        var cord = navigator.GetClosestCell(pos);
        if (cord == null)
            return;
        var bot = SelectBot();
        if (bot == null)
            return;
        var spawner = SelectSpawner(cord.Value, bot);
        if (spawner == null)
            return;
        spawner.SpawnBot(bot, cord.Value);
        Debug.Log(++count);
    }

    private IEnumerator StartSpawning()
    {
        while (true)
        {
            var pos = (Vector2)Camera.main.ScreenToWorldPoint(Input.mousePosition);
            SpawnBot(pos);
            yield return new WaitForSeconds(0.1f);
        }
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            cor = StartCoroutine(StartSpawning());
        }
        else if (Input.GetMouseButtonUp(0))
        {
            if (cor != null)
            {
                StopCoroutine(cor);
                cor = null;
            }
        }
        else if (Input.GetKeyDown(KeyCode.S))
        {
            var cord = ((Vector2)Camera.main.ScreenToWorldPoint(Input.mousePosition)).ToCellCord();
            if (!navigator.OccupiedCoordinates.ContainsKey(cord) || navigator.OccupiedCoordinates[cord].IsEmpty)
                return;
            var bot = navigator.OccupiedCoordinates[cord].Controller.Movement;
            if (bot.MoveTo(Vector2Int.zero, false)) 
                bot.TargetReachedEvent.AddListener(DestroyMovement);
        }
    }

    private void DestroyMovement(BotMovement movement)
    {
        movement.TargetReachedEvent.RemoveListener(DestroyMovement);
        Destroy(movement.gameObject);
    }
}
