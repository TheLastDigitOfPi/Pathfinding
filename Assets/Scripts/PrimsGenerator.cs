using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class PrimsGenerator : MazeGenerator
{
    public override async void ToggleMazeGeneration()
    {
        if (IsRunning)
        {
            IsPaused = !IsPaused;
            return;
        }

        var handler = MazeHandler.Instance;
        var MazeData = handler.MazeData;
        var rand = handler.GetRandomizer();

        Cell currentCell = null;
        if (!GetRandomStartingCell(rand, out currentCell))
            return;
        IsRunning = true;
        PlaceForcedWalls();

        Cell prevCell = null;
        int loops = 0;
        Dictionary<Cell, Cell> remainingCells = new();
        remainingCells.Add(currentCell, null);
        while (true)
        {
            if (remainingCells.Count == 0)
            {
                FillInRemainingSpots();
                TryBreakWallGroups();
                if (!TryFindUngeneratedCells(out var cells))
                    break;
                remainingCells.TryAdd(cells[0], null);
            }
            //Get a random cell from our dictionary
            currentCell = remainingCells.ElementAt(rand.Next(0, remainingCells.Count)).Key;
            if (!remainingCells.TryGetValue(currentCell, out prevCell))
                prevCell = null;
            remainingCells.Remove(currentCell);

            if (currentCell.GenerationVisited)
                continue;

            if (IsPaused || !IsRunning)
            {
                if (!IsRunning)
                    return;
                await WaitUntilPlaying();
                if (!IsRunning)
                    return;
            }

            handler.PlaceTile(CellTypes.Floor, currentCell.Position3);

            PlaceWalls();
            FindNextCell();
            if (loops % handler.DelayFrenquency == 0)
                await UniTask.Delay(handler.DelayTimeMS);
            loops++;
        }
        IsRunning = false;
        Debug.Log("Finished generated Prim's Maze");

        void FindNextCell()
        {
            if (!MazeData.GeneratableNeighbors(currentCell, out var cells, random: true))
                return;
            foreach (var cell in cells)
            {
                remainingCells.TryAdd(cell, currentCell);
            }
        }

        void PlaceWalls()
        {
            if (prevCell == null)
                return;
            Vector2Int vectorDirection = currentCell.Position - prevCell.Position;
            int startingDirection = VectorToArrayDirection(vectorDirection);
            PlaceDirectionalWalls(prevCell.Position, startingDirection);
        }

        int VectorToArrayDirection(Vector2Int direction)
        {
            for (int i = 0; i < directionX.Length; i++)
            {
                if (direction.x == directionX[i] && direction.y == directionY[i])
                    return i;
            }
            return 0;
        }

    }
}
