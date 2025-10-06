using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

/// <summary>
/// A concrete implementation of a chunk for the Unity engine.
/// It uses a RenderTexture to store its heightmap data and provides
/// engine-specific functionality like applying the heightmap to a Unity Terrain object.
/// </summary>
public class UnityChunk : BaseChunk, IChunk<RenderTexture>, IParticleErodibleChunk
{
    private RenderTexture heightMap;

    private ComputeBuffer _incomingParticlesBuffer;
    public ComputeBuffer IncomingParticlesBuffer => _incomingParticlesBuffer;

    public bool InitialParticlesDropped { get; set; }


    public UnityChunk(GridCoordinates coordinates, RenderTexture pregeneratedHeightmap) : base(coordinates)
    {
        this.heightMap = pregeneratedHeightmap;
        InitializeCommon(pregeneratedHeightmap.width);
    }

    public UnityChunk(GridCoordinates coordinates, int resolution) : base(coordinates)
    {
        this.heightMap = new RenderTexture(resolution, resolution, 0, RenderTextureFormat.RFloat);
        this.heightMap.enableRandomWrite = true;
        this.heightMap.Create();

        InitializeCommon(resolution);
    }

    private void InitializeCommon(int resolution)
    {
        int particleStructSize = Marshal.SizeOf<Particle>();
        int maxIncomingParticles = 300000;
        _incomingParticlesBuffer = new ComputeBuffer(maxIncomingParticles, particleStructSize, ComputeBufferType.Append);
        ClearIncomingParticles();

        InitialParticlesDropped = false;
    }


    #region IChunk<RenderTexture> Implementation

    public RenderTexture GetHeightMapData()
    {
        return this.heightMap;
    }

    public void SetHeightMapData(RenderTexture data)
    {
        this.heightMap = data;
    }

    #endregion

    #region Interface Implementations

    public void UploadPendingParticles(List<Particle> particles)
    {
        if (particles == null || particles.Count == 0 || _incomingParticlesBuffer == null) return;

        if (particles.Count > _incomingParticlesBuffer.count)
        {
            Debug.LogWarning($"Zu viele ankommende Partikel ({particles.Count}) für Chunk {Coordinates}. Kürze auf die Buffer-Kapazität von {_incomingParticlesBuffer.count}.");
            int startIndex = particles.Count - _incomingParticlesBuffer.count;
            particles = particles.GetRange(startIndex, _incomingParticlesBuffer.count);
        }

        _incomingParticlesBuffer.SetData(particles);

        _incomingParticlesBuffer.SetCounterValue((uint)particles.Count);
    }

    public void ClearIncomingParticles()
    {
        _incomingParticlesBuffer?.SetCounterValue(0);
    }

    #endregion

    #region Unity-Specific Functionality
    public void ApplyToTerrain(Terrain terrain)
    {
        if (terrain == null || this.heightMap == null)
        {
            Debug.LogError("Terrain or HeightMap is null.");
            return;
        }

        int terrainResolution = terrain.terrainData.heightmapResolution;
        if (this.heightMap.width != terrainResolution || this.heightMap.height != terrainResolution)
        {
            Debug.LogError("RenderTexture resolution does not match terrain heightmap resolution.");
            return;
        }

        Texture2D tempTexture = new Texture2D(terrainResolution, terrainResolution, TextureFormat.RFloat, false);

        RenderTexture.active = this.heightMap;
        tempTexture.ReadPixels(new Rect(0, 0, terrainResolution, terrainResolution), 0, 0);
        tempTexture.Apply();
        RenderTexture.active = null;

        float[,] heights = new float[terrainResolution, terrainResolution];
        Color[] pixels = tempTexture.GetPixels();

        for (int y = 0; y < terrainResolution; y++)
        {
            for (int x = 0; x < terrainResolution; x++)
            {
                heights[y, x] = pixels[y * terrainResolution + x].r;
            }
        }

        terrain.terrainData.SetHeights(0, 0, heights);

        Object.Destroy(tempTexture);
    }

    public void ReleaseResources()
    {
        if (this.heightMap != null)
        {
            RenderTexture.ReleaseTemporary(this.heightMap); 
            this.heightMap = null;
        }
        _incomingParticlesBuffer?.Release();
        _incomingParticlesBuffer = null;
    }

    #endregion
}
