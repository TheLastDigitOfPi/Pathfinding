using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using static UnityEditor.PlayerSettings;

public abstract class MazeGenerator : MonoBehaviour
{
    protected int[] directionX = { 1, 0, -1, 0 };
    protected int[] directionY = { 0, 1, 0, -1 };
    public event Action OnMazeGenerationComplete { add { onMazeGenerationComplete += value; } remove { onMazeGenerationComplete -= value; } }
    protected Action onMazeGenerationComplete;
    public bool IsRunning = false;
    public bool IsPaused = false;
    public virtual void ResetMazeGeneration()
    {
        IsPaused = false;
        IsRunning = false;
    }
    public abstract void ToggleMazeGeneration();
    /// <summary>
    /// Determines if a coordinate represnts a wall spot that is required
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <returns></returns>
    protected bool AutoWallSpot(int x, int y)
    {
        return x % 2 == 1 && y % 2 == 1;
    }
    /// <summary>
    /// Check if a cell is on the edge of the maze
    /// </summary>
    /// <param name="cell"></param>
    /// <returns>Returns true if cell is on edge of maze</returns>
    protected bool CellOnEdge(Cell cell)
    {
        var handler = MazeHandler.Instance;
        var mazeData = handler.MazeData;
        return cell.xPos == 0 || cell.yPos == 0 || cell.xPos == mazeData.Size - 1 || cell.yPos == mazeData.Size - 1;
    }
    /// <summary>
    /// Fills in any spots in the maze that have not been generated with walls
    /// </summary>
    protected void FillInRemainingSpots()
    {
        var handler = MazeHandler.Instance;
        var mazeData = handler.MazeData;
        for (int x = 0; x < mazeData.Size; x++)
        {
            for (int y = 0; y < mazeData.Size; y++)
            {
                var cell = mazeData.CellAtPos(new Vector2Int(x, y));
                if (cell.GenerationVisited)
                    continue;
                handler.PlaceTile(CellTypes.Wall, cell.Position3);
            }
        }
    }

    /// <summary>
    /// Task used to stop execution until the generated is unpaused or canceled
    /// </summary>
    /// <returns></returns>
    protected async UniTask WaitUntilPlaying()
    {
        while (IsPaused)
        {
            await UniTask.Delay(3);
            if (!IsRunning)
                return;
        }
    }
    /// <summary>
    /// Places walls on maze that are required. Cells are required if both coordinates are multiples of 2
    /// </summary>
    protected void PlaceForcedWalls()
    {
        var handler = MazeHandler.Instance;
        var mazeData = handler.MazeData;
        foreach (var cell in mazeData.Cells)
        {
            if (AutoWallSpot(cell.xPos, cell.yPos) && !CellOnEdge(cell))
                handler.PlaceTile(CellTypes.Wall, cell.Position3);
        }
    }

    /// <summary>
    /// Attempt to find a random starting cell and return it in out parameter
    /// </summary>
    /// <param name="rand"></param>
    /// <param name="FoundCell">The random cell found</param>
    /// <returns>Returns true if found a random starting cell</returns>
    protected bool GetRandomStartingCell(System.Random rand, out Cell FoundCell)
    {
        FoundCell = null;
        var handler = MazeHandler.Instance;
        var mazeData = handler.MazeData;
        List<Cell> unvisitedCells = new();
        foreach (var cell in mazeData.Cells)
        {
            if (!cell.GenerationVisited)
                unvisitedCells.Add(cell);
        }

        if (unvisitedCells.Count == 0)
            return false;
        unvisitedCells = unvisitedCells.OrderBy(a => rand.Next()).ToList();
        int checkCell = 0;
        while (checkCell < unvisitedCells.Count)
        {
            FoundCell = unvisitedCells[checkCell];
            checkCell++;
            if (AutoWallSpot(FoundCell.xPos, FoundCell.yPos))
                continue;
            break;
        }
        return true;
    }

    /// <summary>
    /// Place walls based on the direction that the maze is being generated. Will place walls on sides perpendicular to the current direction
    /// </summary>
    /// <param name="startingPos"></param>
    /// <param name="startingDirection"></param>
    protected void PlaceDirectionalWalls(Vector2Int startingPos, int startingDirection)
    {
        var handler = MazeHandler.Instance;
        var mazeData = handler.MazeData;
        List<int> inverseDirections = new List<int> { 2, 3, 0, 1 };
        List<int> directions = new List<int> { 0, 1, 2, 3 };

        directions.Remove(startingDirection);
        directions.Remove(inverseDirections[startingDirection]);
        foreach (var direction in directions)
        {
            int newX = startingPos.x + directionX[direction];
            int newY = startingPos.y + directionY[direction];
            if (!mazeData.TryGetCellAtPos(newX, newY, out Cell cell))
                continue;
            if (cell.GenerationVisited)
                continue;


            handler.PlaceTile(CellTypes.Wall, cell.Position3);
        }
    }

    /// <summary>
    /// Attempt to find any ungenerated cells in the maze and return any found
    /// </summary>
    /// <param name="ungeneratedCells"></param>
    /// <returns>Returns list of cells that are currently ungenerated</returns>
    protected bool TryFindUngeneratedCells(out List<Cell> ungeneratedCells)
    {
        var handler = MazeHandler.Instance;
        var mazeData = handler.MazeData;
        ungeneratedCells = new();

        foreach (var cell in mazeData.Cells)
        {
            if (cell.CellType == CellTypes.NotGenerated)
                ungeneratedCells.Add(cell);
        }
        return ungeneratedCells.Count > 0;
    }

    /// <summary>
    /// Search the entire maze and attempt to break any groups of walls that don't produce the desired maze wall density
    /// </summary>
    /// <returns>Returns true if broken any walls</returns>
    protected bool TryBreakWallGroups()
    {
        var handler = MazeHandler.Instance;
        var mazeData = handler.MazeData;

        int groupsBroken = 0;
        for (int x = 0; x < mazeData.Size; x++)
        {
            for (int y = 0; y < mazeData.Size; y++)
            {
                if (!mazeData.TryGetCellAtPos(x, y, out Cell cell))
                    continue;
                //If a corner or a walkable cell, skip it
                if (AutoWallSpot(x, y) || cell.Walkable)
                    continue;

                //If a wall see if we can break it
                if (cell.CellType == CellTypes.Wall && CanBreakWallGroup(cell))
                    groupsBroken++;
            }
        }
        return groupsBroken > 0;
    }

    /// <summary>
    /// Test if the current wall is part of a breakable wall group. If it is then the group will be broken
    /// </summary>
    /// <param name="wallToCheck"></param>
    /// <returns>Returns true if the wall was found to be part of a wall group</returns>
    protected bool CanBreakWallGroup(Cell wallToCheck)
    {
        //We will check if the wall is part of a 2x2 group

        int[] checkX = { 0, 1, 1 };
        int[] checkY = { 1, 1, 0 };

        var handler = MazeHandler.Instance;
        var mazeData = handler.MazeData;

        for (int i = 0; i < checkX.Length; i++)
        {
            if (!AlsoWall(wallToCheck.xPos + checkX[i], wallToCheck.yPos + checkY[i]))
                return false;
        }

        //Now we convert up to a 3x3 from that point back to empty
        //This allows the generation to go back through and fill in the maze spot
        for (int x = 0; x < 3; x++)
        {
            for (int y = 0; y < 3; y++)
            {
                int testX = wallToCheck.xPos + x;
                int testY = wallToCheck.yPos + y;
                if (AutoWallSpot(testX, testY))
                    continue;
                if (!mazeData.TryGetCellAtPos(testX, testY, out var cell))
                    continue;
                if (cell.CellType == CellTypes.Floor)
                    continue;
                handler.PlaceTile(CellTypes.NotGenerated, cell.Position3);
            }
        }

        return true;

        bool AlsoWall(int x, int y)
        {
            if (!mazeData.TryGetCellAtPos(x, y, out var cell))
                return false;
            return cell.CellType == CellTypes.Wall;
        }

    }

    protected bool MoveToRandomNeighbor(Cell originalCell, System.Random rand, out Cell chosenCell)
    {
        var handler = MazeHandler.Instance;
        var mazeData = handler.MazeData;
        chosenCell = null;
        List<int> directions = new List<int> { 0, 1, 2, 3 };
        directions = directions.OrderBy(d => rand.Next()).ToList();
        foreach (var direction in directions)
        {
            int newX = originalCell.xPos + directionX[direction];
            int newY = originalCell.yPos + directionY[direction];

            if (AutoWallSpot(newX, newY))
                continue;
            if (mazeData.TryGetCellAtPos(newX, newY, out var foundCell))
            {
                if (foundCell.GenerationVisited)
                    continue;

                PlaceDirectionalWalls(originalCell.Position, direction);
                chosenCell = foundCell;
                return true;
            }
        }
        return false;
    }
}
