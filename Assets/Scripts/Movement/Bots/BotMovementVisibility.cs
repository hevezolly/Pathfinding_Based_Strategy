using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BotMovementVisibility : MonoBehaviour
{

    [SerializeField]
    private SpriteRenderer renderrer;
    [SerializeField]
    private int layerOffset;

    [SerializeField]
    private float scaleAmount;


    private int initialOrder;
    private Vector3 initialScale;
    private void Awake()
    {
        initialOrder = renderrer.sortingOrder;
        initialScale = transform.localScale;
    }
    
    public void StartMove()
    {
        renderrer.sortingOrder = initialOrder + layerOffset;
        transform.localScale = initialScale * scaleAmount;
    }

    public void DestinationReached()
    {
        renderrer.sortingOrder = initialOrder;
        transform.localScale = initialScale;
    }
}
