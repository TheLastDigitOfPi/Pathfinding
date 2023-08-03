using UnityEngine;
using System;

public abstract class MazeGenerator : MonoBehaviour
{
    public event Action OnMazeGenerationComplete {add {onMazeGenerationComplete += value;} remove {onMazeGenerationComplete -= value;} }
    protected Action onMazeGenerationComplete;
    public bool IsRunning = false;
    public bool IsPaused = false;
    public abstract void ResetMazeGeneration();
    public abstract void ToggleMazeGeneration();

}
