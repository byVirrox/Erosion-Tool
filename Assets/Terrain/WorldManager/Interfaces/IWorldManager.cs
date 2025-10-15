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
}
