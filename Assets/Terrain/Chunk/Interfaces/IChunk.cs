/// <summary>
/// Defines the public contract for a terrain chunk within the world grid.
/// </summary>
public interface IChunk
{
    /// <summary>
    /// The unique grid coordinates of the chunk in the world.
    /// </summary>
    GridCoordinates Coordinates { get; } 

    /// <summary>
    /// Indicates whether the chunk needs to be recalculated or updated.
    /// </summary>
    bool IsDirty { get; set; }

    /// <summary>
    /// Sets a reference to a neighboring chunk.
    /// </summary>
    /// <param name="direction">The direction of the neighbor (e.g., North, East).</param>
    /// <param name="neighbor">The instance of the neighboring chunk.</param>
    void SetNeighbor(NeighborDirection direction, IChunk neighbor);

    /// <summary>
    /// Gets the neighboring chunk in a given direction.
    /// </summary>
    IChunk GetNeighbor(NeighborDirection direction);
}