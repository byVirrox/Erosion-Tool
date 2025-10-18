using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Defines the contract for a world that can handle particle erosion,
/// including marking chunks as dirty and managing pending particles.
/// </summary>
public interface IParticleWorldManager : IWorldManager
{
    /// <summary>
    /// Marks a chunk as 'dirty' and adds it to the processing queue.
    /// </summary>
    void MarkChunkAsDirty(IChunk chunk);

    /// <summary>
    /// Adds particles to a list for a chunk that is not currently loaded.
    /// </summary>
    void AddPendingParticles(GridCoordinates coords, List<Particle> particles);
}
