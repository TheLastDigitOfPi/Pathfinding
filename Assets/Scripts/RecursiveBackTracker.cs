using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Cysharp.Threading.Tasks;

public class RecursiveBackTracker : MazeGenerator
{
    async UniTask WaitUntilPlaying()
    {
        while (IsPaused)
        {
            await UniTask.Delay(3);
            if (!IsRunning)
                return;
        }
    }

    int[] directionX = { 1, 0, -1, 0 };
    int[] directionY = { 0, 1, 0, -1 };

    public override void ResetMazeGeneration()
    {
        IsPaused = false;
        IsRunning = false;
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
        Cell currentCell = null;

        List<Cell> unvisitedCells = new();
        foreach (var cell in MazeData.Cells)
        {
            if (!cell.GenerationVisited)
                unvisitedCells.Add(cell);
        }

        if (unvisitedCells.Count == 0)
            return;
        unvisitedCells.OrderBy(a => rand.Next()).ToList();
        int checkCell = 0;
        while (checkCell < unvisitedCells.Count)
        {
            currentCell = unvisitedCells[checkCell];
            checkCell++;
            if (AutoWallSpot(currentCell.xPos, currentCell.yPos))
                continue;
            break;
        }
        PlaceWallsVersion1();
        #endregion

        #region Main Loop

        Stack<Cell> cells = new Stack<Cell>();
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
            if (!CellHasNeighbors(currentCell, out Cell foundCell))
            {
                //if it doesn't, go back to the previous cell
                if (!cells.TryPop(out Cell prevCell))
                {
                    //if there is no previous cell, we have finished
                    fillInRemainingSpots();

                    //But first check if any filled in spots are too large
                    foreach (var cell in cells)
                    {
                        if (cell.CellType == CellTypes.Wall)
                            TryBreakWallGroup(cell);
                    }
                    //Then see if we can continue on any cells
                    foreach (var cell in cells)
                    {
                        if (!cell.Walkable)
                            continue;
                        cells.Push(cell);
                    }

                    if (cells.Count == 0)
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

        #endregion

        #region Useful Functions

        bool CellOnEdge(Cell cell)
        {
            return cell.xPos == 0 || cell.yPos == 0 || cell.xPos == MazeData.Size - 1 || cell.yPos == MazeData.Size - 1;
        }

        bool AutoWallSpot(int x, int y)
        {
            return x % 2 == 1 && y % 2 == 1;
        }

        void PlaceWallsVersion1()
        {
            foreach (var cell in MazeData.Cells)
            {
                if (AutoWallSpot(cell.xPos, cell.yPos) && !CellOnEdge(cell))
                    handler.PlaceTile(CellTypes.Wall, cell.Position3);
            }
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
            IsRunning = false;
            onMazeGenerationComplete?.Invoke();
        }

        bool CellHasNeighbors(Cell originalCell, out Cell randomNeighbor)
        {
            randomNeighbor = null;

            List<int> directions = new List<int> { 0, 1, 2, 3 };
            directions = directions.OrderBy(d => rand.Next()).ToList();
            foreach (var direction in directions)
            {
                int newX = originalCell.xPos + directionX[direction];
                int newY = originalCell.yPos + directionY[direction];

                if (AutoWallSpot(newX, newY))
                    continue;
                if (MazeData.TryGetCellAtPos(newX, newY, out var foundCell))
                {
                    if (foundCell.GenerationVisited)
                    {
                        if (foundCell.CellType == CellTypes.Wall)
                        {
                            if (TryBreakWallGroup(foundCell))
                            {
                                randomNeighbor = foundCell;
                                BlackoutCells(originalCell.Position, direction);
                                break;
                            }
                        }
                        continue;
                    }
                    randomNeighbor = foundCell;
                    BlackoutCells(originalCell.Position, direction);
                    return true;
                }
            }


            return false;
        }

        bool TryBreakWallGroup(Cell wallToCheck)
        {
            //We will check if the wall is part of a 2x2 group

            if (!CheckEachCorner(out var cornerX, out var cornerY))
                return false;

            bool CheckEachCorner(out int[] cornerX, out int[] cornerY)
            {

                int[] checkRightX = { 0, 1, 1 };
                int[] checkLeftX = { 0, -1, -1 };
                int[] checkUpY = { 1, 1, 0 };
                int[] checkDownY = { -1, -1, 0 };

                cornerX = checkRightX;
                cornerY = checkUpY;

                if (CheckCorner(checkRightX, checkUpY))
                {
                    cornerX = checkRightX;
                    cornerY = checkUpY;
                    return true;
                }
                if (CheckCorner(checkRightX, checkDownY))
                {
                    cornerX = checkRightX;
                    cornerY = checkDownY;
                    return true;
                }
                if (CheckCorner(checkLeftX, checkUpY))
                {
                    cornerX = checkLeftX;
                    cornerY = checkUpY;
                    return true;
                }
                if (CheckCorner(checkLeftX, checkDownY))
                {
                    cornerX = checkLeftX;
                    cornerY = checkDownY;
                    return true;
                }

                return false;

                bool CheckCorner(int[] checkX, int[] checkY)
                {
                    for (int i = 0; i < checkX.Length; i++)
                    {
                        if (!AlsoWall(wallToCheck.xPos + checkX[i], wallToCheck.yPos + checkY[i]))
                            return false;
                    }
                    return true;
                }

            }


            //Now we convert up to a 3x3 from that point back to empty
            //This allows the generation to go back through and fill in the maze spot
            for (int x = 0; x < 2; x++)
            {
                for (int y = 0; y < 2; y++)
                {
                    int testX = wallToCheck.xPos + cornerX[x];
                    int testY = wallToCheck.yPos + cornerY[y];
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

        void BlackoutCells(Vector2Int startingPos, int startingDirection)
        {

            //int[] directionX = { 1, 0, -1, 0 };
            //int[] directionY = { 0, 1, 0, -1 };

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

                if (!MazeData.TryGetCellAtPos(newX, newY, out var cell))
                    continue;
                if (cell.GenerationVisited)
                    continue;

                //Check if cell creates an unfinadable path don't place it

                newX += directionX[direction];
                newY += directionY[direction];
                if (!MazeData.TryGetCellAtPos(newX, newY, out var checkCell))
                    continue;

                if (!MazeData.HasPotentialPathContinuation(checkCell))
                {
                    continue;
                    handler.PlaceTile(CellTypes.Floor, cell.Position3);
                    cells.Push(cell);
                }

                handler.PlaceTile(CellTypes.Wall, cell.Position3);
            }

        }

        #endregion

    }
}