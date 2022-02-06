using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class BotController : MonoBehaviour, IReserverEntity
{

    [SerializeField]
    private BotBlueprint bot;
    [SerializeField]
    private SpriteRenderer renderer;
    public virtual BotBlueprint Blueprint => bot;

    [SerializeField]
    private UnityEvent disableBotEvent;

    public UnityEvent DisableBotEvent => disableBotEvent;

    [SerializeField]
    private UnityEvent enableBotEvent;

    public UnityEvent EnableBotEvent => enableBotEvent;

    public BotMovement Movement { get; private set; }

    public bool IsInStructure => Structure != null;

    public Structure Structure { get; private set; }

    public bool IsSingleBot => true;

    private void Awake()
    {
        Movement = GetComponent<BotMovement>();
    }

    private void Start()
    {
        if (bot != null)
        {
            bot.Init(0);
            SetBot(bot);
        }
    }

    public void JoinToStructure(Structure structure)
    {
        Structure = structure;
        GetComponent<BotMovement>().enabled = false;
        disableBotEvent?.Invoke();
    }

    public void RemoveFromStructure()
    {
        Structure = null;
        GetComponent<BotMovement>().enabled = true;
        enableBotEvent?.Invoke();
    }

    public void SetBot(BotBlueprint bot)
    {
        this.bot = bot;
        renderer.sprite = bot.Sprite;
    }
}
