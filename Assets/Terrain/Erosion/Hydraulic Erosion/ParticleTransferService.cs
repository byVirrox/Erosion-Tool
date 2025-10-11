using System.Collections.Generic;
using System.Linq;
using UnityEngine;


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
                    erodibleNeighbor.AppendFromCPU(transfer.Value, eroder.GetTransferShader());
                    world.MarkChunkAsDirty(erodibleNeighbor);
                }
                else
                {
                    world.AddPendingParticles(neighborCoords, transfer.Value);
                }
            }
        }




        eroder.ClearOutgoingBuffers();
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

        sourceBuffer.SetCounterValue(0);
    }

    // TODO Optimierung Direction = NULL keine OUtgoing Particles mehr vereinfachung der architektur jeder bekommt ein label
    private static List<OutgoingParticle> ReadbackBuffer(ITerrainParticleEroder eroder, IParticleErodibleChunk chunk, int bufferIndex)
    {
        var sourceBuffer = eroder.GetOutgoingBuffers()[bufferIndex];
        int particleCount = eroder.GetAppendBufferCount(sourceBuffer);
        if (particleCount == 0)
        {
            return new List<OutgoingParticle>();
        }  

        OutgoingParticle[] readParticles = new OutgoingParticle[particleCount];
        sourceBuffer.GetData(readParticles);

        sourceBuffer.SetCounterValue(0);

        return readParticles.ToList();
    }

    private static Dictionary<Direction, List<OutgoingParticle>> ReadbackAndSortCorners(ITerrainParticleEroder eroder, IParticleErodibleChunk chunk)
    {
        var sortedCorners = new Dictionary<Direction, List<OutgoingParticle>> {
            { Direction.NorthEast, new List<OutgoingParticle>() }, { Direction.SouthEast, new List<OutgoingParticle>() },
            { Direction.SouthWest, new List<OutgoingParticle>() }, { Direction.NorthWest, new List<OutgoingParticle>() }
        };

        var sourceBuffer = eroder.GetOutgoingBuffers()[4];
        int particleCount = sourceBuffer.count;
        if (particleCount > 0)
        {
            OutgoingParticle[] cornerParticles = new OutgoingParticle[particleCount];
            sourceBuffer.GetData(cornerParticles);

            foreach (var cornerParticle in cornerParticles)
            {
                if (sortedCorners.ContainsKey((Direction)cornerParticle.exitDirection))
                {
                    sortedCorners[(Direction)cornerParticle.exitDirection].Add(cornerParticle);
                }
            }
        }
        if ((eroder as ParticleTerrainEroder).log == true)
        {
            sortedCorners.TryGetValue(Direction.NorthEast, out var ne);
            Debug.Log("NE:" + ne.Count + " at x" + chunk.Coordinates.ToString());
            sortedCorners.TryGetValue(Direction.NorthWest, out var nw);
            Debug.Log("NW:" + nw.Count + " at x" + chunk.Coordinates.ToString());
            sortedCorners.TryGetValue(Direction.SouthEast, out var se);
            Debug.Log("SE:" + se.Count + " at x" + chunk.Coordinates.ToString());
            sortedCorners.TryGetValue(Direction.SouthWest, out var sw);
            Debug.Log("SW:" + sw.Count + " at x" + chunk.Coordinates.ToString());
            /**
            foreach (var lol in sw)
            {
                if (lol.particle.pos.x - 513 < 0 || lol.particle.pos.y - 513 < 0)
                Debug.Log("x:" + lol.particle.pos.x + ",y" + lol.particle.pos.y);
            }
                **/
   
        }

        sourceBuffer.SetCounterValue(0);

        return sortedCorners;
    }

    private static Direction GetDirectionForEdgeBuffer(int index)
    {
        return index switch { 0 => Direction.North, 1 => Direction.East, 2 => Direction.South, 3 => Direction.West, _ => Direction.North };
    }

    #endregion
}
