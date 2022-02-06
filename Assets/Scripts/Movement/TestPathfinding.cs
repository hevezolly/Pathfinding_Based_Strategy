using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Pathfinding;

public class TestPathfinding : MonoBehaviour
{

    private AILerp ai;

    private void OnEnable()
    {
        ai = GetComponent<AILerp>();
        if (ai != null) ai.onSearchPath += Update;
    }

    private void OnDisable()
    {
        if (ai != null) ai.onSearchPath -= Update;
    }

    // Update is called once per frame
    void Update()
    {
        if (ai == null)
            return;
        var pos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        ai.destination = pos;
    }
}
