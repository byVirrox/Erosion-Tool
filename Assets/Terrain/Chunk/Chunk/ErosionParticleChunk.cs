using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

/// <summary>
/// A concrete implementation of a chunk for the Unity engine.
/// It uses a RenderTexture to store its heightmap data and provides
/// engine-specific functionality like applying the heightmap to a Unity Terrain object.
/// </summary>
public class ErosionParticleChunk : BaseChunk, IChunk<RenderTexture>, IParticleErodibleChunk
{
    private RenderTexture heightMap;

    private ComputeBuffer _incomingParticlesBuffer;
    public ComputeBuffer IncomingParticlesBuffer => _incomingParticlesBuffer;

    public bool InitialParticlesDropped { get; set; }

    private const int maxIncomingParticles = 300000;
    private ComputeShader _transferShader;
    public int LastProcessedFrame { get; set; } = -1;


    public ErosionParticleChunk(GridCoordinates coordinates, RenderTexture pregeneratedHeightmap, ComputeShader transferShader) : base(coordinates)
    {
        this._transferShader = transferShader;
        this.heightMap = pregeneratedHeightmap;
        InitializeCommon(pregeneratedHeightmap.width);
    }

    private void InitializeCommon(int resolution)
    {
        _incomingParticlesBuffer = new ComputeBuffer(maxIncomingParticles, Marshal.SizeOf<Particle>(), ComputeBufferType.Append);
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

    public void AppendFromCPU(List<Particle> particles)
    {
        if (particles == null || particles.Count == 0) 
            return;

        ComputeBuffer tempCpuBuffer = new ComputeBuffer(particles.Count, Marshal.SizeOf<Particle>());
        tempCpuBuffer.SetData(particles);

        int kernel = _transferShader.FindKernel("CopyAppendParticles");
        _transferShader.SetBuffer(kernel, "Source", tempCpuBuffer);
        _transferShader.SetBuffer(kernel, "Destination", _incomingParticlesBuffer);

        int numThreadGroups = Mathf.CeilToInt(particles.Count / 64f);
        _transferShader.Dispatch(kernel, numThreadGroups, 1, 1);

        tempCpuBuffer.Release();
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

        RenderTexture.active = this.heightMap;

        terrain.terrainData.CopyActiveRenderTextureToHeightmap(
        new RectInt(0, 0, terrainResolution, terrainResolution),
            new Vector2Int(0, 0),
            TerrainHeightmapSyncControl.HeightAndLod
        );

        RenderTexture.active = null;
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
