using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EmptyBotBlueprint : BotBlueprint
{

    public override void Init(int index)
    {
    }

    public override int Index { get => -1; protected set { return; } }

    public override bool IsEmpty => true;

    public override GameObject Spawn(Vector2 position)
    {
        return null;
    }
}
