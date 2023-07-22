using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(MazeHandler), true)]
public class MazeControllerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        var controller = (MazeHandler)target;
        if (GUILayout.Button("Reset Maze"))
        {
            controller.ResetMaze();
        }

        if (GUILayout.Button("Build Maze"))
        {
            controller.ToggleRunMazeGeneration();
        }
        if (GUILayout.Button("Update Maze"))
        {
            controller.UpdateMaze();
        }
    }

}
