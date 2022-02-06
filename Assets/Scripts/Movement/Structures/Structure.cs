using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Pathfinding;
using System.Linq;

public class Structure : MonoBehaviour, IReserverEntity
{
    [SerializeField]
    private StructureBlueprint blueprint;
    [SerializeField]
    private Navigator navigator;
    [SerializeField]
    private SpriteRenderer SpriteRenderer;

    public StructureBlueprint Blueprint => blueprint;

    public bool IsSingleBot => false;

    private Dictionary<BotController, Vector3> partsOffsets;

    public Dictionary<Vector2Int, BotController> Content { get; private set; }

    private StructureMovement movable;

    private void Awake()
    {
        movable = GetComponent<StructureMovement>();
    }

    private void Start()
    {
        //blueprint.Init();
        var parts = new Dictionary<Vector2Int, BotController>();
        foreach (var local in blueprint.Layout.Keys.Where(k => !blueprint.Layout[k].IsEmpty))
        {
            var cord = blueprint.LocalCordToGlobal(local, transform.position);
            var bot = blueprint.Layout[local].Spawn(cord.ToWorldPosition());
            parts[local] = bot.GetComponent<BotController>();
        }
        SetBlueprint(blueprint, parts);
    }

    public void SetBlueprint(StructureBlueprint blueprint, Dictionary<Vector2Int, BotController> parts)
    {
        this.blueprint = blueprint;
        ApplyBlueprint();
        Content = new Dictionary<Vector2Int, BotController>(parts);
        ExtendContent();
        LinkParts(parts);
    }

    private void ExtendContent()
    {
        foreach (var local in blueprint.Layout.Keys.Where(k => blueprint.Layout[k].IsEmpty))
        {
            var cord = blueprint.LocalCordToGlobal(local, transform.position);
            Content[local] = null;
        }
    }

    private void LinkParts(Dictionary<Vector2Int, BotController> parts)
    {
        partsOffsets = new Dictionary<BotController, Vector3>();
        foreach (var p in parts.Keys)
        {
            if (parts[p] == null)
                continue;
            partsOffsets.Add(parts[p], parts[p].transform.position - transform.position);
            parts[p].JoinToStructure(this);
        }
    }

    private void Update()
    {
        if (!movable.IsMoving)
            return;
        if (partsOffsets == null)
            return;
        foreach (var bot in partsOffsets.Keys)
        {
            bot.transform.position = transform.position + partsOffsets[bot];
        }
    }

    private void ApplyBlueprint()
    {
        SpriteRenderer.sprite = blueprint.Sprite;
        GetComponent<StructureMovement>()
            .SetBlueprintSpecifications(new StructureTraversal(this, navigator), this);
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (blueprint != null)
        {
            blueprint.DrawStructure(transform.position);
        }
    }
#endif
}
