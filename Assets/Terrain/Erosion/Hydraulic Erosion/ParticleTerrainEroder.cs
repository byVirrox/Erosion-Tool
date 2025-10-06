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
    public ComputeShader textureToBufferShader;
    public ComputeShader bufferToTextureShader;
    public ErosionConfig config;

    private ComputeBuffer _brushIndicesBuffer;
    private ComputeBuffer _brushWeightsBuffer;
    private ComputeBuffer _particleStartBuffer;
    private ComputeBuffer[] _outgoingParticleBuffers;
    private ComputeBuffer _particleCountBuffer;


    private void OnEnable()
    {
        ReleaseAllBuffers();
    }


    public ErosionResult Erode(IParticleErodibleChunk chunk, INoiseGenerator generator, int worldSeed)
    {
        EnsureBuffersAreInitialized();
        int borderSize = config.erosionBrushRadius;

        RenderTexture haloMap = BuildHaloMap(chunk, borderSize, generator);

        int kernel = erosionShader.FindKernel("CSMain");
        PrepareParticleStartBuffer(chunk, borderSize, worldSeed);

        if (_particleStartBuffer != null && _particleStartBuffer.count > 0)
        {
            SetShaderParameters((IChunk<RenderTexture>)chunk, haloMap, kernel);
            foreach (var buffer in _outgoingParticleBuffers) { buffer?.SetCounterValue(0); }
            int numThreadGroups = Mathf.CeilToInt(_particleStartBuffer.count / 1024f);
            erosionShader.Dispatch(kernel, numThreadGroups, 1, 1);
        }

        List<IChunk> dirtiedNeighbors = DeconstructHaloMap(haloMap, chunk, borderSize);

        var counts = new Dictionary<int, int>();
        for (int i = 0; i < 5; i++)
        {
            counts[i] = GetAppendBufferCount(_outgoingParticleBuffers[i]);
        }

        RenderTexture.ReleaseTemporary(haloMap);

        return new ErosionResult { OutgoingParticleCounts = counts, DirtiedNeighbors = dirtiedNeighbors };
    }

    public ComputeShader GetTransferShader() => particleTransferShader;
    public ComputeBuffer[] GetOutgoingBuffers() => _outgoingParticleBuffers;


    //TODO Auslagern in static class
    public int GetAppendBufferCount(ComputeBuffer appendBuffer)
    {
        if (appendBuffer == null) return 0;
        if (_particleCountBuffer == null) EnsureBuffersAreInitialized();

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

        _outgoingParticleBuffers = new ComputeBuffer[5];
        int maxOutgoing = (config != null) ? config.numErosionIterations : 1024;
        int outgoingStructSize = Marshal.SizeOf<OutgoingParticle>();
        for (int i = 0; i < 5; i++)
        {
            _outgoingParticleBuffers[i] = new ComputeBuffer(Mathf.Max(1, maxOutgoing), outgoingStructSize, ComputeBufferType.Append);
        }

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
            startParticles.AddRange(incoming);
        }
        chunk.ClearIncomingParticles();

        if (!chunk.InitialParticlesDropped)
        {
            int chunkSeed = WorldHash.GetChunkSeed(chunk.Coordinates, worldSeed);
            System.Random rand = new System.Random(chunkSeed);
            
            int numNewParticles = config.numErosionIterations;
            int mapRes = (chunk as IChunk<RenderTexture>).GetHeightMapData().width;

            for (int i = 0; i < numNewParticles; i++)
            {
                int randomX = rand.Next(0, mapRes + 1);
                int randomY = rand.Next(0, mapRes + 1 );

                startParticles.Add(new Particle
                {
                    pos = new Vector2(randomX, randomY) + new Vector2(borderSize, borderSize),
                    dir = Vector2.zero,
                    speed = config.startSpeed,
                    water = config.startWater,
                    sediment = 0,
                    lifetime = 0
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
        int mapResolution = chunk.GetHeightMapData().width;
        int borderSize = (mapSizeWithBorder - mapResolution) / 2;

        erosionShader.SetTexture(kernel, "map", haloMap);
        erosionShader.SetBuffer(kernel, "brushIndices", _brushIndicesBuffer);
        erosionShader.SetBuffer(kernel, "brushWeights", _brushWeightsBuffer);

        erosionShader.SetBuffer(kernel, "initialParticles", _particleStartBuffer);
        erosionShader.SetBuffer(kernel, "outgoingParticlesN", _outgoingParticleBuffers[0]);
        erosionShader.SetBuffer(kernel, "outgoingParticlesE", _outgoingParticleBuffers[1]);
        erosionShader.SetBuffer(kernel, "outgoingParticlesS", _outgoingParticleBuffers[2]);
        erosionShader.SetBuffer(kernel, "outgoingParticlesW", _outgoingParticleBuffers[3]);
        erosionShader.SetBuffer(kernel, "outgoingParticlesCorners", _outgoingParticleBuffers[4]);

        erosionShader.SetInt("mapSizeWithBorder", mapSizeWithBorder);
        erosionShader.SetInt("mapResolution", mapResolution);
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

    private RenderTexture BuildHaloMap(IChunk<RenderTexture> chunk, int borderSize, INoiseGenerator generator)
    {
        RenderTexture sourceMap = chunk.GetHeightMapData();
        if (sourceMap == null) return null;

        int resolution = sourceMap.width;
        int borderedResolution = resolution + borderSize * 2;

        RenderTexture haloMap = generator.GenerateTexture(chunk.Coordinates, resolution, borderSize);

        foreach (Direction dir in System.Enum.GetValues(typeof(Direction)))
        {
            if (chunk.GetNeighbor(dir) is IChunk<RenderTexture> neighbor)
            {
                CopyNeighborBorder(haloMap, neighbor.GetHeightMapData(), dir, borderSize);
            }
        }

        Graphics.CopyTexture(sourceMap, 0, 0, 0, 0, resolution, resolution, haloMap, 0, 0, borderSize, borderSize);

        return haloMap;
    }

    /// <summary>
    /// Deconstructs the halo map after erosion, copying the modified center back to the source chunk
    /// and the modified borders back to the neighboring chunks.
    /// </summary>
    /// <returns>A list of neighbors that were modified and should be marked as dirty.</returns>
    private List<IChunk> DeconstructHaloMap(RenderTexture haloMap, IParticleErodibleChunk sourceChunk, int borderSize)
    {
        var dirtiedNeighbors = new List<IChunk>();

        foreach (Direction dir in System.Enum.GetValues(typeof(Direction)))
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
    private void CommitBorderToNeighbor(RenderTexture haloMap, IChunk<RenderTexture> neighbor, Direction directionOfNeighbor, int borderSize)
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

    private void CopyNeighborBorder(RenderTexture haloMap, RenderTexture neighborMap, Direction dir, int borderSize)
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
    private BorderCopyData GetBorderCopyData(Direction dir, int resolution, int borderSize)
    {
        int borderedResolution = resolution + borderSize * 2;

        return dir switch
        {
            Direction.North => new BorderCopyData
            {
                SourceRect = new RectInt(0, 0, resolution, borderSize),
                DestinationOrigin = new Vector2Int(borderSize, borderedResolution - borderSize)
            },
            Direction.East => new BorderCopyData
            {
                SourceRect = new RectInt(0, 0, borderSize, resolution),
                DestinationOrigin = new Vector2Int(borderedResolution - borderSize, borderSize)
            },
            Direction.South => new BorderCopyData
            {
                SourceRect = new RectInt(0, resolution - borderSize, resolution, borderSize),
                DestinationOrigin = new Vector2Int(borderSize, 0)
            },
            Direction.West => new BorderCopyData
            {
                SourceRect = new RectInt(resolution - borderSize, 0, borderSize, resolution),
                DestinationOrigin = new Vector2Int(0, borderSize)
            },
            Direction.NorthEast => new BorderCopyData
            {
                SourceRect = new RectInt(0, 0, borderSize, borderSize),
                DestinationOrigin = new Vector2Int(borderedResolution - borderSize, borderedResolution - borderSize)
            },
            Direction.SouthEast => new BorderCopyData
            {
                SourceRect = new RectInt(0, resolution - borderSize, borderSize, borderSize),
                DestinationOrigin = new Vector2Int(borderedResolution - borderSize, 0)
            },
            Direction.SouthWest => new BorderCopyData
            {
                SourceRect = new RectInt(resolution - borderSize, resolution - borderSize, borderSize, borderSize),
                DestinationOrigin = new Vector2Int(0, 0)
            },
            Direction.NorthWest => new BorderCopyData
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

    private void ReleaseAllBuffers()
    {
        _brushIndicesBuffer?.Release();
        _brushWeightsBuffer?.Release();
        _particleStartBuffer?.Release();
        _particleCountBuffer?.Release();
        if (_outgoingParticleBuffers != null)
        {
            foreach (var buffer in _outgoingParticleBuffers)
            {
                buffer?.Release();
            }
        }

        _brushIndicesBuffer = null;
        _brushWeightsBuffer = null;
        _particleStartBuffer = null;
        _particleCountBuffer = null;
        _outgoingParticleBuffers = null;
    }


    private struct BorderCopyData
    {
        public RectInt SourceRect;
        public Vector2Int DestinationOrigin;
    }
}