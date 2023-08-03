using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Linq;

public class HuntAndKIllGenerator : MazeGenerator
{
    int[] directionX = { 1, 0, -1, 0 };
    int[] directionY = { 0, 1, 0, -1 };

    public override void ResetMazeGeneration()
    {
        IsPaused = false;
        IsRunning = false;
    }

    async UniTask WaitUntilPlaying()
    {
        while (IsPaused)
        {
            await UniTask.Delay(3);
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

        #region Setup
        var handler = MazeHandler.Instance;
        bool UseRandomSeed = handler.UseRandomSeed;
        //Get Randomizer
        var seed = handler.Seed;
        string randSeed = seed;
        if (UseRandomSeed)
        {
            randSeed = UnityEngine.Random.Range(0, 48515651).ToString();
            handler.UpdateRandomSeed(randSeed);
        }
        System.Random rand = new System.Random(randSeed.GetHashCode());

        //Main Generation Loop
        var MazeData = handler.MazeData;
        var currentCell = MazeData.CellAtPos(new Vector2Int(rand.Next(0, MazeData.Size), rand.Next(0, MazeData.Size)));
        while (AutoWallSpot(currentCell.xPos, currentCell.yPos))
        {
            currentCell = MazeData.CellAtPos(new Vector2Int(rand.Next(0, MazeData.Size), rand.Next(0, MazeData.Size)));
        }
        PlaceWallsVersion1(currentCell);


        #endregion


        #region Main Loop

        int loops = 0;
        while (true)
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
            if (!TryFindUnvisitedCell(out var cell))
            {
                fillInRemainingSpots();
                if (!TryFindUnvisitedCell(out var newCell))
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

            if (!RandomNeighbor(startingCell.Position, out var newCell))
                return;
            //CheckAndSetBorders(newCell);
            if (currentItteration % handler.DelayFrenquency == 0)
                await UniTask.Delay(handler.DelayTimeMS);
            currentItteration++;
            await StartWalk(newCell, currentItteration);
        }


        #endregion

        #region Helpful Functions

        bool CellOnEdge(Cell cell)
        {
            return cell.xPos == 0 || cell.yPos == 0 || cell.xPos == MazeData.Size - 1 || cell.yPos == MazeData.Size - 1;
        }
        bool AutoWallSpot(int x, int y)
        {
            return x % 2 == 1 && y % 2 == 1;
        }

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
        }



        void PlaceWallsVersion1(Cell originalCell)
        {
            foreach (var cell in MazeData.Cells)
            {
                if (AutoWallSpot(cell.xPos, cell.yPos) && !CellOnEdge(cell))
                    handler.PlaceTile(CellTypes.Wall, cell.Position3);
            }
        }


        bool CanBreakWallGroup(Cell wallToCheck)
        {
            //We will check if the wall is part of a 2x2 group


            int[] checkX = { 0, 1, 1 };
            int[] checkY = { 1, 1, 0 };

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
                    if (!MazeData.TryGetCellAtPos(testX, testY, out var cell))
                        continue;
                    handler.PlaceTile(CellTypes.NotGenerated, cell.Position3);
                }
            }

            return true;

            bool AlsoWall(int x, int y)
            {
                if (!MazeData.TryGetCellAtPos(x, y, out var cell))
                    return false;
                return cell.CellType == CellTypes.Wall;
            }

        }

        bool CanBreakWall(Cell wallToBreak)
        {

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

                if (AutoWallSpot(newX, newY))
                    continue;
                if (MazeData.TryGetCellAtPos(newX, newY, out var foundCell))
                {
                    if (foundCell.GenerationVisited)
                    {
                        continue;
                        if (foundCell.CellType == CellTypes.Wall && CanBreakWall(foundCell))
                        {
                            chosenCell = foundCell;
                            return true;
                        }
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
                    //If a corner, skip it
                    if (AutoWallSpot(x, y))
                        continue;
                    //If a walkable cell skip it
                    if (cell.Walkable)
                    {
                        continue;
                    }

                    //If a wall see if we can break it
                    if (cell.CellType == CellTypes.Wall)
                    {
                        if (CanBreakWallGroup(cell))
                            continue;
                    }

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

        #endregion














    }


}