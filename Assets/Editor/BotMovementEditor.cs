using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Pathfinding;
using UnityEditor;

[CustomEditor(typeof(BotMovement), true)]
[CanEditMultipleObjects]
public class BotMovementEditor : AILerpEditor
{
    protected override void Inspector()
    {
        PropertyField("navigator");
        base.Inspector();
        PropertyField("startMoveEvent");
        PropertyField("stopMoveEvent");
        PropertyField("destinationReachedEvent");
    }
}
