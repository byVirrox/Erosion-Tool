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
    /// <summary>
    Particle[] Erode(IParticleErodibleChunk chunk, RenderTexture haloMap, int worldSeed);
}