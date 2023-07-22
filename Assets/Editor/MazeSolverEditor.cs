using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(MazeSolver), true)]
public class MazeSolverEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        var controller = (MazeSolver)target;
        if (GUILayout.Button("Solve Maze"))
        {
            controller.ToggleMazeSolver();
        }
    }

}