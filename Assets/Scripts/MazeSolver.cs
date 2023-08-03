using System;
using UnityEngine;

public abstract class MazeSolver : MonoBehaviour
{
    public bool IsRunning { get; protected set; } = false;
    public bool IsPaused { get; protected set; } = false;
    public event Action OnMazeSolve { add { onMazeSolve += value; } remove { onMazeSolve -= value; } }
    protected Action onMazeSolve;
    public abstract void ToggleMazeSolver();
    public abstract void CancelMazeSolver();
}
