using UnityEngine;

/// <summary>
/// Abstrakte Basisklasse für alle Terrain-Generatoren, die als ScriptableObject-Assets existieren.
/// Stellt sicher, dass nur gültige Generatoren im Inspector zugewiesen werden können.
/// </summary>
public abstract class TerrainGenerator : ScriptableObject, ITerrainGenerator
{
    public abstract RenderTexture Generate(TerrainGenerationContext context);
}
