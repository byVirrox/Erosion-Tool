using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(WorldManager))]
public class WorldManagerEditor : Editor
{
    private WorldManager _worldManager;

    private void OnEnable()
    {
        _worldManager = (WorldManager)target;
    }

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space(10);

        if (GUILayout.Button("Update View (Load/Unload Chunks)"))
        {
            if (_worldManager.player != null)
            {
                _worldManager.UpdateViewPosition(_worldManager.player.position);
                Debug.Log("UpdateViewPosition got triggered manually.");
            }
            else
            {
                Debug.LogWarning("Player Transform has not been assigned in WorldManager.");
            }
        }

        if (GUILayout.Button("Force Full Regeneration"))
        {
            _worldManager.ForceFullRegeneration();
            Debug.Log("ForceFullRegeneration triggered: All active chunks have been regenerated and marked as ‘dirty’.");
        }
    }
}
