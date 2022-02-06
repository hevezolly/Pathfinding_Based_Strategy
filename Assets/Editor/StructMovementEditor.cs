using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Pathfinding;
using UnityEditor;


[CustomEditor(typeof(StructureMovement), true)]
[CanEditMultipleObjects]
public class StructMovementEditor : AILerpEditor
{
    protected override void Inspector()
    {
        PropertyField("navigator");
        base.Inspector();
        PropertyField("startMoveEvent");
        PropertyField("targetReachedEvent");
    }
}
