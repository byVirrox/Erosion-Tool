using System.Collections.Generic;

/// <summary>
/// An abstract base class providing common logic for all chunk types.
/// </summary>
public abstract class BaseChunk : IChunk 
{
    public GridCoordinates Coordinates { get; protected set; }
    public bool IsDirty { get; set; }

    protected Dictionary<NeighborDirection, IChunk> neighbors = new Dictionary<NeighborDirection, IChunk>();

    protected BaseChunk(GridCoordinates coordinates)
    {
        this.Coordinates = coordinates;
        this.IsDirty = true;
    }

    public void SetNeighbor(NeighborDirection direction, IChunk neighbor)
    {
        neighbors[direction] = neighbor;
    }

    public IChunk GetNeighbor(NeighborDirection direction)
    {
        neighbors.TryGetValue(direction, out IChunk neighbor);
        return neighbor;
    }
}