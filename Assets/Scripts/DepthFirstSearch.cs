using UnityEngine;
using Cysharp.Threading.Tasks;

public class DepthFirstSearch : MazeSolver
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
        Cell currentNode = startCell;

        //Reset any current pathfinding
        handler.ClearPathfindingCells();
        
        //Start pathfinding
        IsRunning = true;
        await DepthFirstSearchStart();

        //End Pathfinding
        IsRunning = false;
        Debug.Log("Finished Solving Maze");

        async UniTask DepthFirstSearchStart()
        {
            int loop = -1;
            while (loop < MazeData.Size * 200)
            {
                loop++;
                //Check if paused
                if (IsPaused || !IsRunning)
                {
                    if (!IsRunning)
                        return;
                    await WaitUntilPlaying();
                    if (!IsRunning || !Application.isPlaying)
                    {
                        IsRunning = false;
                        return;
                    }
                }

                await serachNextCell();

                if (currentNode == null)
                    return;

                if (loop % handler.DelayFrenquency == 0)
                    await UniTask.Delay(handler.DelayTimeMS);
            }

            async UniTask serachNextCell()
            {
                if (FindSiglePathfindableNeighbor(currentNode, out var foundCell))
                {
                    foundCell.PrevRouteCell = currentNode;
                    if (foundCell == endCell)
                    {
                        await FoundPath(foundCell);
                        return;
                    }
                    handler.PlaceTile(CellTypes.Searching, foundCell.Position3);
                    currentNode = foundCell;
                    return;
                }
                currentNode = currentNode.PrevRouteCell;
            }

        }

    }

}
