using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NavigatorInit : MonoBehaviour
{
    [SerializeField]
    private Navigator navigator;
    [SerializeField]
    private GameObject BaseBot;

    private void Awake()
    {
        navigator.Initiate(BaseBot.GetComponent<BotController>());
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        navigator.DrawGizmos();  
    }
#endif
}
