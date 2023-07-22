using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine.XR;
using static UnityEditor.PlayerSettings;
using UnityEngine.Tilemaps;
using System.Runtime.InteropServices;
using System;

public class HuntAndKIllGenerator : MazeGenerator
{
    int[] directionX = { 1, 0, -1, 0 };
    int[] directionY = { 0, 1, 0, -1 };

    public override void ResetMazeGeneration()
    {
        IsPaused = false;
        IsRunning = false;
    }

    async Task WaitUntilPlaying()
    {
        while (IsPaused)
        {
            await Task.Delay(3);
            if (!IsRunning)
                return;
        }
    }

    public override async void ToggleMazeGeneration()
    {
        if (IsRunning)
        {
            IsPaused = !IsPaused;
            return;
        }
        IsRunning = true;

        var handler = MazeHandler.Instance;
        bool UseRandomSeed = handler.UseRandomSeed;
        //Get Randomizer
        var seed = handler.Seed;

        System.Random rand = UseRandomSeed ? new System.Random(UnityEngine.Random.Range(0, 48515651).GetHashCode()) : new System.Random(seed.GetHashCode());

        //Main Generation Loop
        var MazeData = handler.MazeData;
        var currentCell = MazeData.CellAtPos(new Vector2Int(rand.Next(0, MazeData.Size), rand.Next(0, MazeData.Size)));
        int loops = 0;
        while (true)
        {
            if (IsPaused)
            {
                await WaitUntilPlaying();
                if (!IsRunning)
                    return;
            }
            //Walk with cell
            await StartWalk(currentCell, 0);

            //Find new cell after walk, return if none found
            if (!TryFindUnvisitedCell(out var cell))
                break;
            currentCell = cell;
            loops++;
            if (loops % handler.DelayFrenquency == 0)
                await Task.Delay(handler.DelayTimeMS);
        }
        fillInRemainingSpots();
        Debug.Log("Finished Making Maze");
        void fillInRemainingSpots()
        {
            for (int x = 0; x < MazeData.Size; x++)
            {
                for (int y = 0; y < MazeData.Size; y++)
                {
                    var cell = MazeData.CellAtPos(new Vector2Int(x, y));
                    if (cell.GenerationVisited)
                        continue;
                    handler.PlaceTile(CellTypes.Wall, cell.Position3);
                }
            }
            IsRunning = false;
            onMazeGenerationComplete?.Invoke();
        }

        async Task StartWalk(Cell startingCell, int currentItteration)
        {
            handler.PlaceTile(CellTypes.Floor, startingCell.Position3);
            startingCell.GenerationVisited = true;
            if (IsPaused)
            {
                await WaitUntilPlaying();
                if (!IsRunning)
                    return;
            }
            if (!RandomNeighbor(startingCell.Position, out var newCell))
                return;
            //CheckAndSetBorders(newCell);
            if (currentItteration % handler.DelayFrenquency == 0)
                await Task.Delay(handler.DelayTimeMS);
            await StartWalk(newCell, currentItteration++);
        }


        bool CanBreakWall(Cell wallToBreak)
        {
            if (wallToBreak.AttemptedToBreakWall)
                return false;

            //We can break a wall if it will not result in a 2x2 walkable cell


            int[] checkX = { -1, -1, 1, 1 };
            int[] checkY = { -1, 1, -1, 1 };

            //To see if it will be a 2x2 cell, lets check the 3x3 grid it occupies
            /*
              
                Not valid because top left produces 2x2
                     W W W 
                     W B B 
                     W B B

                Is valid

                     W W W 
                     B B B 
                     W B B
             */


            //Check each corner of the 3x3 and if all 4 corner spots pass the test then it can be broken

            for (int i = 0; i < checkX.Length; i++)
            {
                if (!ValidCorner(wallToBreak.xPos + checkX[i], wallToBreak.yPos + checkY[i], i))
                    return false;
            }
            return true;

            bool ValidCorner(int x, int y, int index)
            {
                if (!MazeData.TryGetCellAtPos(x, y, out var cell))
                    return true;

                int dir1 = -1 * checkX[index];
                int dir2 = -1 * checkY[index];

                if (!MazeData.TryGetCellAtPos(x + dir1, y, out var cell1))
                    return false;
                if (!MazeData.TryGetCellAtPos(x, y + dir2, out var cell2))
                    return false;

                //Will return valid as long as at least 1 of the corner spots is unwalkable

                return !cell1.Walkable || !cell2.Walkable || !cell.Walkable;
            }

        }

        void CheckAndSetBorders(Cell nextCell)
        {
            int[] checkX = { -1, -1, 1, 1 };
            int[] checkY = { -1, 1, -1, 1 };

            var startingPosX = nextCell.Position.x;
            var startingPosY = nextCell.Position.y;

            for (int i = 0; i < checkX.Length; i++)
            {
                checkCorner(startingPosX + checkX[i], startingPosY + checkX[i], i);
            }

            void checkCorner(int posX, int posY, int index)
            {
                if (!MazeData.TryGetCellAtPos(posX, posY, out var cell))
                    return;
                if (!cell.Walkable)
                    return;

                int dir1 = -1 * checkX[index];
                int dir2 = -1 * checkY[index];

                //Check neighboring cell
                if (!MazeData.TryGetCellAtPos(posX + dir1, posY, out var cell1))
                    return;
                if (!MazeData.TryGetCellAtPos(posX, posY + dir2, out var cell2))
                    return;
                if (cell1.Walkable)
                {
                    cell2.GenerationVisited = true;
                    handler.PlaceTile(CellTypes.Wall, cell2.Position3);
                    return;
                }
                if (cell2.Walkable)
                {
                    cell1.GenerationVisited = true;
                    handler.PlaceTile(CellTypes.Wall, cell1.Position3);
                    return;
                }
            }

        }
        bool RandomNeighbor(Vector2Int pos, out Cell chosenCell)
        {
            chosenCell = null;

            List<int> directions = new List<int> { 0, 1, 2, 3 };
            directions = directions.OrderBy(d => rand.Next()).ToList();
            var orginalCell = MazeData.CellAtPos(pos);
            foreach (var direction in directions)
            {
                int newX = pos.x + directionX[direction];
                int newY = pos.y + directionY[direction];
                if (MazeData.TryGetCellAtPos(newX, newY, out var foundCell))
                {
                    if (foundCell.GenerationVisited)
                    {
                        continue;
                    }

                    BlackoutCells(orginalCell.Position, direction);
                    chosenCell = foundCell;
                    return true;
                }
            }
            return false;
        }

        bool TryFindUnvisitedCell(out Cell foundCell)
        {
            foundCell = null;
            for (int x = 0; x < MazeData.Size; x++)
            {
                for (int y = 0; y < MazeData.Size; y++)
                {
                    var cell = MazeData.CellAtPos(new Vector2Int(x, y));
                    if (cell.GenerationVisited)
                    {
                        continue;
                        //Check if we can break wall
                        if (cell.CellType == CellTypes.Wall)
                        {
                            if (CanBreakWall(cell))
                            {
                                cell.AttemptedToBreakWall = true;
                                foundCell = cell;
                                return true;
                            }
                            cell.AttemptedToBreakWall = true;
                        }
                    }
                    if (MazeData.CellHasWalkableNeighbor(x, y, rand))
                    {
                        foundCell = cell;
                        return true;
                    }
                }
            }
            return false;
        }

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
                handler.PlaceTile(CellTypes.Wall, cell.Position3);
            }

        }





    }


}