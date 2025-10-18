using UnityEngine;

/// <summary>
/// Abstract base class for all terrain generators that exist as ScriptableObject assets.
/// Ensures that only valid generators can be assigned in the Inspector.
/// </summary>
public abstract class TerrainGenerator : ScriptableObject, ITerrainGenerator
{
    public abstract RenderTexture Generate(TerrainGenerationContext context);
}
