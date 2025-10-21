using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ErosionConfig))]
public class ErosionConfigEditor : Editor
{
    private static int s_LastMouseUpFrame = 0;
    private ErosionConfig _targetConfig;

    private void OnEnable()
    {
        _targetConfig = (ErosionConfig)target;
    }

    public override void OnInspectorGUI()
    {
        EditorGUI.BeginChangeCheck();

        DrawDefaultInspector();

        if (EditorGUI.EndChangeCheck())
        {
            EditorUtility.SetDirty(target);

            if (CheckStructuralChange())
            {
                TriggerStructuralRegeneration();
            }
            else
            {
                TriggerWorldRegenerationDelayed();
            }
        }

        if (Event.current.type == EventType.MouseUp && s_LastMouseUpFrame != Time.frameCount)
        {
            s_LastMouseUpFrame = Time.frameCount;
            if (CheckStructuralChange())
            {
                TriggerStructuralRegeneration();
            }
            else
            {
                TriggerWorldRegenerationDelayed();
            }
        }
    }

    private bool CheckStructuralChange()
    {
        return serializedObject.FindProperty("erosionBrushRadius").intValue != _targetConfig.erosionBrushRadius ||
               serializedObject.FindProperty("haloZoneWidth").intValue != _targetConfig.haloZoneWidth;
    }

    private void TriggerStructuralRegeneration()
    {
        TriggerWorldRegenerationDelayed();
    }

    private void TriggerWorldRegenerationDelayed()
    {
        WorldManager worldManager = Object.FindFirstObjectByType<WorldManager>();
        if (worldManager != null)
        {
            worldManager.TriggerRegenerationDelayed();
        }
    }
}