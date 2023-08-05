using Cysharp.Threading.Tasks;
using System;
using UnityEngine;

public abstract class MazeSolver : MonoBehaviour
{
    public bool IsRunning { get; protected set; } = false;
    public bool IsPaused { get; protected set; } = false;
    public event Action OnMazeSolve { add { onMazeSolve += value; } remove { onMazeSolve -= value; } }
    protected Action onMazeSolve;
    public abstract void ToggleMazeSolver();

    protected async UniTask WaitUntilPlaying()
    {
        while (IsPaused)
        {
            await UniTask.Delay(5);
            if (!IsRunning || !Application.isPlaying)
                return;
        }
    }
    public virtual void CancelMazeSolver()
    {
        IsRunning = false;
        IsPaused = false;
    }

    protected async UniTask FoundPath(Cell currentPathCell)
    {
        var handler = MazeHandler.Instance;
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

    
    protected bool FindSiglePathfindableNeighbor(Cell checkCell, out Cell foundCell)
    {
        var handler = MazeHandler.Instance;
        var mazeData = handler.MazeData;
        foundCell = null;
        int[] xPositions = { 1, -1, 0, 0 };
        int[] yPositions = { 0, 0, -1, 1 };

        for (int i = 0; i < xPositions.Length; i++)
        {
            if (!mazeData.TryGetCellAtPos(checkCell.xPos + xPositions[i], checkCell.yPos + yPositions[i], out var cell))
                continue;
            if ((cell.PathfindingVisited || !cell.Walkable) && cell.CellType != CellTypes.End)
                continue;
            foundCell = cell;
            return true;
        }
        return false;
    }
}
