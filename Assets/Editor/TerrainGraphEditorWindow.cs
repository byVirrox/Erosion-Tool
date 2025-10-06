using UnityEditor;
using UnityEngine;
using Unity.GraphToolkit.Editor;

public class TerrainGraphEditorWindow : EditorWindow
{

    public static void OpenWindow()
    {
        GetWindow<TerrainGraphEditorWindow>("Terrain Graph Editor");
    }

    private void CreateGUI()
    {

    }
}
