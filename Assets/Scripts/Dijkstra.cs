using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class Dijkstra : MazeSolver
{

    public override async void ToggleMazeSolver()
    {
        if (IsRunning)
        {
            IsPaused = !IsPaused;
            return;
        }

        //Setup
        var handler = MazeHandler.Instance;
        var MazeData = handler.MazeData;

        //Check that start and end are valid
        var startCell = handler.StartCell;
        var endCell = MazeHandler.Instance.EndCell;
        if (startCell == null || endCell == null)
            return;
        if (startCell == endCell)
            return;

        //Reset any previous pathfinding
        handler.ClearPathfindingCells();
        startCell.PathfindingValue = 0;

        //Start pathfinding
        IsRunning = true;
        await recursiveLoop(startCell);

        //End pathfinding
        IsRunning = false;
        Debug.Log("Finished Solving Maze");

        async UniTask recursiveLoop(Cell currentNode)
        {
            List<Cell> unvistedCells = new();
            unvistedCells.Add(startCell);
            int loop = -1;
            while (loop < MazeData.Size * 500)
            {
                loop++;
                unvistedCells.Remove(currentNode);
                if (currentNode.CellType != CellTypes.Start && currentNode.CellType != CellTypes.End)
                    handler.PlaceTile(CellTypes.Searching, currentNode.Position3);

                if (IsPaused || !IsRunning)
                {
                    await WaitUntilPlaying();
                    if (!IsRunning || !Application.isPlaying)
                    {
                        IsRunning = false;
                        return;
                    }
                }
                if (loop % handler.DelayFrenquency == 0)
                    await UniTask.Delay(handler.DelayTimeMS);
                //If cell is our goal, we made it
                if (currentNode.Equals(endCell))
                {
                    await FoundPath(currentNode);
                    return;
                }
                updatePathValues();

                //If every unvisited cell has infinity value, stop - no path available
                if (IsEveryUnvisitedNodeInfinity())
                {
                    return;
                }
                //Otherwise continue with lowest cell value
                Cell lowestScoreCell = null;
                foreach (var cell in unvistedCells)
                {
                    if (cell.PathfindingVisited || cell.PathfindingValue == -1)
                        continue;
                    if (lowestScoreCell == null)
                    {
                        lowestScoreCell = cell;
                        continue;
                    }
                    if (cell.PathfindingValue < lowestScoreCell.PathfindingValue)
                        lowestScoreCell = cell;
                }
                currentNode = lowestScoreCell;
            }


            void updatePathValues()
            {
                if (MazeData.PathfindableNeighbors(currentNode.xPos, currentNode.yPos, out var unvistedNeighbors))
                {
                    foreach (var cell in unvistedNeighbors)
                    {
                        unvistedCells.Add(cell);

                        int newValue = currentNode.PathfindingValue + cell.PathfindingCost;
                        int oldValue = cell.PathfindingValue;


                        if (oldValue == -1)
                        {
                            cell.PathfindingValue = newValue;
                            cell.PrevRouteCell = currentNode;
                        }
                        else if (newValue < oldValue)
                        {
                            cell.PrevRouteCell = currentNode;
                            cell.PathfindingValue = newValue;
                        }

                    }
                    return;
                }
            }

            bool IsEveryUnvisitedNodeInfinity()
            {
                foreach (var cell in unvistedCells)
                {
                    if (cell.PathfindingValue != -1)
                        return false;
                }
                return true;
            }
        }


    }

}
