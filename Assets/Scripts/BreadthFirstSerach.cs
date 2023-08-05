using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class BreadthFirstSerach : MazeSolver
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

        //Make sure start and end are valid
        var startCell = handler.StartCell;
        var endCell = handler.EndCell;
        if (startCell == null || endCell == null)
            return;
        if (startCell == endCell)
            return;

        //Reset any previous pathfinding values
        handler.ClearPathfindingCells();

        //Start pathfinding
        IsRunning = true;
        await StartBreadthFirstSearch(startCell);
        
        //End pathfinding
        IsRunning = false;
        Debug.Log("Finished Solving Maze");

        async UniTask StartBreadthFirstSearch(Cell currentNode)
        {
            Queue<Cell> CellsToSearch = new Queue<Cell>();
            CellsToSearch.Enqueue(startCell);
            int loop = -1;
            while (CellsToSearch.TryDequeue(out var nextNode))
            {

                if (CellsToSearch.Count > 200)
                {
                    foreach (var testCell in CellsToSearch)
                    {
                        handler.PlaceTile(CellTypes.FoundPath, testCell.Position3);
                        Debug.Log(testCell.Position3);
                    }
                    return;
                }

                //If we found our end, stop the search
                if (nextNode == endCell)
                {
                    currentNode = nextNode;
                    await FoundPath(currentNode);
                    return;
                }

                //Otherwise add the neighbors that can be checked to the queue
                currentNode = nextNode;
                if (currentNode.CellType != CellTypes.Start && currentNode.CellType != CellTypes.End)
                    handler.PlaceTile(CellTypes.Searching, currentNode.Position3);

                if (MazeData.PathfindableNeighbors(currentNode.xPos, currentNode.yPos, out var foundCells))
                {
                    foreach (var foundCell in foundCells)
                    {
                        foundCell.PrevRouteCell = currentNode;
                        if (CellsToSearch.Contains(foundCell))
                            continue;
                        CellsToSearch.Enqueue(foundCell);
                    }
                }


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

                loop++;

            }
        }

    }

    public override void CancelMazeSolver()
    {
        IsPaused = false;
        IsRunning = false;
    }
}