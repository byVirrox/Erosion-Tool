using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(WorldManager))]
public class WorldManagerEditor : Editor
{
    private WorldManager _worldManager;

    private void OnEnable()
    {
        // Hole eine Referenz auf das WorldManager-Skript, das wir inspizieren.
        _worldManager = (WorldManager)target;
    }

    public override void OnInspectorGUI()
    {
        // Zeichne den Standard-Inspector (alle öffentlichen Felder wie Player, viewDistance etc.)
        DrawDefaultInspector();

        // Füge etwas Abstand hinzu
        EditorGUILayout.Space(10);

        // --- Unser benutzerdefiniertes Editor-UI ---

        // Button 1: Simuliert eine Aktualisierung der Spielerposition
        if (GUILayout.Button("Update View (Load/Unload Chunks)"))
        {
            if (_worldManager.player != null)
            {
                // Rufe die Kernfunktion auf, die auch im Update-Loop verwendet wird.
                _worldManager.UpdateViewPosition(_worldManager.player.position);
                Debug.Log("UpdateViewPosition manuell ausgelöst.");
            }
            else
            {
                Debug.LogWarning("Player-Transform ist im WorldManager nicht zugewiesen.");
            }
        }

        // Button 2: Führt die ForceFullRegeneration-Logik aus
        if (GUILayout.Button("Force Full Regeneration"))
        {
            // Führe die Logik aus und gib eine Bestätigung in der Konsole aus.
            _worldManager.ForceFullRegeneration();
            Debug.Log("ForceFullRegeneration ausgelöst: Alle aktiven Chunks wurden neu generiert und als 'dirty' markiert.");
        }
    }
}
