/// <summary>
/// Defines a contract for a factory that creates and initializes new chunk instances.
/// </summary>
public interface IChunkFactory<TChunk> where TChunk : IChunk
{
    TChunk CreateChunk(GridCoordinates coords, int resolution, int worldSeed);

    ITerrainGenerator GetGenerator();
}