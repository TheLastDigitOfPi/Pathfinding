using UnityEngine;
using Cysharp.Threading.Tasks;

public class DepthFirstSearch : MazeSolver
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
        bool foundPath = false;

        Cell currentNode = startCell;
        await DepthFirstSearchStart();
        IsRunning = false;
        Debug.Log("Finished Solving Maze");
        async UniTask DepthFirstSearchStart()
        {
            int loop = -1;
            while (true && loop < MazeData.Size * 200)
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

                if (foundPath)
                    return;

                if (currentNode == null)
                    return;

                if (loop % handler.DelayFrenquency == 0)
                    await UniTask.Delay(handler.DelayTimeMS);
            }

            async UniTask serachNextCell()
            {
                if (MazeData.FindSiglePathfindableNeighbor(currentNode, out var foundCell))
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

        async UniTask FoundPath(Cell currentPathCell)
        {
            foundPath = true;
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
        IsRunning = false;
        IsPaused = false;
    }
}
