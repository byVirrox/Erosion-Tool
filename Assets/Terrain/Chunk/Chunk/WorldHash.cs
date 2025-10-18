public static class WorldHash
{
    /// <summary>
    /// Generates a unique and deterministic integer seed for a chunk.
    /// </summary>
    /// <param name="coords">The coordinates of the chunk.</param>
    /// <param name="worldSeed">The global seed of the world.</param>
    /// <returns>A unique integer seed for this chunk.</returns>
    public static int GetChunkSeed(GridCoordinates coords, int worldSeed)
    {
        int hash = 17;
        unchecked 
        {
            hash = hash * 23 + coords.X;
            hash = hash * 23 + coords.Y;
            hash = hash * 23 + worldSeed;
        }
        return hash;
    }
}
