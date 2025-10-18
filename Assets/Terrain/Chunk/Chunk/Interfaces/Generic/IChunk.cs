using UnityEngine;

public interface IChunk<T> : IChunk
{
    /// <summary>
    /// Gets the strongly-typed heightmap data of the chunk.
    /// </summary>
    T GetHeightMapData();

    /// <summary>
    /// Sets the strongly-typed heightmap data of the chunk.
    /// </summary>
    /// <param name="data">The new heightmap data.</param>
    void SetHeightMapData(T data);
}
