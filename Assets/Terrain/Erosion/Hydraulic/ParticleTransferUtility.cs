using System.Collections.Generic;


public static class ParticleTransferUtility
{
    public static void ProcessTransfers(IParticleErodibleChunk sourceChunk, IParticleEroder eroder, IParticleWorldManager world)
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
                //  collect for GPU-Transfer
                if (!gpuTransfers.ContainsKey(loadedNeighbor))
                {
                    gpuTransfers[loadedNeighbor] = new List<Particle>();
                }
                gpuTransfers[loadedNeighbor].Add(particle);
            }
            else
            {
                // Safe as"pending" 
                world.AddPendingParticles(neighborCoords, new List<Particle> { particle });
            }
        }

        // GPU Transfer
        foreach (var transfer in gpuTransfers)
        {
            var neighborChunk = transfer.Key;
            var particlesToTransfer = transfer.Value;

            neighborChunk.AppendFromCPU(particlesToTransfer, eroder.GetTransferShader());
            world.MarkChunkAsDirty(neighborChunk);
        }
    }
}
