using System.Collections.Generic;
using UnityEngine;

using System.Linq;


public static class ParticleTransferService
{
    /// <summary>
    /// Hauptmethode: Verarbeitet die Ergebnisse eines Erode-Aufrufs.
    /// </summary>
    public static void ProcessTransfers(IParticleErodibleChunk sourceChunk, ITerrainParticleEroder eroder, IWorldManager world, Dictionary<int, int> counts)
    {
        for (int i = 0; i < 4; i++)
        {
            if (counts[i] == 0) continue;

            Direction dir = GetDirectionForEdgeBuffer(i);
            GridCoordinates neighborCoords = world.GetNeighborCoords(sourceChunk.Coordinates, dir);

            if (world.GetChunk(neighborCoords) is IParticleErodibleChunk erodibleNeighbor)
            {
                // GPU
                TransferGpuToGpu(eroder, sourceChunk, i, erodibleNeighbor);
                world.MarkChunkAsDirty(erodibleNeighbor);
            }
            else
            {
                // CPU neighbor nicht geladen 
                var particles = ReadbackBuffer(eroder, sourceChunk, i);
                world.AddPendingParticles(neighborCoords, particles);
            }
        }

        // Eck-Buffer index 4
        if (counts[4] > 0)
        {
            var sortedCorners = ReadbackAndSortCorners(eroder, sourceChunk);

            foreach (var transfer in sortedCorners)
            {
                if (transfer.Value.Count == 0) continue;

                GridCoordinates neighborCoords = world.GetNeighborCoords(sourceChunk.Coordinates, transfer.Key);

                if (world.GetChunk(neighborCoords) is IParticleErodibleChunk erodibleNeighbor)
                {
                    erodibleNeighbor.UploadPendingParticles(transfer.Value);
                    world.MarkChunkAsDirty(erodibleNeighbor);
                }
                else
                {
                    world.AddPendingParticles(neighborCoords, transfer.Value);
                }
            }
        }
    }

    #region Private Static Helper Methods

    private static void TransferGpuToGpu(ITerrainParticleEroder eroder, IParticleErodibleChunk source, int bufferIndex, IParticleErodibleChunk dest)
    {
        var transferShader = eroder.GetTransferShader();
        var sourceBuffer = eroder.GetOutgoingBuffers()[bufferIndex];
        int particleCount = eroder.GetAppendBufferCount(sourceBuffer);

        /**
        OutgoingParticle[] readParticles = new OutgoingParticle[particleCount];
        sourceBuffer.GetData(readParticles);
        foreach (var item in readParticles)
        {
            Debug.Log("Position" + item.particle.pos.ToString() + ", " + item.exitDirection.ToString());
        } 
        **/

        int kernel = transferShader.FindKernel("CopyAppendParticles");
        transferShader.SetBuffer(kernel, "Source", sourceBuffer);
        transferShader.SetBuffer(kernel, "Destination", dest.IncomingParticlesBuffer);

        int numThreadGroups = Mathf.CeilToInt(particleCount / 64f);
        transferShader.Dispatch(kernel, numThreadGroups, 1, 1);

        Debug.Log("Sucessful Transfer");
    }

    // TODO Optimierung Direction = NULL keine OUtgoing Particles mehr vereinfachung der architektur jeder bekommt ein label
    private static List<Particle> ReadbackBuffer(ITerrainParticleEroder eroder, IParticleErodibleChunk chunk, int bufferIndex)
    {
        var sourceBuffer = eroder.GetOutgoingBuffers()[bufferIndex];
        int particleCount = eroder.GetAppendBufferCount(sourceBuffer);
        if (particleCount == 0) return new List<Particle>();

        OutgoingParticle[] readParticles = new OutgoingParticle[particleCount];
        sourceBuffer.GetData(readParticles);
        return readParticles.Select(p => p.particle).ToList();
    }

    private static Dictionary<Direction, List<Particle>> ReadbackAndSortCorners(ITerrainParticleEroder eroder, IParticleErodibleChunk chunk)
    {
        var sortedCorners = new Dictionary<Direction, List<Particle>> {
            { Direction.NorthEast, new List<Particle>() }, { Direction.SouthEast, new List<Particle>() },
            { Direction.SouthWest, new List<Particle>() }, { Direction.NorthWest, new List<Particle>() }
        };

        var sourceBuffer = eroder.GetOutgoingBuffers()[4];
        int particleCount = eroder.GetAppendBufferCount(sourceBuffer);
        if (particleCount > 0)
        {
            OutgoingParticle[] cornerParticles = new OutgoingParticle[particleCount];
            sourceBuffer.GetData(cornerParticles);

            foreach (var op in cornerParticles)
            {
                if (sortedCorners.ContainsKey((Direction)op.exitDirection))
                {
                    sortedCorners[(Direction)op.exitDirection].Add(op.particle);
                }
            }
        }

        sortedCorners.TryGetValue(Direction.NorthEast, out var ne);
        Debug.Log("NE:" + ne.Count);
        sortedCorners.TryGetValue(Direction.NorthWest, out var nw);
        Debug.Log("NW:" + nw.Count);
        sortedCorners.TryGetValue(Direction.SouthEast, out var se);
        Debug.Log("SE:" + se.Count);
        sortedCorners.TryGetValue(Direction.SouthWest, out var sw);
        Debug.Log("SW:" + sw.Count);

        return sortedCorners;
    }

    private static Direction GetDirectionForEdgeBuffer(int index)
    {
        return index switch { 0 => Direction.North, 1 => Direction.East, 2 => Direction.South, 3 => Direction.West, _ => Direction.North };
    }

    #endregion
}
