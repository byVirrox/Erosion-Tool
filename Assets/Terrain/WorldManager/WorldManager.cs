using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class WorldManager : AbstractWorldManager<UnityChunk, UnityChunkFactory>, IParticleWorldManager
{
    public class UnloadedChunkData
    {
        public RenderTexture Heightmap;
        public List<Particle> PendingParticles = new List<Particle>();
        public bool InitialParticlesDropped;
    }

    [Header("System-Specific Dependencies")]
    [SerializeField] private ParticleTerrainEroder eroder;

    [Header("Performance Settings")]
    [Tooltip("Die maximale Anzahl an Chunks, die pro Frame erodiert werden sollen.")]
    [SerializeField] private int maxChunksToProcessPerFrame = 5;

    [Header("Visuals")]
    [SerializeField] private GameObject terrainPrefab;

    private ITerrainParticleEroder _eroder;

    private readonly Dictionary<GridCoordinates, Terrain> _activeTerrainObjects = new Dictionary<GridCoordinates, Terrain>();

    private readonly Dictionary<GridCoordinates, UnloadedChunkData> _unloadedChunkCache = new Dictionary<GridCoordinates, UnloadedChunkData>();

    private readonly Queue<UnityChunk> _dirtyChunksQueue = new Queue<UnityChunk>();
    private readonly HashSet<UnityChunk> _chunksInQueue = new HashSet<UnityChunk>();
    private ITerrainGenerator _terrainGenerator;

    #region Overridden Base Methods

    protected override void InitializeServices()
    {
        _eroder = eroder;
        _terrainGenerator = chunkFactory.GetGenerator();
    }

    protected override void LoadChunk(GridCoordinates coords)
    {
        UnityChunk newChunk;

        if (_unloadedChunkCache.TryGetValue(coords, out var cachedData))
        {
            newChunk = new UnityChunk(coords, cachedData.Heightmap);
            newChunk.InitialParticlesDropped = cachedData.InitialParticlesDropped;

            if (cachedData.PendingParticles.Count > 0)
            {
                newChunk.AppendFromCPU(cachedData.PendingParticles, _eroder.GetTransferShader());
            }

            _unloadedChunkCache.Remove(coords);
        }
        else
        {
            newChunk = chunkFactory.CreateChunk(coords, worldConfig.worldSeed);
        }

        if (newChunk == null) return;

        _activeChunks.Add(coords, newChunk);
        SpawnChunkGameObject(newChunk);

        foreach (NeighborDirection dir in System.Enum.GetValues(typeof(NeighborDirection)))
        {
            GridCoordinates neighborCoords = WorldCoordinateUtils.GetNeighborCoords(coords, dir);
            if (_activeChunks.TryGetValue(neighborCoords, out UnityChunk neighbor))
            {
                newChunk.SetNeighbor(dir, neighbor);
                neighbor.SetNeighbor(WorldCoordinateUtils.GetOppositeDirection(dir), newChunk);
            }
        }

        MarkChunkAsDirty(newChunk);
    }

    protected override void UnloadChunk(GridCoordinates coords)
    {
        if (_activeChunks.TryGetValue(coords, out UnityChunk chunkToUnload))
        {
            if (!_unloadedChunkCache.TryGetValue(coords, out var cacheEntry))
            {
                cacheEntry = new UnloadedChunkData();
                _unloadedChunkCache[coords] = cacheEntry;
            }

            RenderTexture originalHeightmap = chunkToUnload.GetHeightMapData();
            RenderTexture cachedHeightmap = RenderTexture.GetTemporary(originalHeightmap.descriptor);
            Graphics.CopyTexture(originalHeightmap, cachedHeightmap);

            if (cacheEntry.Heightmap != null)
            {
                RenderTexture.ReleaseTemporary(cacheEntry.Heightmap);
            }

            cacheEntry.Heightmap = cachedHeightmap;
            cacheEntry.InitialParticlesDropped = chunkToUnload.InitialParticlesDropped;

            base.UnloadChunk(coords);
        }
    }


    protected override void SpawnChunkGameObject(UnityChunk chunk)
    {
        Vector3 position = new Vector3(chunk.Coordinates.X * chunkFactory.chunkSizeInWorldUnits, 0, chunk.Coordinates.Y * chunkFactory.chunkSizeInWorldUnits);
        GameObject terrainObj = Instantiate(terrainPrefab, position, Quaternion.identity, this.transform);
        terrainObj.name = $"Terrain Chunk ({chunk.Coordinates.X}, {chunk.Coordinates.Y})";

        Terrain terrain = terrainObj.GetComponent<Terrain>();
        TerrainCollider collider = terrainObj.GetComponent<TerrainCollider>();

        TerrainData clonedData = Instantiate(terrain.terrainData);
        terrain.terrainData = clonedData;
        collider.terrainData = clonedData;

        terrain.terrainData.heightmapResolution = (int) chunkFactory.heightmapResolution;
        terrain.terrainData.size = new Vector3(chunkFactory.chunkSizeInWorldUnits, terrain.terrainData.size.y, chunkFactory.chunkSizeInWorldUnits);

        _activeTerrainObjects.Add(chunk.Coordinates, terrain);
        chunk.ApplyToTerrain(terrain);
    }

    protected override void DespawnChunkGameObject(UnityChunk chunk)
    {
        if (_activeTerrainObjects.TryGetValue(chunk.Coordinates, out Terrain terrainToDestroy))
        {
            chunk.ReleaseResources();

            Destroy(terrainToDestroy.terrainData);
            Destroy(terrainToDestroy.gameObject);
            _activeTerrainObjects.Remove(chunk.Coordinates);
        }
    }

    protected override void ProcessQueues()
    {
        ProcessDirtyChunks();
    }

    public override void ForceFullRegeneration()
    {
        if (_terrainGenerator == null)
        {
            Debug.LogError("Terrain Generator is not initialized. Cannot regenerate.");
            return;
        }

        Debug.Log($"Starting regeneration for {_activeChunks.Count} active chunks...");

        foreach (var cachedData in _unloadedChunkCache.Values)
        {
            if (cachedData.Heightmap != null)
            {
                RenderTexture.ReleaseTemporary(cachedData.Heightmap);
            }
        }
        _unloadedChunkCache.Clear();


        var chunksToRegenerate = _activeChunks.Values.ToList();

        foreach (var chunk in chunksToRegenerate)
        {
            var context = new TerrainGenerationContext
            {
                Coords = chunk.Coordinates,
                Resolution = (int) chunkFactory.heightmapResolution,
                BorderSize = 0,
                WorldSeed = worldConfig.worldSeed
            };

            RenderTexture newHeightmap = _terrainGenerator.Generate(context);
            if (newHeightmap == null)
            {
                Debug.LogError($"Failed to regenerate heightmap for chunk at {chunk.Coordinates}");
                continue;
            }

            var oldHeightmap = chunk.GetHeightMapData();
            chunk.SetHeightMapData(newHeightmap);

            if (oldHeightmap != null)
            {
                RenderTexture.ReleaseTemporary(oldHeightmap);
            }


            chunk.InitialParticlesDropped = false;
            chunk.ClearIncomingParticles();

            MarkChunkAsDirty(chunk);
        }
    }

    #endregion

    #region IParticleWorldManager Implementation

    public void MarkChunkAsDirty(IChunk chunk)
    {
        if (chunk is UnityChunk typedChunk && !_chunksInQueue.Contains(typedChunk))
        {
            _dirtyChunksQueue.Enqueue(typedChunk);
            _chunksInQueue.Add(typedChunk);
        }
    }

    public void AddPendingParticles(GridCoordinates coords, List<Particle> particles)
    {
        var cacheEntry = GetOrCreateCacheEntry(coords);
        cacheEntry.PendingParticles.AddRange(particles);
    }

    #endregion

    #region System-Specific Logic

    private void ProcessDirtyChunks()
    {
        for (int i = 0; i < maxChunksToProcessPerFrame && _dirtyChunksQueue.Count > 0; i++)
        {
            UnityChunk chunkToProcess = _dirtyChunksQueue.Dequeue();
            _chunksInQueue.Remove(chunkToProcess);

            chunkToProcess.IsDirty = true;

            EnsureNeighborChunksExist(chunkToProcess.Coordinates);

            int borderSize = Mathf.Max(eroder.config.erosionBrushRadius, eroder.config.haloZoneWidth);

            RenderTexture haloMap = HaloMapUtility.BuildHaloMap(chunkToProcess, borderSize, _activeChunks, _unloadedChunkCache);

            if (haloMap == null)
            {
                chunkToProcess.IsDirty = false;
                continue;
            }

            _eroder.Erode(chunkToProcess, haloMap, worldConfig.worldSeed);

            List<IChunk> dirtiedNeighbors = HaloMapUtility.DeconstructHaloMap(haloMap, chunkToProcess, borderSize, _activeChunks, _unloadedChunkCache);

            RenderTexture.ReleaseTemporary(haloMap);

            ParticleTransferUtility.ProcessTransfers(chunkToProcess, _eroder, this);

            if (dirtiedNeighbors != null)
            {
                foreach (var neighbor in dirtiedNeighbors)
                {
                    MarkChunkAsDirty(neighbor);
                }
            }

            if (_activeTerrainObjects.TryGetValue(chunkToProcess.Coordinates, out Terrain terrainToUpdate))
            {
                chunkToProcess.ApplyToTerrain(terrainToUpdate);
            }

            chunkToProcess.IsDirty = false;
        }
    }


    private void EnsureNeighborChunksExist(GridCoordinates centerCoords)
    {
        foreach (NeighborDirection dir in System.Enum.GetValues(typeof(NeighborDirection)))
        {
            GridCoordinates neighborCoords = WorldCoordinateUtils.GetNeighborCoords(centerCoords, dir);

            if (!_activeChunks.ContainsKey(neighborCoords) && !_unloadedChunkCache.ContainsKey(neighborCoords))
            {
                GetOrCreateCacheEntry(neighborCoords);
            }
        }
    }

    private UnloadedChunkData GetOrCreateCacheEntry(GridCoordinates coords)
    {
        if (_unloadedChunkCache.TryGetValue(coords, out var cacheEntry))
        {
            return cacheEntry;
        }
        else
        {
            var newCacheEntry = new UnloadedChunkData();
            var context = new TerrainGenerationContext
            {
                Coords = coords,
                Resolution = (int) chunkFactory.heightmapResolution,
                BorderSize = 0,
                WorldSeed = worldConfig.worldSeed
            };
            newCacheEntry.Heightmap = _terrainGenerator.Generate(context);
            newCacheEntry.InitialParticlesDropped = false;

            _unloadedChunkCache[coords] = newCacheEntry;
            return newCacheEntry;
        }
    }

    #endregion
}

