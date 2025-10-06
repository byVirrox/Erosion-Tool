/// <summary>
/// Defines a contract for a factory that creates and initializes new chunk instances.
/// </summary>
public interface IChunkFactory
{
    /// <summary>
    /// Creates a new chunk instance at the specified coordinates with a given resolution.
    /// </summary>
    /// <param name="coords">The grid coordinates for the new chunk.</param>
    /// <param name="resolution">The resolution of the chunk's heightmap.</param>
    /// <returns>A newly created and initialized IChunk instance.</returns>
    IChunk CreateChunk(GridCoordinates coords, int resolution);

    INoiseGenerator GetNoiseGenerator();
}