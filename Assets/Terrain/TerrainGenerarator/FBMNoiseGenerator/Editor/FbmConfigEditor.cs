using UnityEditor;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

[CustomEditor(typeof(FbmConfig))]
public class FbmConfigEditor : Editor
{
    private static int s_LastMouseUpFrame = 0;

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        if (GUI.changed)
        {
            EditorUtility.SetDirty(target);
        }

        if (Event.current.type == EventType.MouseUp || GUI.changed && s_LastMouseUpFrame != Time.frameCount)
        {
            s_LastMouseUpFrame = Time.frameCount;
            TriggerWorldRegeneration();
        }
    }

    private void TriggerWorldRegeneration()
    {
        WorldManager worldManager = Object.FindFirstObjectByType<WorldManager>();

        if (worldManager != null)
        {
            worldManager.TriggerRegenerationDelayed();
        }
    }
}