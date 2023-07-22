using System;
using System.Collections;
using UnityEditor.ProjectWindowCallback;
using UnityEngine;


public class DepthFirstSearch : MazeSolver
{

    public override void ToggleMazeSolver()
    {
        MazeHandler.Instance.ClearPathfindingCells();
    }

    public override void CancelMazeSolver()
    {
        throw new System.NotImplementedException();
    }
}


public abstract class MazeSolver : MonoBehaviour
{
    public bool IsRunning { get; protected set; } = false;
    public event Action OnMazeSolve { add { onMazeSolve += value; } remove { onMazeSolve -= value; } }
    protected Action onMazeSolve;
    public abstract void ToggleMazeSolver();
    public abstract void CancelMazeSolver();
}
