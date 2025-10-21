using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// Defines the contract for a service that can apply a particle-based erosion algorithm (e.g., Hydraulic Erosion) 
/// to a terrain chunk.
/// 
/// The eroder manages the lifecycle of particle simulation buffers, executes the erosion logic on the GPU,
/// and provides the necessary resources and utilities (like the transfer shader and particle counting) 
/// for orchestrating inter-chunk particle movement.
/// </summary>
public interface IParticleEroder
{
    /// <summary>
    /// Executes the particle-based erosion process for the given chunk.
    /// This typically involves dropping new particles, processing incoming particles, 
    /// running the simulation on the GPU, and updating the provided halo map.
    /// </summary>
    /// <param name="chunk">The chunk object containing particle buffers and metadata.</param>
    /// <param name="haloMap">The combined RenderTexture (chunk map + neighbor borders) to be eroded.</param>
    /// <param name="worldSeed">The global seed used for deterministic particle dropping.</param>
    void Erode(IParticleErodibleChunk chunk, RenderTexture haloMap, int worldSeed);

    /// <summary>
    /// Retrieves the ComputeShader used for transferring particles between chunks.
    /// This is typically used by an external utility to read and reset the outgoing buffer.
    /// </summary>
    /// <returns>The ComputeShader responsible for particle transfer logic.</returns>
    ComputeShader GetTransferShader();

    /// <summary>
    /// Resets the counter of the internal buffer holding particles that have left the chunk's boundaries.
    /// Must be called after the outgoing particles have been successfully transferred.
    /// </summary>
    void ClearOutgoingBuffer();

    /// <summary>
    /// Retrieves the ComputeBuffer containing particles that have simulated outside the current chunk's bounds.
    /// This buffer is typically an AppendStructuredBuffer on the GPU.
    /// </summary>
    /// <returns>The buffer containing particles to be transferred to neighboring chunks.</returns>
    ComputeBuffer GetOutgoingBuffer();

    /// <summary>
    /// Gets the number of elements currently in an Append-type ComputeBuffer on the GPU.
    /// This is generally an expensive operation as it requires reading data back from the GPU.
    /// </summary>
    /// <param name="appendBuffer">The buffer whose count should be retrieved.</param>
    /// <returns>The number of elements in the buffer.</returns>
    int GetAppendBufferCount(ComputeBuffer appendBuffer);
}