using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class PrimsGenerator : MazeGenerator
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
        IsRunning = false;
        IsPaused = false;
    }

    public override async void ToggleMazeGeneration()
    {
        if (IsRunning)
        {
            IsPaused = !IsPaused;
            return;
        }

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

        IsRunning = true;
        PlaceWallsVersion1();
        #endregion


        Cell prevCell = null;
        int loops = 0;
        Dictionary<Cell, Cell> remainingCells = new Dictionary<Cell, Cell>();
        remainingCells.Add(currentCell, null);
        while (remainingCells.Count > 0)
        {
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

            List<int> inverseDirections = new List<int> { 2, 3, 0, 1 };
            List<int> directions = new List<int> { 0, 1, 2, 3 };
            foreach (var direction in directions)
            {
                if (direction == startingDirection)
                    continue;
                if (direction == inverseDirections[startingDirection])
                    continue;
                int newX = prevCell.xPos + directionX[direction];
                int newY = prevCell.yPos + directionY[direction];

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
                    continue;

                handler.PlaceTile(CellTypes.Wall, cell.Position3);

                //Basically we want to try and prevent paths from looping in on themselves making a 3x3
                /*          
                            o o o
                            o W o <- Do not want this
                            o o o
                 */

                //If we went up or down check the two neighboring cells
                /*
                        W x W x W 
                        x o o o x 
                        W x W o W <-- left x needs to be wall now
                        x x x x x 
                        W x W x W

                        W x W x W 
                        x o o o x 
                        W W W o W <-- to look like this
                        x x x x x 
                        W x W x W

                This results in way too much computational checking, so must be some better way

                x+ 2* y direction y+ -1*ydirection

                 */

            }
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

        void PlaceWallsVersion1()
        {
            foreach (var cell in MazeData.Cells)
            {
                if (AutoWallSpot(cell.xPos, cell.yPos) && !CellOnEdge(cell))
                    handler.PlaceTile(CellTypes.Wall, cell.Position3);
            }
        }

        bool CellOnEdge(Cell cell)
        {
            return cell.xPos == 0 || cell.yPos == 0 || cell.xPos == MazeData.Size - 1 || cell.yPos == MazeData.Size - 1;
        }
        bool AutoWallSpot(int x, int y)
        {
            return x % 2 == 1 && y % 2 == 1;
        }

    }
}

public class EllersGenerator : MazeGenerator
{
    public override void ResetMazeGeneration()
    {
        throw new NotImplementedException();
    }

    public override void ToggleMazeGeneration()
    {
        throw new NotImplementedException();
    }
}
