using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

[CreateAssetMenu(fileName = "new struct blueprint", menuName = "Blueprint")]
public class StructureBlueprint : ScriptableObject
{
    private const char NotPartOfStructure = '?';
    private const char EmptyTraversable = '_';
    private const char EmptyNonTraversable = '#';

    [SerializeField]
    private Texture2D StructureTexture;
    [Tooltip("layout of bots. symbol " +
        "<?> - place is not part of a structure. " +
        "<#> - place must be empty, not reachable for bots." +
        "<_> - place must be empty, reachable for bots." +
        "Other symbols are the same as in usedBots List")]
    [SerializeField]
    [TextArea(3, 50)]
    private string LayoutEncoding;

    [SerializeField]
    private GameObject structTemplate;

    public GameObject StructureTemplate => structTemplate;
    
    [System.Serializable]
    private class MarkedBot
    {
        public char Symbol;
        public BotBlueprint Bot;
    }

    [Tooltip("used bots and encodings. Use any Character except <?>, <_> and <#>")]
    [SerializeField]
    private List<MarkedBot> usedBots;

    private Vector2Int dimentions;
    public Vector2Int Dimentions => dimentions;

    private Dictionary<Vector2Int, BotBlueprint> layout;

    public Dictionary<Vector2Int, BotBlueprint> Layout => layout;

    private Dictionary<Vector2Int, BotBlueprint> layoutForBuilding;
    public Dictionary<Vector2Int, BotBlueprint> LayoutForBuilding => layoutForBuilding;

    private IEnumerable<Vector2Int> PrecalculatedNeighbours;

    private IEnumerable<Vector2Int> PrecalculatedFourNeighbours;


    private Dictionary<BotBlueprint, HashSet<Vector2Int>> localPositionsOfBots;

    public Vector2 Offset { get; private set; }

    public Sprite Sprite { get; private set; }

    private Dictionary<char, BotBlueprint> GetCharacterTable()
    {
        var table = new Dictionary<char, BotBlueprint>();
        foreach (var sb in usedBots)
        {
            table.Add(sb.Symbol, sb.Bot);
        }
        return table;
    }

    private void ConfigureLayout()
    {
        localPositionsOfBots = new Dictionary<BotBlueprint, HashSet<Vector2Int>>();
        layout = new Dictionary<Vector2Int, BotBlueprint>();
        layoutForBuilding = new Dictionary<Vector2Int, BotBlueprint>();
        var lookupTable = GetCharacterTable();
        var lines = LayoutEncoding.Split();
        dimentions.y = lines.Length;
        dimentions.x = lines.Max(l => l.Length);
        var current = new Vector2Int(0, dimentions.y - 1);
        foreach (var line in lines)
        {
            foreach (var symb in line)
            {
                if (symb == EmptyNonTraversable)
                {
                    if (!localPositionsOfBots.ContainsKey(BotBlueprint.Empty))
                        localPositionsOfBots[BotBlueprint.Empty] = new HashSet<Vector2Int>();
                    layout[current] = BotBlueprint.Empty;
                    layoutForBuilding[current] = BotBlueprint.Empty;
                    localPositionsOfBots[BotBlueprint.Empty].Add(current);
                }
                else if (symb == EmptyTraversable)
                {
                    if (!localPositionsOfBots.ContainsKey(BotBlueprint.Empty))
                        localPositionsOfBots[BotBlueprint.Empty] = new HashSet<Vector2Int>();
                    layoutForBuilding[current] = BotBlueprint.Empty;
                    localPositionsOfBots[BotBlueprint.Empty].Add(current);
                }
                else if (symb != NotPartOfStructure)
                {
                    if (!localPositionsOfBots.ContainsKey(lookupTable[symb]))
                        localPositionsOfBots[lookupTable[symb]] = new HashSet<Vector2Int>();
                    layout[current] = lookupTable[symb];
                    layoutForBuilding[current] = lookupTable[symb];
                    localPositionsOfBots[lookupTable[symb]].Add(current);
                }
                current.x++;
            }
            current.x = 0;
            current.y--;
        }
    }

    public Vector2Int LocalCordToGlobal(Vector2Int local, Vector2 center)
    {
        return local + GetOriginPosition(center).ToCellCord();
    }

    public Vector2 GetCenterByTwoCords(Vector2Int local, Vector2Int global)
    {
        var originPos = (global - local).ToWorldPosition();
        //center - Offset - ((dimentions).ToWorldPosition() - CoordinatesConfig.CellDimentions) / 2;
        return originPos + Offset + ((dimentions).ToWorldPosition() - CoordinatesConfig.CellDimentions) / 2;
    }

    public bool ContainsBot(BotBlueprint bot)
    {
        return localPositionsOfBots.ContainsKey(bot);
    }

    public HashSet<Vector2Int> BotsPositions(BotBlueprint bot)
    {
        return localPositionsOfBots[bot];
    }

    private Vector3 CalculateOffset()
    {
        return new Vector3((Dimentions.x % 2 == 0) ? CoordinatesConfig.CellSize / 2 : 0f,
           (Dimentions.y % 2 == 0) ? CoordinatesConfig.CellSize / 2 : 0f);
    }

    private Sprite CalculateSprite()
    {
        var dimInTexCord = (Vector2)Dimentions * Mathf.Min(StructureTexture.width / (float)Dimentions.x,
            (float)StructureTexture.height / Dimentions.y);
        var rect = new Rect(0, StructureTexture.height - dimInTexCord.y, dimInTexCord.x, dimInTexCord.y);
        var pivot = (Vector2.one +
            new Vector2((Dimentions.x % 2 == 0) ? 1f / Dimentions.x : 0f,
            (Dimentions.y % 2 == 0) ? 1f / Dimentions.y : 0f)) / 2;
        var pixelsPerUnit = dimInTexCord.x / (Dimentions.x * CoordinatesConfig.CellSize);
        return Sprite.Create(StructureTexture, rect, pivot, pixelsPerUnit);
    }

    public void Init()
    {
        ConfigureLayout();

        Offset = CalculateOffset();

        Sprite = CalculateSprite();

        PrecalculatedNeighbours = CalculateNeighbours();
        PrecalculatedFourNeighbours = CalculateNeighbours(false);
    }

    #region neighbours

    public IEnumerable<Vector2Int> GetNeighbours(Vector2 center)
    {
        var cordOffset = GetOriginPosition(center).ToCellCord();
        return PrecalculatedNeighbours.Select(n => n + cordOffset);
    }

    public IEnumerable<Vector2Int> GetFourNeighbours(Vector2 center)
    {
        var cordOffset = GetOriginPosition(center).ToCellCord();
        return PrecalculatedFourNeighbours.Select(n => n + cordOffset);
    }

    public IEnumerable<Vector2Int> GetOccupiedCords(Vector2 center, bool countEmpty=true)
    {
        var cordOffset = GetOriginPosition(center).ToCellCord();
        return layout.Keys.Where(k => countEmpty || !layout[k].IsEmpty).Select(n => n + cordOffset);
    }

    public IEnumerable<Vector2Int> GetNeighbours()
    {
        return PrecalculatedNeighbours;
    }

    public IEnumerable<Vector2Int> GetFourNeighbours()
    {
        return PrecalculatedFourNeighbours;
    }

    private IEnumerable<Vector2Int> CalculateNeighbours(bool eight=true)
    {
        var visitedNeighbours = new HashSet<Vector2Int>();
        foreach (var cord in layout.Keys) 
        {
            if (layout[cord].IsEmpty)
                continue;
            var neighbours = cord.GetEightNeighbours();
            if (!eight)
                neighbours = cord.GetNeighbours();
            foreach (var n in neighbours)
            {
                if (visitedNeighbours.Contains(n) || layout.ContainsKey(n))
                    continue;
                yield return n;
                visitedNeighbours.Add(n);
            }
        }
    }

    #endregion

    public Vector2 GetOriginPosition(Vector2 center)
    {
        return center - Offset - ((dimentions).ToWorldPosition() - CoordinatesConfig.CellDimentions) / 2;
    }

    public Vector2 GerCenterPosFromCenterCord(Vector2Int centerCord)
    {
        return centerCord.ToWorldPosition() + Offset;
    }

    public Vector2Int GetCenterCord(Vector2 center)
    {
        return (center - Offset).ToCellCord();
    }

#if UNITY_EDITOR
    public void DrawStructure(Vector2 center)
    {
        if (layout == null)
            return;
        foreach (var cord in GetOccupiedCords(center))
        {
            Gizmos.color = new Color(1, 0, 0, 0.2f);
            Gizmos.DrawCube(cord.ToWorldPosition(),
                Vector2.one * CoordinatesConfig.CellSize);
        }
        foreach (var n in GetNeighbours(center))
        {
            Gizmos.color = new Color(1, 1, 0, 0.1f);
            Gizmos.DrawCube(n.ToWorldPosition(),
                Vector2.one * CoordinatesConfig.CellSize);
        }
    }
#endif
}
