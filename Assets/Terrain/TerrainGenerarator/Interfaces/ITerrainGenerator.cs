using UnityEngine;

public interface ITerrainGenerator
{
    RenderTexture Generate(TerrainGenerationContext context);
}
