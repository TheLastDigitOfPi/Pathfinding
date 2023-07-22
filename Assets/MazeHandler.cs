using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using System;
using System.Threading.Tasks;
using System.Linq;
using static MazeHandler;
using TMPro;
using Unity.VisualScripting;
using static UnityEditor.PlayerSettings;
using Unity.VisualScripting.Antlr3.Runtime.Tree;

public class MazeHandler : MonoBehaviour
{

    public static MazeHandler Instance;

    [field: SerializeField] public Maze MazeData { get; private set; }

    [field: Header("Tiles")]
    [field: SerializeField] public Tilemap MazeMap { get; private set; }
    [field: SerializeField] public Tilemap HighlightMap { get; private set; }
    [field: SerializeField] public TileBase WhiteTile { get; private set; }
    [field: SerializeField] public TileBase BlackTile { get; private set; }
    [field: SerializeField] public TileBase GreenTile { get; private set; }
    [field: SerializeField] public TileBase YellowTile { get; private set; }
    [field: SerializeField] public TileBase GrayTile { get; private set; }
    [field: SerializeField] public TileBase RedTile { get; private set; }
    [field: SerializeField] public TileBase BlueTile { get; private set; }

    [field: Header("Maze Generation")]
    [field: SerializeField] public string Seed { get; private set; }
    [field: SerializeField] public bool UseRandomSeed { get; private set; }
    [field: SerializeField] public MazeGenerator[] MazeGenerators { get; private set; }
    [field: SerializeField] public int CurrentMazeGenerator { get; private set; }


    [field: Header("Size Settings")]
    [field: SerializeField] public TextMeshProUGUI SizeText { get; private set; }
    public int MazeSize { get { return MazeData.Size; } }
    [SerializeField] int _currentMazeSizeIndex;

    [SerializeField] int[] _mazeSizes = { 10, 25, 32, 50, 100 };

    [field: Header("Speed Settings")]

    [field: SerializeField] public int DelayTimeMS { get; private set; } = 100;
    [field: SerializeField] public int DelayFrenquency { get; private set; } = 5;
    [field: SerializeField] public TextMeshProUGUI SpeedText { get; private set; }
    [field: SerializeField] public int FastestMSSpeed { get; private set; } = 2;
    [field: SerializeField] public int SlowestMSSpeed { get; private set; } = 100;
    [field: SerializeField] public int MSChangeAmount { get; private set; } = 20;
    [field: SerializeField] public int FastestFrequency { get; private set; } = 100;
    [field: SerializeField] public int SlowestFrequency { get; private set; } = 1;
    [field: SerializeField] public int FrequencyChangeAmount { get; private set; } = 5;


    public event Action OnMazeGenerated;


    [field: Header("Tile Tracking")]
    public Cell StartCell { get; private set; }
    public Cell EndCell { get; private set; }
    [field: SerializeField] public int BlackTilesCount { get; private set; }
    [field: SerializeField] public int WhiteTilesCount { get; private set; }
    [field: SerializeField] public int GreenTilesCount { get; private set; }
    [field: SerializeField] public int YellowTilesCount { get; private set; }
    [field: SerializeField] public int GrayTilesCount { get; private set; }
    [SerializeField] TextMeshProUGUI BlackTileCounter;
    [SerializeField] TextMeshProUGUI WhiteTileCounter;
    [SerializeField] TextMeshProUGUI GreenTileCounter;
    [SerializeField] TextMeshProUGUI YellowTileCounter;


    [field: Header("Maze Solving")]
    [field: SerializeField] public MazeSolver[] MazeSolvers { get; private set; }
    [field: SerializeField] public int CurrentMazeSolver { get; private set; } = 0;

    private void Awake()
    {
        if (Instance != null)
            return;
        Instance = this;
    }

    void Start()
    {
        ResetMaze();
        UpdateSpeedText();
        UpdateSizeText();
        StartCell = null;
        EndCell = null;
    }

    #region Maze Generation
    public void ToggleRunMazeGeneration()
    {
        MazeGenerators[CurrentMazeGenerator].ToggleMazeGeneration();
    }

    public void ResetMazeGeneration()
    {
        MazeGenerators[CurrentMazeGenerator].ResetMazeGeneration();
        ResetMaze();
    }
    #endregion

    #region Cell Updating

    internal void ClearPathfindingCells()
    {
        for (int x = 0; x < MazeData.Size; x++)
        {
            for (int y = 0; y < MazeData.Size; y++)
            {
                if (!MazeData.TryGetCellAtPos(x, y, out var cell))
                    continue;
                cell.PathfindingValue = -1;
                cell.PathfindingVisited = false;
                if (cell.CellType != CellTypes.Searching)
                    continue;
                cell.CellType = CellTypes.Floor;
                PlaceTile(CellTypes.Floor, cell.Position3);
            }
        }
    }
    public void ResetMaze()
    {
        MazeData.ResetMaze();
        MazeMap.ClearAllTiles();
        ResetTileCounters();
        MazeMap.transform.localScale = new Vector3(0.275f * 0.33f, 0.275f * 0.33f, 1) * (100f / (MazeSize + 2));
        HighlightMap.transform.localScale = new Vector3(0.275f * 0.33f, 0.275f * 0.33f, 1) * (100f / (MazeSize + 2));
        MazeMap.transform.localPosition = new Vector3(MazeMap.transform.localScale.x * (-1 * MazeSize / 2f), MazeMap.transform.localScale.y * (-1 * MazeSize / 2f), 0);
        HighlightMap.transform.localPosition = new Vector3(MazeMap.transform.localScale.x * (-1 * MazeSize / 2f), MazeMap.transform.localScale.y * (-1 * MazeSize / 2f), 0);

        for (int i = -1; i < MazeSize + 1; i++)
        {
            for (int y = -1; y < MazeSize + 1; y++)
            {
                if (i == -1 || i == MazeSize || y == -1 || y == MazeSize)
                {
                    PlaceTile(CellTypes.Wall, new Vector3Int(i, y, 0));
                    continue;
                }
                PlaceTile(CellTypes.Empty, new Vector3Int(i, y, 0));
            }
        }
        StartCell = null;
        EndCell = null;

    }

    void ResetTileCounters()
    {
        BlackTilesCount = 0;
        GreenTilesCount = 0;
        WhiteTilesCount = 0;
        YellowTilesCount = 0;
        //Update visual text counter
        BlackTileCounter.text = BlackTilesCount.ToString();
        GreenTileCounter.text = GreenTilesCount.ToString();
        WhiteTileCounter.text = WhiteTilesCount.ToString();
        YellowTileCounter.text = YellowTilesCount.ToString();
    }
    public void UpdateMaze()
    {
        foreach (var cell in MazeData.Cells)
        {
            if (cell.CellType != TileBaseToEnum(MazeMap.GetTile(cell.Position3)))
                PlaceTile(cell.CellType, cell.Position3);
        }
    }
    public bool CellOnExternalWalls(int x, int y)
    {
        if (x == -1 || x == MazeSize)
        {
            if (y >= -1 && y <= MazeSize)
                return true;
        }
        if (y == -1 || y == MazeSize)
        {
            if (x >= -1 && x <= MazeSize)
                return true;
        }
        return false;
    }
    public CellTypes TileBaseToEnum(TileBase baseTile)
    {
        if (baseTile == WhiteTile)
            return CellTypes.Floor;
        if (baseTile == BlackTile)
            return CellTypes.Wall;
        if (baseTile == GrayTile)
            return CellTypes.Empty;
        if (baseTile == YellowTile)
            return CellTypes.End;
        if (baseTile == GreenTile)
            return CellTypes.Start;
        return CellTypes.Empty;
    }
    public TileBase EnumToTileBase(CellTypes baseTile)
    {
        if (baseTile == CellTypes.Floor)
            return WhiteTile;
        if (baseTile == CellTypes.Wall)
            return BlackTile;
        if (baseTile == CellTypes.Empty)
            return GrayTile;
        if (baseTile == CellTypes.End)
            return YellowTile;
        if (baseTile == CellTypes.Start)
            return GreenTile;
        if (baseTile == CellTypes.Searching)
            return RedTile;
        if (baseTile == CellTypes.FoundPath)
            return BlueTile;
        return WhiteTile;
    }



    /// <summary>
    /// Set the maze tile to the desired tile type
    /// </summary>
    /// <param name="newTileType"></param>
    /// <param name="pos"></param>
    public void PlaceTile(CellTypes newTileType, Vector3Int pos)
    {
        //If not on our maze, return
        if (!MazeData.CellWithinBounds(pos.x, pos.y) && !CellOnExternalWalls(pos.x, pos.y))
            return;
        var originalTile = MazeMap.GetTile(pos);
        var originalTileType = TileBaseToEnum(originalTile);
        //If changing tile to same tile, return
        if (originalTileType == newTileType && originalTile != null)
            return;

        //Check if tile is allowed to be placed
        if (newTileType == CellTypes.Start && GreenTilesCount > 0 && newTileType != CellTypes.Empty)
            return;
        if (newTileType == CellTypes.End && YellowTilesCount > 0 && newTileType != CellTypes.Empty)
            return;

        //Remove original tile count
        var orginalTileType = TileBaseToEnum(originalTile);
        switch (orginalTileType)
        {
            case CellTypes.Start:
                StartCell = null;
                GreenTilesCount--;
                break;
            case CellTypes.End:
                EndCell = null;
                YellowTilesCount--;
                break;
            case CellTypes.Wall:
                BlackTilesCount--;
                break;
            case CellTypes.Floor:
                WhiteTilesCount--;
                break;
            case CellTypes.Empty:
                GrayTilesCount--;
                break;
            default:
                break;
        }

        //Set the new tile and update the maze data
        var newTileBase = EnumToTileBase(newTileType);
        MazeMap.SetTile(pos, newTileBase);
        if (MazeData.CellWithinBounds(pos.x, pos.y))
        {
            var cell = MazeData.CellAtPos(pos.x, pos.y);
            cell.GenerationVisited = IsGeneratedTileType(newTileType);
            cell.Walkable = IsWalkableTile(newTileType);
            cell.CellType = newTileType;
        }

        //Increase new tile count
        switch (newTileType)
        {
            case CellTypes.Start:
                StartCell = MazeData.CellAtPos(pos.x, pos.y);
                GreenTilesCount++;
                break;
            case CellTypes.End:
                EndCell = MazeData.CellAtPos(pos.x, pos.y);
                YellowTilesCount++;
                break;
            case CellTypes.Wall:
                BlackTilesCount++;
                break;
            case CellTypes.Floor:
                WhiteTilesCount++;
                break;
            case CellTypes.Empty:
                GrayTilesCount++;
                break;
            default:
                break;
        }

        //Update visual text counter
        BlackTileCounter.text = BlackTilesCount.ToString();
        GreenTileCounter.text = GreenTilesCount.ToString();
        WhiteTileCounter.text = WhiteTilesCount.ToString();
        YellowTileCounter.text = YellowTilesCount.ToString();
    }

    bool IsGeneratedTileType(CellTypes type)
    {
        switch (type)
        {
            case CellTypes.Start:
                return true;
            case CellTypes.End:
                return true;
            case CellTypes.Wall:
                return true;
            case CellTypes.Floor:
                return true;
            case CellTypes.Empty:
                return false;
            case CellTypes.Searching:
                return true;
            case CellTypes.FoundPath:
                return true;
            case CellTypes.None:
                return false;
            default:
                return false;
        }
    }
    public bool IsWalkableTile(CellTypes type)
    {
        switch (type)
        {
            case CellTypes.Start:
                return true;
            case CellTypes.End:
                return true;
            case CellTypes.Wall:
                return false;
            case CellTypes.Floor:
                return true;
            case CellTypes.Empty:
                return false;
            case CellTypes.None:
                return false;
            default:
                return false;
        }
    }
    #endregion

    #region Maze Pathfinding

    public void ToggleRunPathfinding()
    {
        MazeSolvers[CurrentMazeSolver].ToggleMazeSolver();
    }

    public void ResetPathfinding()
    {
        MazeSolvers[CurrentMazeSolver].CancelMazeSolver();
        MazeData.ResetPathfinding();
        UpdateMaze();
    }

    #endregion




    #region Maze Settings

    public void DecreaseMazeSize()
    {
        _currentMazeSizeIndex--;
        if (_currentMazeSizeIndex <= 0)
            _currentMazeSizeIndex = 0;
        MazeData.Size = _mazeSizes[_currentMazeSizeIndex];
        UpdateSizeText();
        ResetMaze();
    }
    public void IncreaseSpeed()
    {
        //If the frequency is maxed out, we cannot go faster
        if (DelayFrenquency >= FastestFrequency)
            return;

        //If the delay is maxed out, increase freqency
        if (DelayTimeMS <= FastestMSSpeed)
        {
            //DelayFrenquency += FrequencyChangeAmount;
            DelayFrenquency *= 2;

            if (DelayFrenquency >= FastestFrequency)
                DelayFrenquency = FastestFrequency;
            if (DelayFrenquency == 1 + FrequencyChangeAmount)
                DelayFrenquency = FrequencyChangeAmount;
            UpdateSpeedText();
            return;

        }
        //Otherwise lower the delay
        //DelayTimeMS -= MSChangeAmount;
        DelayTimeMS /= 2;
        if (DelayTimeMS <= FastestMSSpeed)
            DelayTimeMS = FastestMSSpeed;
        UpdateSpeedText();
        return;

    }
    public void DecreaseSpeed()
    {
        //If the delay time is maxed out, we cannot go slower
        if (DelayTimeMS >= SlowestMSSpeed)
            return;

        //If the frequency is not at lowest , lower it
        if (DelayFrenquency > SlowestFrequency)
        {
            //DelayFrenquency -= FrequencyChangeAmount;
            DelayFrenquency /= 2;
            if (DelayFrenquency <= SlowestFrequency)
                DelayFrenquency = SlowestFrequency;
            UpdateSpeedText();
            return;
        }

        //Otherwise increase the delay
        //DelayTimeMS += MSChangeAmount;
        DelayTimeMS *= 2;
        if (DelayTimeMS >= SlowestMSSpeed)
            DelayTimeMS = SlowestMSSpeed;
        if (DelayTimeMS == 1 + MSChangeAmount)
            DelayTimeMS = MSChangeAmount;
        UpdateSpeedText();
        return;
    }
    void UpdateSpeedText()
    {
        SpeedText.text = Math.Round((100 * ((float)SlowestMSSpeed / DelayTimeMS) * DelayFrenquency / FastestFrequency), 1).ToString() + "x";
    }
    void UpdateSizeText()
    {
        SizeText.text = _mazeSizes[_currentMazeSizeIndex].ToString();
    }
    public void ToggleUseRandomSeed()
    {
        UseRandomSeed = !UseRandomSeed;
    }
    public void IncreaseMazeSize()
    {
        _currentMazeSizeIndex++;
        if (_currentMazeSizeIndex >= _mazeSizes.Length)
            _currentMazeSizeIndex = _mazeSizes.Length - 1;
        MazeData.Size = _mazeSizes[_currentMazeSizeIndex];
        UpdateSizeText();
        ResetMaze();
    }

    #endregion



}

public static class Extensions
{
    public static void Shuffle<T>(this IList<T> list, System.Random rng)
    {
        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            T value = list[k];
            list[k] = list[n];
            list[n] = value;
        }
    }
}


[System.Serializable]
public class Maze
{
    public int Size = 100;
    public const int MAXMAZESIZE = 256;
    public const int DEFAULTMAZESIZE = 100;
    public Cell[,] Cells;

    public Maze()
    {
        Size = DEFAULTMAZESIZE;
        Cells = new Cell[Size, Size];
        for (int x = 0; x < Size; x++)
        {
            for (int y = 0; y < Size; y++)
            {
                Cells[x, y] = new Cell(x, y);
            }
        }
    }

    public Maze(int size)
    {
        Size = size;
        Cells = new Cell[size, size];
        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                Cells[x, y] = new Cell(x, y);
            }
        }
    }
    public void ResetMaze()
    {
        if (Size <= 0)
            Size = DEFAULTMAZESIZE;
        Cells = new Cell[Size, Size];
        for (int x = 0; x < Size; x++)
        {
            for (int y = 0; y < Size; y++)
            {
                Cells[x, y] = new Cell(x, y);
            }
        }
    }

    public Cell CellAtPos(Vector2Int pos)
    {
        if (pos.x < 0 || pos.y < 0 || pos.x >= Size || pos.y >= Size)
            return null;
        return Cells[pos.x, pos.y];
    }

    public Cell CellAtPos(int x, int y)
    {
        if (x < 0 || y < 0 || x >= Size || y >= Size)
            return null;
        return Cells[x, y];
    }

    public bool TryGetCellAtPos(int x, int y, out Cell cell)
    {
        cell = null;
        if (!CellWithinBounds(x, y))
            return false;
        cell = Cells[x, y];
        return true;
    }

    public bool CellHasWalkableNeighbor(int x, int y, System.Random rand)
    {
        List<int> directions = new() { 0, 1, 2, 3 };
        directions = directions.OrderBy(d => rand.Next()).ToList();

        foreach (var direction in directions)
        {
            int newX = x;
            int newY = y;

            switch (direction)
            {
                case 0:
                    newX++; // Move right
                    break;
                case 1:
                    newY++; // Move up
                    break;
                case 2:
                    newX--; // Move left
                    break;
                case 3:
                    newY--; //Move down
                    break;
            }

            if (CellWithinBounds(newX, newY) && CellAtPos(newX, newY).Walkable)
            {
                return true;
            }
        }
        return false;
    }

    public bool CellWithinBounds(int x, int y)
    {
        return x >= 0 && y >= 0 && x < Size && y < Size;
    }

    public bool PathfindableNeighbors(int x, int y, out List<Cell> cells)
    {
        cells = null;
        if (!CellWithinBounds(x, y))
            return false;
        cells = new();
        int[] xPositions = { 1, -1, 0, 0 };
        int[] yPositions = { 0, 0, -1, 1 };

        for (int i = 0; i < xPositions.Length; i++)
        {
            if (!TryGetCellAtPos(x + xPositions[i], y + yPositions[i], out var cell))
                continue;
            if (cell.PathfindingVisited || !cell.Walkable)
                continue;
            cells.Add(cell);
        }
        return cells.Count > 0;
    }

    internal void ResetPathfinding()
    {
        foreach (var cell in Cells)
        {
            if (!cell.PathfindingVisited || cell.PathfindingValue == -1)
                continue;
            cell.PathfindingVisited = false;
            cell.PathfindingValue = -1;
            cell.CellType = CellTypes.Floor;
        }
    }
}
[System.Serializable]
public class Cell
{
    public override bool Equals(object obj)
    {
        if (obj is not Cell)
            return false;
        var other = obj as Cell;
        return other.Position.Equals(this.Position);
    }
    public Cell PrevRouteCell;
    public int xPos;
    public int yPos;
    public bool GenerationVisited = false;
    public bool Walkable = false;
    public bool PathfindingVisited = false;
    public int PathfindingValue = -1;
    public bool AttemptedToBreakWall = false;
    public CellTypes CellType = CellTypes.Empty;
    public Cell()
    {
    }
    public Cell(int x, int y)
    {
        xPos = x;
        yPos = y;
    }

    public Vector2Int Position
    {
        get { return new Vector2Int(xPos, yPos); }
    }

    public Vector3Int Position3
    {
        get { return new Vector3Int(xPos, yPos, 0); }
    }
}


/*

    

    public void CheckAndSetBorders(Vector2Int pos, Tilemap tileMap, TileBase blackTile)
    {
        //After we move, the previous choices are now set as unwalkable

        //A maze cannot have a 2x2 of available space, so let's check if there are any
        //We know a 2x2 space is going to happen if a corner is visited
        /* Bottom Left
               x x x
               0 0 x
               0 x x
        */
//Open cell at x,y - open cell at 2x+1,2y+1

/*
var testX = 2 * pos.x + 1;
var testY =  2 * pos.y + 1;
if (CellWithinBounds(testX, testY))
{
     Cells[testX, testY].Visited = true;
     Cells[testX, testY].Walkable = true;
    tileMap.SetTile(new Vector3Int(testX, testY, 0), whiteTile);
}
//if Wall between x,y and x,y+1 - wall at 2x +1, 2(y+1))

//if Wall between x,y and x+1,y - wall at 2(x +1), 2y+1

//all corner cells (2x,2y) are walls

var BottomLeftCorner = pos + Vector2Int.left + Vector2Int.down;
var BottomRightCorner = pos + Vector2Int.right + Vector2Int.down;
var TopLeftCorner = pos + Vector2Int.left + Vector2Int.up;
var TopRightCorner = pos + Vector2Int.right + Vector2Int.up;

TestPosition(BottomLeftCorner, Vector2Int.right, Vector2Int.up);
TestPosition(BottomRightCorner, Vector2Int.left, Vector2Int.up);
TestPosition(TopLeftCorner, Vector2Int.right, Vector2Int.down);
TestPosition(TopRightCorner, Vector2Int.left, Vector2Int.down);


void TestPosition(Vector2Int pos, Vector2Int direction1, Vector2Int direction2)
{
    if (!CellWithinBounds(pos.x, pos.y))
        return;

    if (!Cells[pos.x, pos.y].GenerationVisited)
        return;


    var currentTestPos = pos + direction1;
    if (CellWithinBounds(currentTestPos.x, currentTestPos.y))
    {
        if (!Cells[currentTestPos.x, currentTestPos.y].GenerationVisited)
        {
            Cells[currentTestPos.x, currentTestPos.y].GenerationVisited = true;
            Cells[currentTestPos.x, currentTestPos.y].CellType = CellTypes.Wall;
            tileMap.SetTile(new Vector3Int(currentTestPos.x, currentTestPos.y, 0), blackTile);

        }
    }

    currentTestPos = pos + direction2;
    if (CellWithinBounds(currentTestPos.x, currentTestPos.y))
    {
        if (!Cells[currentTestPos.x, currentTestPos.y].GenerationVisited)
        {
            Cells[currentTestPos.x, currentTestPos.y].GenerationVisited = true;
            Cells[currentTestPos.x, currentTestPos.y].CellType = CellTypes.Wall;
            tileMap.SetTile(new Vector3Int(currentTestPos.x, currentTestPos.y, 0), blackTile);
        }
    }

}

}

public async void BuildMazeHuntAndKill()
{
if (!MazeCanGenerate)
    return;
ResetMaze();
MazeCanGenerate = false;
MazePaused = false;
//Get Randomizer
System.Random rand = UseRandomSeed ? new System.Random(UnityEngine.Random.Range(0, 48515651).GetHashCode()) : new System.Random(Seed.GetHashCode());
//Initial Walk
var initialCell = MazeData.CellAtPos(new Vector2Int(rand.Next(0, MazeData.Size), rand.Next(0, MazeData.Size)));
initialCell.GenerationVisited = true;

if (MazePaused)
{
    await WaitUntilPlaying();
    if (MazeCanGenerate)
        return;
}
await StartWalk(initialCell);

//Set loop protection
int loops = 0;
while (loops < 100 * MazeData.Size)
{
    if (MazePaused)
    {
        await WaitUntilPlaying();
        if (MazeCanGenerate)
            return;
    }
    var nextCell = findUnvisitedCell();
    if (nextCell == null)
        break;

    nextCell.GenerationVisited = true;
    MazeData.CheckAndSetBorders(nextCell.Position, MazeMap, BlackTile);
    OnTileChange?.Invoke();
    await StartWalk(nextCell);
    if (MazeCanGenerate)
        return;
    loops++;
    if (loops % DelayFrenquency == 0)
        await Task.Delay(DelayTimeMS);
}
fillInRemainingSpots();
MazeCanGenerate = true;
void fillInRemainingSpots()
{
    for (int x = 0; x < MazeData.Size; x++)
    {
        for (int y = 0; y < MazeData.Size; y++)
        {
            var cell = MazeData.CellAtPos(new Vector2Int(x, y));
            if (cell.GenerationVisited)
                continue;
            PlaceTile(cell.Position3, BlackTile);

        }
    }

    //PlaceEdgeCells();
    void PlaceEdgeCells()
    {
        List<(int, int)> cells = new();
        //place exit and entrance

        //Get all edge cells
        for (int x = -1; x < MazeData.Size + 1; x++)
        {
            for (int y = -1; y < MazeData.Size + 1; y++)
            {
                if (x == -1 || x == MazeSize)
                {
                    cells.Add((x, y));
                    continue;
                }
                if (y == -1 || y == MazeSize)
                {
                    cells.Add((x, y));
                    continue;
                }
            }
        }

        //remove edge cells not connected to a path

        if (cells.Count < 2)
            return;
        var exit = cells[rand.Next(0, cells.Count)];
        var entrance = cells[rand.Next(0, cells.Count)];
        while (exit == entrance)
        {
            entrance = cells[rand.Next(0, cells.Count)];
        }

        PlaceTile(new Vector3Int(exit.Item1, exit.Item2, 0), YellowTile);
        PlaceTile(new Vector3Int(entrance.Item1, entrance.Item2, 0), YellowTile);
    }


}

//UpdateWholeMaze();
Cell findUnvisitedCell()
{
    for (int x = 0; x < MazeData.Size; x++)
    {
        for (int y = 0; y < MazeData.Size; y++)
        {
            var cell = MazeData.CellAtPos(new Vector2Int(x, y));
            if (cell.GenerationVisited)
                continue;
            if (MazeData.CellHasWalkableNeighbor(x, y, rand))
                return cell;

            //MazeMap.SetTile(cell.Position3, BlackTile);
        }
    }
    return null;
}

async Task WaitUntilPlaying()
{
    while (MazePaused)
    {
        await Task.Delay(5);
    }
}

async Task StartWalk(Cell startingCell)
{
    PlaceTile(startingCell.Position3, WhiteTile);
    startingCell.GenerationVisited = true;
    startingCell.Walkable = true;
    int loops = 0;
    while (loops < 30 * MazeData.Size)
    {
        if (MazePaused)
        {
            await WaitUntilPlaying();
            if (MazeCanGenerate)
                return;
        }
        if (!RandomNeighbor(startingCell.Position, out var chosenCell))
            break;
        chosenCell.GenerationVisited = true;
        chosenCell.Walkable = true;
        startingCell = chosenCell;
        PlaceTile(startingCell.Position3, WhiteTile);

        //MazeData.CheckAndSetBorders(randCell.Position, MazeMap, BlackTile);
        loops++;
        if (loops % DelayFrenquency == 0)
            await Task.Delay(DelayTimeMS);
    }
}

bool RandomNeighbor(Vector2Int pos, out Cell chosenCell)
{

    chosenCell = null;


    int[] directionX = { 1, 0, -1, 0 };
    int[] directionY = { 0, 1, 0, -1 };

    List<int> directions = new List<int> { 0, 1, 2, 3 };
    directions = directions.OrderBy(d => rand.Next()).ToList();
    var orginalCell = MazeData.CellAtPos(pos);
    foreach (var direction in directions)
    {
        int newX = pos.x + directionX[direction];
        int newY = pos.y + directionY[direction];
        if (MazeData.CellWithinBounds(newX, newY))
        {
            var foundCell = MazeData.CellAtPos(newX, newY);
            if (foundCell.GenerationVisited)
                continue;
            BlackoutCells(orginalCell.Position, direction);
            chosenCell = foundCell;
            return true;
        }
    }
    return false;


    void BlackoutCells(Vector2Int startingPos, int startingDirection)
    {

        List<int> inverseDirections = new List<int> { 2, 3, 0, 1 };
        List<int> directions = new List<int> { 0, 1, 2, 3 };
        foreach (var direction in directions)
        {
            if (direction == startingDirection)
                continue;
            if (direction == inverseDirections[startingDirection])
                continue;
            int newX = startingPos.x + directionX[direction];
            int newY = startingPos.y + directionY[direction];
            if (!MazeData.CellWithinBounds(newX, newY))
                continue;
            var cell = MazeData.CellAtPos(new Vector2Int(newX, newY));
            if (cell.GenerationVisited)
                continue;
            cell.GenerationVisited = true;
            PlaceTile(cell.Position3, BlackTile);
        }

    }
}

}


private void UpdateTileCounter()
    {
        var bounds = MazeMap.cellBounds;
        BlackTilesCount = 0;
        WhiteTilesCount = 0;
        GreenTilesCount = 0;
        YellowTilesCount = 0;
        GrayTilesCount = 0;

        for (int x = bounds.min.x; x < bounds.max.x; x++)
        {
            for (int y = bounds.min.y; y < bounds.max.y; y++)
            {
                var foundTile = MazeMap.GetTile(new Vector3Int(x, y, 0));
                if (foundTile == null)
                    continue;
                var tileName = TileBaseToEnum(foundTile);
                switch (tileName)
                {
                    case CellTypes.Start:
                        GreenTilesCount++;
                        break;
                    case CellTypes.End:
                        YellowTilesCount++;
                        break;
                    case CellTypes.Wall:
                        BlackTilesCount++;
                        break;
                    case CellTypes.Floor:
                        WhiteTilesCount++;
                        break;
                    case CellTypes.Empty:
                        GrayTilesCount++;
                        break;
                    default:
                        break;
                }
            }
        }

        BlackTileCounter.text = BlackTilesCount.ToString();
        GreenTileCounter.text = GreenTilesCount.ToString();
        WhiteTileCounter.text = WhiteTilesCount.ToString();
        YellowTileCounter.text = YellowTilesCount.ToString();
    }

*/