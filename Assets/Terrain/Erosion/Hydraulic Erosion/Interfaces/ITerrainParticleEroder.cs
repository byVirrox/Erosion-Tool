using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// Contains the complete results of a single erosion pass on a chunk.
/// </summary>
public struct ErosionResult
{
    /// <summary>
    /// A dictionary mapping the outgoing buffer index to the number of particles that exited.
    /// </summary>
    public Dictionary<int, int> OutgoingParticleCounts;

    /// <summary>
    /// A list of neighboring chunks that were modified during the erosion pass
    /// (by having their borders eroded) and now need to be marked as dirty.
    /// </summary>
    public List<IChunk> DirtiedNeighbors;
}

/// <summary>
/// Defines the contract for a service that can apply an erosion algorithm to a terrain chunk.
/// </summary>
public interface ITerrainParticleEroder
{
    public ErosionResult Erode(IParticleErodibleChunk chunk, INoiseGenerator generator, int worldSeed);

    ComputeShader GetTransferShader();
    ComputeBuffer[] GetOutgoingBuffers();
    /// <summary>
    /// Gets the number of elements currently in an Append-type ComputeBuffer on the GPU.
    /// </summary>
    /// <param name="appendBuffer">The buffer whose count should be retrieved.</param>
    /// <returns>The number of elements in the buffer.</returns>
    int GetAppendBufferCount(ComputeBuffer appendBuffer);
}