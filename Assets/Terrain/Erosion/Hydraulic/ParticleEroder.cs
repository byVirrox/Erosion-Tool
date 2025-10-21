using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

[CreateAssetMenu(fileName = "NewParticleEroder", menuName = "Terrain/Particle Eroder Asset")]
public class ParticleEroder : ScriptableObject, IParticleEroder
{

    [Header("Dependencies")]
    public ComputeShader erosionShader;
    public ComputeShader particleTransferShader;
    public ErosionConfig config;

    private ComputeBuffer _brushIndicesBuffer;
    private ComputeBuffer _brushWeightsBuffer;
    private ComputeBuffer _particleStartBuffer;
    private ComputeBuffer _outgoingParticleBuffer;
    private ComputeBuffer _particleCountBuffer;


    private void OnEnable()
    {
        ReleaseAllBuffers();
    }


    public void Erode(IParticleErodibleChunk chunk, RenderTexture haloMap, int worldSeed)
    {
        EnsureBuffersAreInitialized();
        int borderSize = (haloMap.width - chunk.GetHeightMapData().width) / 2;

        RenderTexture debugTexture = null;

        int kernel = erosionShader.FindKernel("CSMain");
        PrepareParticleStartBuffer(chunk, borderSize, worldSeed);

        if (_particleStartBuffer != null && _particleStartBuffer.count > 0)
        {
            if (_outgoingParticleBuffer == null || _outgoingParticleBuffer.count < _particleStartBuffer.count)
            {
                _outgoingParticleBuffer?.Release();
                _outgoingParticleBuffer = new ComputeBuffer(_particleStartBuffer.count, Marshal.SizeOf<Particle>(), ComputeBufferType.Append);
            }

            SetShaderParameters((IChunk<RenderTexture>)chunk, haloMap, kernel);
            if (config.enableDebugTexture)
            {
                var debugDesc = haloMap.descriptor;
                debugDesc.colorFormat = RenderTextureFormat.ARGB32;
                debugTexture = RenderTexture.GetTemporary(debugDesc);
                
                Graphics.Blit(Texture2D.whiteTexture, debugTexture);
            }
            else
            {
                debugTexture = RenderTexture.GetTemporary(1, 1, 0, RenderTextureFormat.ARGB32);
            }

            debugTexture.enableRandomWrite = true;
            erosionShader.SetTexture(kernel, "DebugOutput", debugTexture);

            _outgoingParticleBuffer.SetCounterValue(0);
            int numThreadGroups = Mathf.CeilToInt(_particleStartBuffer.count / 1024f);
            erosionShader.Dispatch(kernel, numThreadGroups, 1, 1);
        }
        

        if (config.enableDebugTexture && debugTexture != null)
        {
            SaveRenderTextureAsPNG(debugTexture, $"DebugOutput_Chunk_{chunk.Coordinates.X}_{chunk.Coordinates.Y}.png");
        }

        if (debugTexture != null)
        {
            RenderTexture.ReleaseTemporary(debugTexture);
        }
    }

    private void SaveRenderTextureAsPNG(RenderTexture rt, string fileName)
    {
        RenderTexture.active = rt;
        Texture2D tex = new Texture2D(rt.width, rt.height, TextureFormat.RGBA32, false);
        tex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
        tex.Apply();
        RenderTexture.active = null;

        byte[] bytes = tex.EncodeToPNG();
        System.IO.File.WriteAllBytes(Application.dataPath + "/Terrain/Erosion/Hydraulic/DebugImages/" + fileName, bytes);
        Object.Destroy(tex);
        Debug.Log($"Debug-Textur gespeichert als: {fileName}");
    }

    public ComputeShader GetTransferShader() => particleTransferShader;
    public ComputeBuffer GetOutgoingBuffer() => _outgoingParticleBuffer;


    //TODO Auslagern 
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
        _outgoingParticleBuffer?.Release();
        _outgoingParticleBuffer = null;

        _particleCountBuffer = new ComputeBuffer(1, sizeof(int), ComputeBufferType.Raw);
    }


    private void PrepareParticleStartBuffer(IParticleErodibleChunk chunk, int borderSize, int worldSeed)
    {
        List<Particle> startParticles = new List<Particle>();

        int incomingCount = GetAppendBufferCount(chunk.IncomingParticlesBuffer);
        if (incomingCount > 0)
        {
            Particle[] incoming = new Particle[incomingCount];
            chunk.IncomingParticlesBuffer.GetData(incoming);
            chunk.ClearIncomingParticles();
            startParticles.AddRange(incoming);
            if (config.enableDebugParticleCount)
            {
                Debug.Log("Incoming Particle Count: " + incomingCount + " at (x: " + chunk.Coordinates.X + ", y:" + chunk.Coordinates.Y + ")");
            }
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
                    pos = new Vector2((float)randomX, (float)randomY),
                    dir = Vector2.zero,
                    speed = config.startSpeed,
                    water = config.startWater,
                    sediment = 0,
                    age = 0,
                    status = (int)ParticleStatus.InChunk,
                    haloResilience = rand.Next(0, Mathf.Max(config.haloZoneWidth - config.erosionBrushRadius, 1))
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
        erosionShader.SetBool("enableDebugOutput", config.enableDebugTexture);
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
}