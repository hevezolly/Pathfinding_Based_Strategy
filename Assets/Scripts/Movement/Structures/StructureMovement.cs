using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Pathfinding;
using System;
using UnityEngine.Events;

public class StructureMovement : AILerp
{
    [SerializeField]
    private Navigator navigator;
    public Vector2 Position => transform.position;

    [SerializeField]
    private ParametrisedEvent<StructureMovement> startMoveEvent;
    public ParametrisedEvent<StructureMovement> StartMovementEvent => startMoveEvent;

    [SerializeField]
    private ParametrisedEvent<StructureMovement> targetReachedEvent;
    public ParametrisedEvent<StructureMovement> TargetReachedEvent => targetReachedEvent;

    private Structure structure;

    private Vector2? reservedPosition = null;

    protected override void Awake()
    {
        base.Awake();
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        navigator.AddNodesAtEvent?.AddListener(OnAddedCells);
        navigator.RemoveNodesAtEvent?.AddListener(OnRemovedCells);
    }

    public override void OnDisable()
    {
        base.OnDisable();
        navigator.AddNodesAtEvent?.RemoveListener(OnAddedCells);
        navigator.RemoveNodesAtEvent?.RemoveListener(OnRemovedCells);
    }

    private void OnAddedCells(FieldChangeData cells)
    {
        if (!reachedDestination)
        {
            SearchPath();
        }
    }

    private void OnRemovedCells(FieldChangeData cells)
    {
        if (!reachedDestination)
        {
            SearchPath();
        }
    }

    private bool _isMoving = false;
    public bool IsMoving
    {
        get => _isMoving; private set
        {
            if (value != _isMoving)
            {
                if (value)
                    OnMoveStart();
                else
                    OnMoveFinish();
                _isMoving = value;
            }
        }
    }

    private ITraversalProvider traversal;

    protected override void OnPathComplete(Path _p)
    {
        if (_p.error)
        {
            var pos = GetFeetPosition();
            var path = ABPath.Construct(pos, ((Vector2)pos).ToCellCord().ToWorldPosition(), null);
            SetPath(path);
            return;
        }
        base.OnPathComplete(_p);
        if (path != null && !reachedEndOfPath)
        {
            IsMoving = true;
        }
    }

    private void OnMoveStart()
    {
        navigator.ClearStructure(Position, structure);
        startMoveEvent?.Invoke(this);
    }

    private void OnMoveFinish()
    {
        navigator.PlaceStructure(Position, structure);
        if (reachedDestination)
        {
            ClearReservation();
        }
        targetReachedEvent?.Invoke(this);
    }

    public void SetBlueprintSpecifications(ITraversalProvider traversal, Structure structure)
    {
        this.traversal = traversal;
        this.structure = structure;
    }

    public override void SearchPath()
    {
        if (float.IsPositiveInfinity(destination.x)) return;
        if (onSearchPath != null) onSearchPath();

        var currentPosition = GetFeetPosition();

        canSearchAgain = false;

        var path = ABPath.Construct(currentPosition, destination, null);
        path.traversalProvider = traversal;
        //path.calculatePartial = true;
        SetPath(path);
    }

    protected override void ConfigureNewPath()
    {
        base.ConfigureNewPath();
    }

    public override void OnTargetReached()
    {
        base.OnTargetReached();
        IsMoving = false;
    }

    private void ClearReservation()
    {
        if (reservedPosition == null)
            return;
        navigator.ReleaseStructureCells(reservedPosition.Value, structure);
        reservedPosition = null;
    }

    private void ReservePosition(Vector2 position)
    {
        ClearReservation();
        reservedPosition = position;
        navigator.ReserveStructureCells(position, structure);
    }

    protected override void Update()
    {
        var pos = (Vector2)Camera.main.ScreenToWorldPoint(Input.mousePosition);
        if (Input.GetMouseButtonDown(1))
        {
            var near = navigator.GetNearestCellsForStructure(structure, pos);
            if (near != null)
            {
                MoveTo(near.Value.reachable);
            }
        }
        
        base.Update();
    }

    protected override bool shouldRecalculatePath => base.shouldRecalculatePath && !reachedDestination;


    public void MoveTo(Vector2 pos)
    {
        destination = pos;
        ReservePosition(destination);
        SearchPath();
    }

    public override void OnDrawGizmos()
    {
        base.OnDrawGizmos();
        if (!reachedDestination)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, destination);
        }
    }
}
