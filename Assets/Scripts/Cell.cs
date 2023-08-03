using UnityEngine;

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
    public bool PathfindingVisited { get { return IsPathfindingTile(CellType); } }
    public int PathfindingValue = -1;
    public bool AttemptedToBreakWall = false;
    public CellTypes CellType = CellTypes.NotGenerated;
    public int PathfindingCost = 1;
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

    /// <summary>
    /// Resets the cell data to the pre-designated data from the cell type
    /// </summary>
    /// <param name="newType"></param>
    public void SetCellData(CellTypes newType, bool ResetCell = false)
    {
        CellType = newType;
        GenerationVisited = IsGeneratedTileType(newType);
        Walkable = IsWalkableTile(newType);
        if (ResetCell)
        {
            PathfindingValue = -1;
            PrevRouteCell = null;
        }
    }



    public static bool IsGeneratedTileType(CellTypes type)
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
            case CellTypes.NotGenerated:
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
    public static bool IsPathfindingTile(CellTypes type)
    {
        switch (type)
        {
            case CellTypes.Searching:
                return true;
            case CellTypes.FoundPath:
                return true;
            case CellTypes.Start:
                return true;
            default:
                return false;
        }
    }
    public static bool IsWalkableTile(CellTypes type)
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
            case CellTypes.NotGenerated:
                return false;
            case CellTypes.None:
                return false;
            case CellTypes.Searching:
                return true;
            case CellTypes.FoundPath:
                return true;
            default:
                return false;
        }
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