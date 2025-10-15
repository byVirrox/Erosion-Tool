using UnityEngine;

/// <summary>
/// Abstrakte Basisklasse f�r alle Terrain-Generatoren, die als ScriptableObject-Assets existieren.
/// Stellt sicher, dass nur g�ltige Generatoren im Inspector zugewiesen werden k�nnen.
/// </summary>
public abstract class TerrainGenerator : ScriptableObject, ITerrainGenerator
{
    public abstract RenderTexture Generate(TerrainGenerationContext context);
}
