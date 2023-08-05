using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class AStar : MazeSolver
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

        //Check that start and end cells are valid
        var startCell = MazeHandler.Instance.StartCell;
        var endCell = MazeHandler.Instance.EndCell;
        if (startCell == null || endCell == null)
            return;
        if (startCell == endCell)
            return;
        //Reset any previous pathdfinding data
        handler.ClearPathfindingCells();
        startCell.PathfindingValue = 0;
        
        //Start pathfinding
        IsRunning = true;
        await recursiveLoop(startCell);

        //End Pathfinding
        IsRunning = false;
        Debug.Log("Finished Solving Maze");

        async UniTask recursiveLoop(Cell currentNode)
        {
            //Add all walkable cells as unvisited
            List<Cell> unvistedCells = new();
            foreach (var cell in MazeData.Cells)
            {
                if (cell.Walkable)
                    unvistedCells.Add(cell);
            }
            int loop = -1;
            while (loop < MazeData.Size * 500)
            {
                loop++;
                unvistedCells.Remove(currentNode);
                if (currentNode.CellType != CellTypes.Start && currentNode.CellType != CellTypes.End)
                    MazeHandler.Instance.PlaceTile(CellTypes.Searching, currentNode.Position3);

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
                        //Change djikstra to include weight of where cell is relative to goal
                        int newValue = currentNode.PathfindingValue - (int)Vector2.Distance(currentNode.Position, endCell.Position) + cell.PathfindingCost + (int)Vector2.Distance(cell.Position, endCell.Position);
                        int oldValue = cell.PathfindingValue;

                        //MazeHandler.Instance.SetMazeTile(CellTypes.Searching, cell.Position3);

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
                //If no neighbors then pop the cell
                //cellStack.Pop();
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
