using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Defines the contract for a world that can handle particle erosion,
/// including marking chunks as dirty and managing pending particles.
/// </summary>
public interface IParticleWorldManager
{
    /// <summary>
    /// Retrieves a specific chunk if it is currently active.
    /// </summary>
    /// <param name="coordinates">The coordinates of the chunk to retrieve.</param>
    /// <returns>The chunk instance, or null if not active.</returns>
    IChunk GetChunk(GridCoordinates coordinates);
    /// <summary>
    /// Marks a chunk as 'dirty' and adds it to the processing queue.
    /// </summary>
    void MarkChunkAsDirty(IChunk chunk);

    /// <summary>
    /// Adds particles to a list for a chunk that is not currently loaded.
    /// </summary>
    void AddPendingParticles(GridCoordinates coords, List<Particle> particles);
}
