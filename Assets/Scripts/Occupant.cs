using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Occupant
{
    private BotController controller;

    public bool IsEmpty => controller == null;

    public BotController Controller => controller;

    public BotBlueprint Blueprint => (IsEmpty ? BotBlueprint.Empty : controller.Blueprint);

    public bool IsInStructure => (IsEmpty ? structure != null : controller.IsInStructure);

    public Structure Structure => (!IsEmpty ? controller.Structure : structure);
    private Structure structure;
    public Occupant(BotController controller, Structure structure)
    {
        this.controller = controller;
        this.structure = structure;
    }

    public Occupant(BotController controller) : this(controller, null) { }

    public Occupant(Structure structure) : this(null, structure) { }
}
