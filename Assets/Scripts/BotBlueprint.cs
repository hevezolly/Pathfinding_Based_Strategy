using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "new bot", menuName = "Bot")]
public class BotBlueprint : ScriptableObject
{
    [SerializeField]
    private Texture2D texture;
    [SerializeField]
    private GameObject botObject;

    public virtual int Index { get; protected set; }

    public virtual bool IsEmpty => false;

    public Sprite Sprite { get; protected set; }

    public virtual void Init(int index)
    {
        Index = index;
        Sprite = CreateSprite();
    }

    public virtual GameObject Spawn(Vector2 position)
    {
        var bot = Instantiate(botObject, position, botObject.transform.rotation);
        bot.GetComponent<BotController>().SetBot(this);
        return bot;
    }

    private Sprite CreateSprite()
    {
        var dimInTexCord = Vector2.one * Mathf.Min((float)texture.width, (float)texture.height);
        var rect = new Rect(0, texture.height - dimInTexCord.y, dimInTexCord.x, dimInTexCord.y);
        var pivot = Vector2.one / 2;
        var pixelsPerUnit = dimInTexCord.x / CoordinatesConfig.CellSize;
        return Sprite.Create(texture, rect, pivot, pixelsPerUnit);
    }

    private static BotBlueprint _emptyBlueprint;

    public static BotBlueprint Empty
    {
        get
        {
            if (_emptyBlueprint == null)
            {
                _emptyBlueprint = CreateInstance<EmptyBotBlueprint>();
            }
            return _emptyBlueprint;
        }
    }
}
