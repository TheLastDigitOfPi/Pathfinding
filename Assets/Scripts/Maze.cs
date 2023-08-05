using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.UIElements;

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

    public bool GeneratableNeighbors(Cell cell, out List<Cell> neighbors, bool random = false)
    {
        neighbors = new();

        int[] xPositions = { 1, -1, 0, 0 };
        int[] yPositions = { 0, 0, -1, 1 };
        
        for (int i = 0; i < xPositions.Length; i++)
        {
            if (!TryGetCellAtPos(cell.xPos + xPositions[i], cell.yPos + yPositions[i], out var checkCell))
                continue;
            if (checkCell.GenerationVisited)
                continue;
            neighbors.Add(checkCell);
        }

        if (random)
        {
            var seed = MazeHandler.Instance.Seed;
            System.Random rand;
            if(!MazeHandler.Instance.UseRandomSeed)
                rand = new(seed.GetHashCode());
            else
                rand = new(UnityEngine.Random.Range(45,4545451).GetHashCode());
            neighbors = neighbors.OrderBy(d => rand.Next()).ToList();
        }

        return neighbors.Count > 0;
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
            if ((cell.PathfindingVisited || !cell.Walkable) && cell.CellType != CellTypes.End)
                continue;
            cells.Add(cell);
        }
        return cells.Count > 0;
    }
    
    internal void ResetPathfinding()
    {
        foreach (var cell in Cells)
        {
            if (cell.CellType == CellTypes.Start || cell.CellType == CellTypes.End)
            {
                cell.SetCellData(cell.CellType, true);
                continue;
            }

            if (Cell.IsPathfindingTile(cell.CellType))
                cell.SetCellData(CellTypes.Floor, true);
        }
    }
}
