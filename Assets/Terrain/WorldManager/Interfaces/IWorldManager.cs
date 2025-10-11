using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Defines the contract for a world manager responsible for the dynamic
/// loading, generation, and processing of terrain chunks.
/// </summary>
public interface IWorldManager
{
    /// <summary>
    /// A read-only view of the currently active and loaded chunks.
    /// </summary>
    IReadOnlyDictionary<GridCoordinates, IChunk> ActiveChunks { get; }

    /// <summary>
    /// Updates the world state based on a new central position,
    /// typically the player's location. This triggers the loading and unloading of chunks.
    /// </summary>
    /// <param name="newViewPosition">The new world position to focus on.</param>
    void UpdateViewPosition(Vector3 newViewPosition);

    /// <summary>
    /// Retrieves a specific chunk if it is currently active.
    /// </summary>
    /// <param name="coordinates">The coordinates of the chunk to retrieve.</param>
    /// <returns>The chunk instance, or null if not active.</returns>
    IChunk GetChunk(GridCoordinates coordinates);

    /// <summary>
    /// Forces a full regeneration of all active chunks based on the current generation settings.
    /// </summary>
    void ForceFullRegeneration();

    /// <summary>
    /// Calculates the grid coordinates of a neighbor in a given direction.
    /// </summary>
    /// <param name="currentCoords">The coordinates of the starting chunk.</param>
    /// <param name="dir">The direction of the neighbor.</param>
    /// <returns>The coordinates of the neighboring chunk.</returns>
    GridCoordinates GetNeighborCoords(GridCoordinates currentCoords, Direction dir);

    /// <summary>
    /// Marks a chunk as 'dirty' and adds it to the processing queue if it isn't already.
    /// </summary>
    /// <param name="chunk">The chunk to mark as dirty.</param>
    void MarkChunkAsDirty(IChunk chunk);

    /// <summary>
    /// Adds a list of particles to the persistent 'pending' list for a chunk that is not currently loaded.
    /// </summary>
    /// <param name="coords">The coordinates of the unloaded target chunk.</param>
    /// <param name="particles">The list of particles to store.</param>
    void AddPendingParticles(GridCoordinates coords, List<OutgoingParticle> particles);
}
