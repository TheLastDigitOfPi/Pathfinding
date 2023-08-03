using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class BreadthFirstSerach : MazeSolver
{
    async UniTask WaitUntilPlaying()
    {
        while (IsPaused)
        {
            await UniTask.Delay(5);
            if (!IsRunning || !Application.isPlaying)
                return;
        }
    }

    public override async void ToggleMazeSolver()
    {
        if (IsRunning)
        {
            IsPaused = !IsPaused;
            return;
        }

        var handler = MazeHandler.Instance;
        var startCell = handler.StartCell;
        var endCell = handler.EndCell;
        if (startCell == null || endCell == null)
            return;
        if (startCell == endCell)
            return;
        IsRunning = true;
        handler.ClearPathfindingCells();
        var MazeData = handler.MazeData;
        Cell currentNode = startCell;
        var cells = MazeData.Cells;


        Queue<Cell> CellsToSearch = new Queue<Cell>();
        CellsToSearch.Enqueue(startCell);
        await StartBreadthFirstSearch(startCell);
        IsRunning = false;
        Debug.Log("Finished Solving Maze");

        async UniTask StartBreadthFirstSearch(Cell currentNode)
        {
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
                        if(CellsToSearch.Contains(foundCell))
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



        async UniTask FoundPath(Cell currentPathCell)
        {
            int loop = -1;
            while (currentPathCell.PrevRouteCell != null)
            {
                loop++;
                //Set the cell on the board
                if (currentPathCell.CellType != CellTypes.Start && currentPathCell.CellType != CellTypes.End)
                    handler.PlaceTile(CellTypes.FoundPath, currentPathCell.Position3);
                //Check if cell has already been confirmed, meaning it loops somewhere
                if (currentPathCell.PrevRouteCell.CellType == CellTypes.FoundPath)
                    return;
                //Otherwise continue the foundpath search
                currentPathCell = currentPathCell.PrevRouteCell;
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
            }
            onMazeSolve?.Invoke();
        }
    }

    public override void CancelMazeSolver()
    {
        IsPaused = false;
        IsRunning = false;
    }
}