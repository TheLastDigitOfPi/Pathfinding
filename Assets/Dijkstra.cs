using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class Dijkstra : MazeSolver
{

    [SerializeField] int _delayTimeMS;
    bool _paused = false;
    bool _canceled = false;


    async Task WaitUntilPlaying()
    {
        while (_paused)
        {
            await Task.Delay(5);
            if (_canceled || !Application.isPlaying)
                return;
        }
    }

    public override async void ToggleMazeSolver()
    {
        if (IsRunning)
        {
            _paused = !_paused;
            return;
        }
        IsRunning = true;
        _canceled = false;
        var startCell = MazeHandler.Instance.StartCell;
        var endCell = MazeHandler.Instance.EndCell;
        if (startCell == null || endCell == null)
            return;
        if (startCell == endCell)
            return;
        var handler = MazeHandler.Instance;
        MazeHandler.Instance.ClearPathfindingCells();
        var MazeData = MazeHandler.Instance.MazeData;
        var cells = MazeData.Cells;
        int distanceValue = 1;
        startCell.PathfindingValue = 0;
        List<Cell> unvistedCells = new();
        foreach (var cell in cells)
        {
            if (cell.Walkable)
                unvistedCells.Add(cell);
        }
        await recursiveLoop(startCell);
        IsRunning = false;
        Debug.Log("Finished Solving Maze");
        async Task recursiveLoop(Cell currentNode)
        {
            int loop = -1;
            while (true)
            {
                loop++;
                unvistedCells.Remove(currentNode);
                currentNode.PathfindingVisited = true;
                if (currentNode.CellType != CellTypes.Start && currentNode.CellType != CellTypes.End)
                    MazeHandler.Instance.PlaceTile(CellTypes.Searching, currentNode.Position3);

                if (_paused || _canceled)
                {
                    await WaitUntilPlaying();
                    if (_canceled || !Application.isPlaying)
                    {
                        IsRunning = false;
                        return;
                    }
                }
                if (loop % handler.DelayFrenquency == 0)
                    await Task.Delay(handler.DelayTimeMS);
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
                        int newValue = currentNode.PathfindingValue + distanceValue;
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

        async Task FoundPath(Cell endCell)
        {
            Cell lastCellChanged = endCell;
            int loop = -1;
            while (endCell.PrevRouteCell != null)
            {
                loop++;
                if (endCell.CellType != CellTypes.Start && endCell.CellType != CellTypes.End)
                    MazeHandler.Instance.PlaceTile(CellTypes.FoundPath, endCell.Position3);
                endCell = endCell.PrevRouteCell;
                if (endCell == lastCellChanged)
                    return;
                if (_paused || _canceled)
                {
                    await WaitUntilPlaying();
                    if (_canceled || !Application.isPlaying)
                    {
                        IsRunning = false;
                        return;
                    }
                }
                if (loop % handler.DelayFrenquency == 0)
                    await Task.Delay(handler.DelayTimeMS);
            }
            onMazeSolve?.Invoke();
        }
    }

    public override void CancelMazeSolver()
    {
        _canceled = true;
        IsRunning = false;
    }
}