using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Windows;

[CreateAssetMenu(fileName = "NewParticleEroder", menuName = "Terrain/Particle Eroder Asset")]
public class ParticleTerrainEroder : ScriptableObject, ITerrainParticleEroder
{

    [Header("Dependencies")]
    public ComputeShader erosionShader;
    public ComputeShader particleTransferShader;
    public ErosionConfig config;
    public bool log;

    private ComputeBuffer _brushIndicesBuffer;
    private ComputeBuffer _brushWeightsBuffer;
    private ComputeBuffer _particleStartBuffer;
    private ComputeBuffer _outgoingParticleBuffer;
    private ComputeBuffer _particleCountBuffer;


    private void OnEnable()
    {
        ReleaseAllBuffers();
    }


    public ErosionResult Erode(IParticleErodibleChunk chunk, RenderTexture haloMap, int worldSeed)
    {
        EnsureBuffersAreInitialized();
        int borderSize = config.erosionBrushRadius;

        int kernel = erosionShader.FindKernel("CSMain");
        PrepareParticleStartBuffer(chunk, borderSize, worldSeed);

        if (_particleStartBuffer != null && _particleStartBuffer.count > 0)
        {
            // Prüfe, ob der outgoingBuffer groß genug ist. Wenn nicht, erstelle ihn neu.
            // Die Kapazität muss mindestens so groß sein wie die Anzahl der startenden Partikel.
            if (_outgoingParticleBuffer == null || _outgoingParticleBuffer.count < _particleStartBuffer.count)
            {
                _outgoingParticleBuffer?.Release();
                _outgoingParticleBuffer = new ComputeBuffer(_particleStartBuffer.count, Marshal.SizeOf<Particle>(), ComputeBufferType.Append);
            }

            SetShaderParameters((IChunk<RenderTexture>)chunk, haloMap, kernel);
            _outgoingParticleBuffer.SetCounterValue(0);
            int numThreadGroups = Mathf.CeilToInt(_particleStartBuffer.count / 1024f);
            erosionShader.Dispatch(kernel, numThreadGroups, 1, 1);
        }

        List<IChunk> dirtiedNeighbors = DeconstructHaloMap(haloMap, chunk, borderSize);

        RenderTexture.ReleaseTemporary(haloMap);

        return new ErosionResult { DirtiedNeighbors = dirtiedNeighbors };
    }

    public ComputeShader GetTransferShader() => particleTransferShader;
    public ComputeBuffer GetOutgoingBuffer() => _outgoingParticleBuffer;


    //TODO Auslagern in static class
    public int GetAppendBufferCount(ComputeBuffer appendBuffer)
    {
        if (appendBuffer == null)
        {  
            return 0; 
        }
        if (_particleCountBuffer == null)
        {
            EnsureBuffersAreInitialized();
        }
        _particleCountBuffer.SetData(new int[] { 0 });

        ComputeBuffer.CopyCount(appendBuffer, _particleCountBuffer, 0);

        int[] count = new int[1];
        _particleCountBuffer.GetData(count);

        return count[0];
    }

    private void EnsureBuffersAreInitialized()
    {
        if (_brushIndicesBuffer != null)
        {
            return;
        }

        Debug.Log("Initializing Eroder Buffers...");

        var brushIndexOffsets = new List<Vector2Int>();
        var brushWeights = new List<float>();
        if (config != null)
        {
            float weightSum = 0;
            for (int brushY = -config.erosionBrushRadius; brushY <= config.erosionBrushRadius; brushY++)
            {
                for (int brushX = -config.erosionBrushRadius; brushX <= config.erosionBrushRadius; brushX++)
                {
                    float sqrDst = brushX * brushX + brushY * brushY;
                    if (sqrDst < config.erosionBrushRadius * config.erosionBrushRadius)
                    {
                        brushIndexOffsets.Add(new Vector2Int(brushX, brushY));
                        float brushWeight = 1 - Mathf.Sqrt(sqrDst) / config.erosionBrushRadius;
                        weightSum += brushWeight;
                        brushWeights.Add(brushWeight);
                    }
                }
            }
            for (int i = 0; i < brushWeights.Count; i++)
            {
                brushWeights[i] /= weightSum;
            }
        }

        _brushIndicesBuffer = new ComputeBuffer(Mathf.Max(1, brushIndexOffsets.Count), Marshal.SizeOf<Vector2Int>());
        if (brushIndexOffsets.Count > 0) _brushIndicesBuffer.SetData(brushIndexOffsets);

        _brushWeightsBuffer = new ComputeBuffer(Mathf.Max(1, brushWeights.Count), sizeof(float));
        if (brushWeights.Count > 0) _brushWeightsBuffer.SetData(brushWeights);

        int maxOutgoing = (config != null) ? config.numErosionIterations : 1024;
        int particleStructSize = Marshal.SizeOf<Particle>();
        _outgoingParticleBuffer = new ComputeBuffer(Mathf.Max(1, maxOutgoing), particleStructSize, ComputeBufferType.Append);

        _particleCountBuffer = new ComputeBuffer(1, sizeof(int), ComputeBufferType.Raw);
    }

    // TODO maybe optimieren direkt aus Bufferr lesen 
    private void PrepareParticleStartBuffer(IParticleErodibleChunk chunk, int borderSize, int worldSeed)
    {
        List<Particle> startParticles = new List<Particle>();

        // GPU readback
        int incomingCount = GetAppendBufferCount(chunk.IncomingParticlesBuffer);
        if (incomingCount > 0)
        {
            Particle[] incoming = new Particle[incomingCount];
            chunk.IncomingParticlesBuffer.GetData(incoming);
            chunk.ClearIncomingParticles();
            startParticles.AddRange(incoming);
            Debug.Log("Incoming Particles" +  incomingCount + "at x " + chunk.Coordinates.X + ",y " + chunk.Coordinates.Y);
        }
        chunk.ClearIncomingParticles();

        if (!chunk.InitialParticlesDropped)
        {
            int chunkSeed = WorldHash.GetChunkSeed(chunk.Coordinates, worldSeed);
            System.Random rand = new System.Random(chunkSeed);
            
            int mapRes = (chunk as IChunk<RenderTexture>).GetHeightMapData().width;

            for (int i = 0; i < config.numErosionIterations; i++)
            {
                int randomX = rand.Next(borderSize, mapRes + borderSize);
                int randomY = rand.Next(borderSize, mapRes + borderSize);

                startParticles.Add(new Particle
                {
                    pos = new Vector2((float) randomX, (float) randomY),
                    dir = Vector2.zero,
                    speed = config.startSpeed,
                    water = config.startWater,
                    sediment = 0,
                    lifetime = 0,
                    status = (int)ParticleStatus.InChunk,
                });
            }
            chunk.InitialParticlesDropped = true;
        }

        if (_particleStartBuffer == null || _particleStartBuffer.count != startParticles.Count)
        {
            _particleStartBuffer?.Release();
            if (startParticles.Count > 0)
            {
                _particleStartBuffer = new ComputeBuffer(startParticles.Count, Marshal.SizeOf<Particle>());
            }
            else
            {
                _particleStartBuffer = null;
            }
        }

        if (_particleStartBuffer != null)
        {

            _particleStartBuffer.SetData(startParticles);
        }
    }


    private void SetShaderParameters(IChunk<RenderTexture> chunk, RenderTexture haloMap, int kernel)
    {
        int mapSizeWithBorder = haloMap.width;
        int borderSize = (mapSizeWithBorder - chunk.GetHeightMapData().width) / 2;

        erosionShader.SetTexture(kernel, "map", haloMap);
        erosionShader.SetBuffer(kernel, "brushIndices", _brushIndicesBuffer);
        erosionShader.SetBuffer(kernel, "brushWeights", _brushWeightsBuffer);

        erosionShader.SetBuffer(kernel, "initialParticles", _particleStartBuffer);
        erosionShader.SetBuffer(kernel, "outgoingParticles", _outgoingParticleBuffer);

        erosionShader.SetInt("mapSizeWithBorder", mapSizeWithBorder);
        erosionShader.SetInt("borderSize", borderSize);
        erosionShader.SetInt("brushLength", _brushIndicesBuffer.count);
        erosionShader.SetInt("maxLifetime", config.maxLifetime);
        erosionShader.SetFloat("inertia", config.inertia);
        erosionShader.SetFloat("gravity", config.gravity);
        erosionShader.SetFloat("startSpeed", config.startSpeed);
        erosionShader.SetFloat("startWater", config.startWater);
        erosionShader.SetFloat("evaporateSpeed", config.evaporateSpeed);
        erosionShader.SetFloat("sedimentCapacityFactor", config.sedimentCapacityFactor);
        erosionShader.SetFloat("minSedimentCapacity", config.minSedimentCapacity);
        erosionShader.SetFloat("depositSpeed", config.depositSpeed);
        erosionShader.SetFloat("erodeSpeed", config.erodeSpeed);
    }

    /**
    private RenderTexture BuildHaloMap(IChunk<RenderTexture> chunk, int borderSize, IGraphGenerator generator, TerrainRuntimeGraph graph)
    {
        RenderTexture sourceMap = chunk.GetHeightMapData();
        if (sourceMap == null) return null;

        int resolution = sourceMap.width;
        int borderedResolution = resolution + borderSize * 2;

        RenderTexture haloMap = generator.Execute(graph, chunk.Coordinates, resolution, borderSize);

        foreach (NeighborDirection dir in System.Enum.GetValues(typeof(NeighborDirection)))
        {
            if (chunk.GetNeighbor(dir) is IChunk<RenderTexture> neighbor)
            {
                CopyNeighborBorder(haloMap, neighbor.GetHeightMapData(), dir, borderSize);
            }
        }

        Graphics.CopyTexture(sourceMap, 0, 0, 0, 0, resolution, resolution, haloMap, 0, 0, borderSize, borderSize);

        return haloMap;
    }
    **/

    /// <summary>
    /// Deconstructs the halo map after erosion, copying the modified center back to the source chunk
    /// and the modified borders back to the neighboring chunks.
    /// </summary>
    /// <returns>A list of neighbors that were modified and should be marked as dirty.</returns>
    private List<IChunk> DeconstructHaloMap(RenderTexture haloMap, IParticleErodibleChunk sourceChunk, int borderSize)
    {
        var dirtiedNeighbors = new List<IChunk>();

        foreach (NeighborDirection dir in System.Enum.GetValues(typeof(NeighborDirection)))
        {
            if (sourceChunk.GetNeighbor(dir) is IChunk<RenderTexture> neighbor)
            {
                CommitBorderToNeighbor(haloMap, neighbor, dir, borderSize);
                dirtiedNeighbors.Add(neighbor);
            }
        }

        int resolution = sourceChunk.GetHeightMapData().width;
        Graphics.CopyTexture(haloMap, 0, 0, borderSize, borderSize, resolution, resolution, sourceChunk.GetHeightMapData(), 0, 0, 0, 0);

        return dirtiedNeighbors;
    }

    /// <summary>
    /// Copies the modified border data FROM the haloMap BACK to the neighbor's permanent texture.
    /// </summary>
    private void CommitBorderToNeighbor(RenderTexture haloMap, IChunk<RenderTexture> neighbor, NeighborDirection directionOfNeighbor, int borderSize)
    {
        BorderCopyData copyData = GetBorderCopyData(directionOfNeighbor, neighbor.GetHeightMapData().width, borderSize);

        Graphics.CopyTexture(
            haloMap, 0, 0,
            copyData.DestinationOrigin.x, copyData.DestinationOrigin.y,
            copyData.SourceRect.width, copyData.SourceRect.height,
            neighbor.GetHeightMapData(), 0, 0,
            copyData.SourceRect.x, copyData.SourceRect.y
        );
    }

    private void CopyNeighborBorder(RenderTexture haloMap, RenderTexture neighborMap, NeighborDirection dir, int borderSize)
    {
        BorderCopyData copyData = GetBorderCopyData(dir, neighborMap.width, borderSize);

        Graphics.CopyTexture(
            neighborMap, 0, 0,
            copyData.SourceRect.x,
            copyData.SourceRect.y,
            copyData.SourceRect.width,
            copyData.SourceRect.height,
            haloMap, 0, 0,
            copyData.DestinationOrigin.x,
            copyData.DestinationOrigin.y
        );
    }

    // TODO Test schreiben
    private BorderCopyData GetBorderCopyData(NeighborDirection dir, int resolution, int borderSize)
    {
        int borderedResolution = resolution + borderSize * 2;

        return dir switch
        {
            NeighborDirection.North => new BorderCopyData
            {
                SourceRect = new RectInt(0, 0, resolution, borderSize),
                DestinationOrigin = new Vector2Int(borderSize, borderedResolution - borderSize)
            },
            NeighborDirection.East => new BorderCopyData
            {
                SourceRect = new RectInt(0, 0, borderSize, resolution),
                DestinationOrigin = new Vector2Int(borderedResolution - borderSize, borderSize)
            },
            NeighborDirection.South => new BorderCopyData
            {
                SourceRect = new RectInt(0, resolution - borderSize, resolution, borderSize),
                DestinationOrigin = new Vector2Int(borderSize, 0)
            },
            NeighborDirection.West => new BorderCopyData
            {
                SourceRect = new RectInt(resolution - borderSize, 0, borderSize, resolution),
                DestinationOrigin = new Vector2Int(0, borderSize)
            },
            NeighborDirection.NorthEast => new BorderCopyData
            {
                SourceRect = new RectInt(0, 0, borderSize, borderSize),
                DestinationOrigin = new Vector2Int(borderedResolution - borderSize, borderedResolution - borderSize)
            },
            NeighborDirection.SouthEast => new BorderCopyData
            {
                SourceRect = new RectInt(0, resolution - borderSize, borderSize, borderSize),
                DestinationOrigin = new Vector2Int(borderedResolution - borderSize, 0)
            },
            NeighborDirection.SouthWest => new BorderCopyData
            {
                SourceRect = new RectInt(resolution - borderSize, resolution - borderSize, borderSize, borderSize),
                DestinationOrigin = new Vector2Int(0, 0)
            },
            NeighborDirection.NorthWest => new BorderCopyData
            {
                SourceRect = new RectInt(resolution - borderSize, 0, borderSize, borderSize),
                DestinationOrigin = new Vector2Int(0, borderedResolution - borderSize)
            },
            _ => new BorderCopyData
            {
                SourceRect = new RectInt(0, 0, 0, 0),
                DestinationOrigin = new Vector2Int(0, 0)
            }
        };
    }

    private void OnDisable()
    {
        ReleaseAllBuffers();
    }

    public void ClearOutgoingBuffer()
    {
        _outgoingParticleBuffer?.SetCounterValue(0);
    }

    private void ReleaseAllBuffers()
    {
        _brushIndicesBuffer?.Release();
        _brushWeightsBuffer?.Release();
        _particleStartBuffer?.Release();
        _particleCountBuffer?.Release();
        _outgoingParticleBuffer?.Release();
        _brushIndicesBuffer = null;
        _brushWeightsBuffer = null;
        _particleStartBuffer = null;
        _particleCountBuffer = null;
        _outgoingParticleBuffer = null;
    }


    private struct BorderCopyData
    {
        public RectInt SourceRect;
        public Vector2Int DestinationOrigin;
    }
}