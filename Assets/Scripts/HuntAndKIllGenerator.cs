using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Linq;

public class HuntAndKIllGenerator : MazeGenerator
{
    public override async void ToggleMazeGeneration()
    {
        if (IsRunning)
        {
            IsPaused = !IsPaused;
            return;
        }
        IsRunning = true;

        //Setup
        var handler = MazeHandler.Instance;
        var MazeData = handler.MazeData;
        var rand = handler.GetRandomizer();
        if (!GetRandomStartingCell(rand, out Cell currentCell))
            return;

        PlaceForcedWalls();

        //Main loop
        int loops = 0;
        while (loops < MazeData.Size * 200)
        {
            //Walk with cell
            await StartWalk(currentCell, 0);

            //Check if paused
            if (IsPaused || !IsRunning)
            {
                if (!IsRunning)
                    return;
                await WaitUntilPlaying();
                if (!IsRunning)
                    return;
            }

            //Find new cell after walk, return if none found
            if (!TryFindFirstUnvisitedCell(out var cell))
            {
                FillInRemainingSpots();
                if (!TryFindFirstUnvisitedCell(out var newCell))
                    break;
                currentCell = newCell;
                continue;

            }
            currentCell = cell;
            loops++;
            if (loops % handler.DelayFrenquency == 0)
                await UniTask.Delay(handler.DelayTimeMS);
        }

        IsRunning = false;
        onMazeGenerationComplete?.Invoke();
        Debug.Log("Finished Making Maze");


        async UniTask StartWalk(Cell startingCell, int currentItteration)
        {
            if (IsPaused || !IsRunning)
            {
                if (!IsRunning)
                    return;
                await WaitUntilPlaying();
                if (!IsRunning)
                    return;
            }
            handler.PlaceTile(CellTypes.Floor, startingCell.Position3);

            if (!MoveToRandomNeighbor(startingCell, rand, out var newCell))
                return;

            if (currentItteration % handler.DelayFrenquency == 0)
                await UniTask.Delay(handler.DelayTimeMS);

            currentItteration++;
            await StartWalk(newCell, currentItteration);
        }


        bool TryFindFirstUnvisitedCell(out Cell foundCell)
        {
            foundCell = null;
            for (int x = 0; x < MazeData.Size; x++)
            {
                for (int y = 0; y < MazeData.Size; y++)
                {
                    var cell = MazeData.CellAtPos(new Vector2Int(x, y));
                    //If a corner or a walkable cell, skip it
                    if (AutoWallSpot(x, y) || cell.Walkable)
                        continue;

                    //If a wall see if we can break it
                    if (cell.CellType == CellTypes.Wall && CanBreakWallGroup(cell))
                        continue;

                    if (cell.GenerationVisited)
                        continue;

                    if (MazeData.CellHasWalkableNeighbor(x, y, rand))
                    {
                        foundCell = cell;
                        return true;
                    }
                }
            }
            return false;
        }


    }

}