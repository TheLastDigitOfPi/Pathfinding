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
    public int xPos {get; private set;}
    public int yPos {get; private set;}

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
    /// <param name="ResetCell">Should the cell reset it's pathfinding data</param>
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

    /// <summary>
    /// Return if this tile type is one that can be generated
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    public static bool IsGeneratedTileType(CellTypes type)
    {
        return type switch
        {
            CellTypes.Start => true,
            CellTypes.End => true,
            CellTypes.Wall => true,
            CellTypes.Floor => true,
            CellTypes.NotGenerated => false,
            CellTypes.Searching => true,
            CellTypes.FoundPath => true,
            CellTypes.None => false,
            _ => false,
        };
    }
    
    /// <summary>
    /// Returns if the tile type is a pathfinding tile
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    public static bool IsPathfindingTile(CellTypes type)
    {
        return type switch
        {
            CellTypes.Searching => true,
            CellTypes.FoundPath => true,
            CellTypes.Start => true,
            _ => false,
        };
    }
    /// <summary>
    /// Returns if the tile type is able to be navigated on
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    public static bool IsWalkableTile(CellTypes type)
    {
        return type switch
        {
            CellTypes.Start => true,
            CellTypes.End => true,
            CellTypes.Wall => false,
            CellTypes.Floor => true,
            CellTypes.NotGenerated => false,
            CellTypes.None => false,
            CellTypes.Searching => true,
            CellTypes.FoundPath => true,
            _ => false,
        };
    }

    public override int GetHashCode()
    {
        return base.GetHashCode();
    }
}