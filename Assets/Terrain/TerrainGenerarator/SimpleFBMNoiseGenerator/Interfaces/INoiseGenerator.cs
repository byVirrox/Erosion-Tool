using UnityEngine;

/// <summary>
/// Defines the contract for a terrain generator that populates
/// the heightmap of a chunk with initial data.
/// </summary>
public interface INoiseGenerator
{
    /// <summary>
    /// Generates a new RenderTexture filled with procedural noise.
    /// </summary>
    /// <param name="coords">The grid coordinates for a chunk to generate the texture.</param>
    /// <param name="resolution">The base resolution of the texture area (without border).</param>
    /// <param name="borderSize">The width of the border in pixels to add around the base area. Use 0 for a standard chunk.</param>
    /// <returns>A new, noise-filled RenderTexture.</returns>
    /// 
    RenderTexture GenerateTexture(GridCoordinates coords, int resolution, int borderSize);
}
