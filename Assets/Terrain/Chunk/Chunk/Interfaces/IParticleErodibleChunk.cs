using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Defines the contract for a chunk that can be processed by the particle-based erosion system.
/// Its "inbox" for particles is a ComputeBuffer on the GPU.
/// </summary>
public interface IParticleErodibleChunk : IChunk<RenderTexture>
{
    /// <summary>
    /// Indicates whether the initial shower of random particles has already been dropped on this chunk.
    /// </summary>
    bool InitialParticlesDropped { get; set; }

    /// <summary>
    /// The persistent "mailbox" buffer on the GPU for incoming particles.
    /// </summary>
    ComputeBuffer IncomingParticlesBuffer { get; }

    /// <summary>
    /// Clears the incoming particle buffer on the GPU by setting its counter to zero.
    /// </summary>
    void ClearIncomingParticles();

    /// <summary>
    /// Uploads a list of particles from the CPU to the chunk's GPU "inbox" buffer.
    /// Used for pending particles when a chunk is first loaded.
    /// </summary>
    /// <param name="particles">The list of particles to upload.</param>
    void AppendFromCPU(List<Particle> particles, ComputeShader transferShader);
}