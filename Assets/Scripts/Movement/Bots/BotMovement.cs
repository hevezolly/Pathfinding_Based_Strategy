using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Pathfinding;
using UnityEngine.Events;
using System;
using System.Linq;

[RequireComponent(typeof(BotController))]
public class BotMovement : AILerp
{

    [SerializeField]
    private Navigator navigator;

    [SerializeField]
    private ParametrisedEvent<BotMovement> startMoveEvent;
    public ParametrisedEvent<BotMovement> StartMovementEvent => startMoveEvent;

    [SerializeField]
    private ParametrisedEvent<BotMovement> stopMoveEvent;
    public ParametrisedEvent<BotMovement> StopMoveEvent => stopMoveEvent;

    [SerializeField]
    private ParametrisedEvent<BotMovement> destinationReachedEvent;

    public ParametrisedEvent<BotMovement> TargetReachedEvent => destinationReachedEvent;

    private Vector2Int destinationCell;

    private bool truleyReachedDestination = true;
    public bool IsMoving { get; private set; }

    private bool needToStartMoveEvent = true;

    private BotController controller;

    private Action StoppedUpdate = null;

    protected override void Awake()
    {
        controller = GetComponent<BotController>();
        base.Awake();
    }

    protected override void OnEnable()
    {
        navigator.AddNodesAtEvent.AddListener(OnNodesAdd);
        navigator.RemoveNodesAtEvent.AddListener(OnNodesRemove);
        canMove = true;
        base.OnEnable();
        
    }

    public override void OnDisable()
    {
        navigator.AddNodesAtEvent.RemoveListener(OnNodesAdd);
        navigator.RemoveNodesAtEvent.RemoveListener(OnNodesRemove);
        if (path != null)
        {
            path.Release(this);

            path = null;
        }

        canSearch = false;
        canSearchAgain = false;
        canMove = false;
        base.OnDisable();
    }

    private void OnNodesAdd(FieldChangeData nodes)
    {
        if (!truleyReachedDestination)
        {
            if (!nodes.containtsGraphRewhire || ReferenceEquals(nodes.mainCause, controller))
                return;
            destination = destinationCell.ToWorldPosition();
            SearchPath();
        }
    }

    private void OnNodesRemove(FieldChangeData nodes)
    {
        if (!truleyReachedDestination)
        {
            if (!nodes.containtsGraphRewhire || ReferenceEquals(nodes.mainCause, controller))
                return;

            destination = destinationCell.ToWorldPosition();
            SearchPath();
        }
    }

    public bool MoveTo(Vector2 position, bool resetPosition = true)
    {
        if (!enabled)
            return false;
        var cord = position.ToCellCord();

        if (!navigator.CanBotStartFrom(controller, Position.ToCellCord()))
            return false;

        destinationCell = cord;
        destination = position;
        truleyReachedDestination = false;

        navigator.ReserveCell(destinationCell, controller);
        SearchPath();

        return true;
    }

    public void ClearCurrent()
    {
        var cord = Position.ToCellCord();
        if (navigator.OccupiedCoordinates.ContainsKey(cord) && !navigator.OccupiedCoordinates[cord].IsEmpty
            && navigator.OccupiedCoordinates[cord].Controller.Movement == this)
        {
            navigator.ClearBot(cord);
        }
    }

    public bool SetNewBot(Vector2Int releaseCord)
    {
        navigator.ReleaseCell(releaseCord);

        if (!navigator.OccupiedCoordinates.ContainsKey(Position.ToCellCord()))
        {
            navigator.PlaceBot(Position, controller);
            return true;
        }
        return false;
    }

    private void CheckStartMoving()
    {
        if (isStopped)
            return;
        if (path != null && !reachedEndOfPath)
        {
            ClearCurrent();
            var prevMoving = IsMoving;
            IsMoving = true;
            if (prevMoving != IsMoving && needToStartMoveEvent)
            {
                StartMovementEvent?.Invoke(this);
                needToStartMoveEvent = false;
            }
        }
    }

    private bool TurnicateIncorrectPath(Path _p)
    {
        var pos = (Vector2)(Vector3)_p.path[_p.path.Count - 1].position;
        if (pos.ToCellCord() != destinationCell)
        {
            if (_p.vectorPath.Count <= 2)
                return false;
            var lastIndex = _p.vectorPath.FindLastIndex((p) => 
            navigator.OccupiedCoordinates.ContainsKey(((Vector2)p).ToCellCord()));
            _p.vectorPath.RemoveRange(lastIndex + 1, _p.vectorPath.Count - lastIndex - 1);
            _p.path.RemoveRange(lastIndex + 1, _p.vectorPath.Count - lastIndex - 1);
        }
        return true;
    }

    protected override void OnPathComplete(Path _p)
    {
        if (truleyReachedDestination)
            return;
        if (_p.error)
        {
            _p.Release(this);
            return;
        }
        else
        {
            if (!TurnicateIncorrectPath(_p))
                return;
        }
        base.OnPathComplete(_p);
        CheckStartMoving();
    }

    protected override void Update()
    {
        base.Update();
        if (StoppedUpdate != null)
        {
            StoppedUpdate?.Invoke();
            StoppedUpdate = null;
        }
    }

    private void InvokeStop()
    {
        StopMoveEvent?.Invoke(this);
        needToStartMoveEvent = true;
    }

    public override void OnTargetReached()
    {
        base.OnTargetReached();
        var prevIsMoving = IsMoving;
        IsMoving = false;
        var justStopped = prevIsMoving != IsMoving;
        if (truleyReachedDestination)
            return;
        if (Position.ToCellCord() == destinationCell)
        {
            StoppedUpdate = () =>
            {
                truleyReachedDestination = true;
                TargetReachedEvent?.Invoke(this);
                InvokeStop();
                SetNewBot(destinationCell);
            };
        }
        else
        {
            if (!isStopped)
                return;
            StoppedUpdate = () =>
            {
                if (SetNewBot(destinationCell) && justStopped)
                {
                    InvokeStop();
                }
            };
        }
    }

    protected override bool shouldRecalculatePath => base.shouldRecalculatePath && !truleyReachedDestination;

    public Vector2 Position => transform.position;

}
