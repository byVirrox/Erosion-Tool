using System.Collections.Generic;
using System.Linq;
using UnityEngine;


public static class ParticleTransferService
{
    /// <summary>
    /// Hauptmethode: Verarbeitet die Ergebnisse eines Erode-Aufrufs.
    /// </summary>
    public static void ProcessTransfers(IParticleErodibleChunk sourceChunk, ITerrainParticleEroder eroder, IParticleWorldManager world)
    {
        var outgoingBuffer = eroder.GetOutgoingBuffer();
        int totalOutgoingCount = eroder.GetAppendBufferCount(outgoingBuffer);

        if (totalOutgoingCount == 0)
        {
            eroder.ClearOutgoingBuffer();
            return;
        }

        Particle[] allParticles = new Particle[totalOutgoingCount];
        outgoingBuffer.GetData(allParticles);

        eroder.ClearOutgoingBuffer();

        var gpuTransfers = new Dictionary<IParticleErodibleChunk, List<Particle>>();

        foreach (var particle in allParticles)
        {
            ParticleStatus status = (ParticleStatus)particle.status;
            if (status == ParticleStatus.InChunk) continue;

            NeighborDirection targetDir = (NeighborDirection)status;
            GridCoordinates neighborCoords = WorldCoordinateUtils.GetNeighborCoords(sourceChunk.Coordinates, targetDir);

            if (world.GetChunk(neighborCoords) is IParticleErodibleChunk loadedNeighbor)
            {
                // Fall 1: Nachbar ist geladen -> Für GPU-Transfer sammeln
                if (!gpuTransfers.ContainsKey(loadedNeighbor))
                {
                    gpuTransfers[loadedNeighbor] = new List<Particle>();
                }
                gpuTransfers[loadedNeighbor].Add(particle);
            }
            else
            {
                // Fall 2: Nachbar nicht geladen -> Als "pending" für später speichern
                world.AddPendingParticles(neighborCoords, new List<Particle> { particle });
            }
        }

        // 4. Gesammelte GPU-Transfers ausführen
        foreach (var transfer in gpuTransfers)
        {
            var neighborChunk = transfer.Key;
            var particlesToTransfer = transfer.Value;

            neighborChunk.AppendFromCPU(particlesToTransfer, eroder.GetTransferShader());
            world.MarkChunkAsDirty(neighborChunk);
        }
    }
}
