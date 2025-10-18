using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// Defines the contract for a service that can apply an erosion algorithm to a terrain chunk.
/// </summary>
public interface ITerrainParticleEroder
{
    void Erode(IParticleErodibleChunk chunk, RenderTexture haloMap, int worldSeed);

    ComputeShader GetTransferShader();

    void ClearOutgoingBuffer();
    ComputeBuffer GetOutgoingBuffer();
    /// <summary>
    /// Gets the number of elements currently in an Append-type ComputeBuffer on the GPU.
    /// </summary>
    /// <param name="appendBuffer">The buffer whose count should be retrieved.</param>
    /// <returns>The number of elements in the buffer.</returns>
    int GetAppendBufferCount(ComputeBuffer appendBuffer);
}