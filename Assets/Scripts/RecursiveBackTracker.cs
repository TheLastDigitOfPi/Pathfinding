using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Cysharp.Threading.Tasks;

public class RecursiveBackTracker : MazeGenerator
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

        //Get Randomizer
        var rand = handler.GetRandomizer();

        if (!GetRandomStartingCell(rand, out Cell currentCell))
            return;

        PlaceForcedWalls();
        IsRunning = true;

        Stack<Cell> cells = new();
        int loops = 0;
        while (loops < MazeData.Size * 1000)
        {
            if (IsPaused || !IsRunning)
            {
                if (!IsRunning)
                    return;
                await WaitUntilPlaying();
                if (!IsRunning)
                    return;
            }

            handler.PlaceTile(CellTypes.Floor, currentCell.Position3);

            //Check if our cell has any neighbors it can visit
            if (!MoveToRandomNeighbor(currentCell, rand, out Cell foundCell))
            {
                //if it doesn't, go back to the previous cell
                if (!cells.TryPop(out Cell prevCell))
                {
                    FillInRemainingSpots();
                    //if there is no previous cell, check that we have finished and there are no more ungenerated spots
                    if (TryFindUngeneratedCells(out var ungeneratedCells))
                    {
                        currentCell = ungeneratedCells[0];
                        continue;
                    }
                    if (TryBreakWallGroups())
                    {
                        if (TryFindUngeneratedCells(out var newUngeneratedCells))
                        {
                            currentCell = newUngeneratedCells[0];
                            continue;
                        }
                    }
                    break;
                }
                //otherwise our prev cell becomes our new cell
                currentCell = prevCell;
                continue;
            }

            cells.Push(currentCell);
            currentCell = foundCell;
            loops++;

            if (loops % handler.DelayFrenquency == 0)
                await UniTask.Delay(handler.DelayTimeMS);

        }

    }
}