using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class WorldManager : AbstractWorldManager<ErosionParticleChunk, ErosionParticleChunkFactory>, IParticleWorldManager
{
    public class UnloadedChunkData
    {
        public RenderTexture Heightmap;
        public List<Particle> PendingParticles = new List<Particle>();
        public bool InitialParticlesDropped;
    }

    [Header("System-Specific Dependencies")]
    [SerializeField] private ParticleEroder eroder;

    [Header("Performance Settings")]
    [Tooltip("The maximum Number of Chunks that should be eroded every Frame")]
    [SerializeField] private int maxChunksToProcessPerFrame = 5;

    [Header("Visuals")]
    [SerializeField] private GameObject terrainPrefab;

    [Header("WorldRegeneration")]
    [Tooltip("Check to allow Realtimechanges by adjusting parameters")]
    [SerializeField] private bool checkWorldRegeneration = false;

    private const float RegenerationDelay = 0.5f;
    private float _timeUntilRegeneration = -1f;

    private IParticleEroder _eroder;

    private readonly Dictionary<GridCoordinates, Terrain> _activeTerrainObjects = new Dictionary<GridCoordinates, Terrain>();

    private readonly Dictionary<GridCoordinates, UnloadedChunkData> _unloadedChunkCache = new Dictionary<GridCoordinates, UnloadedChunkData>();

    private readonly Queue<ErosionParticleChunk> _dirtyChunksQueue = new Queue<ErosionParticleChunk>();
    private readonly HashSet<ErosionParticleChunk> _chunksInQueue = new HashSet<ErosionParticleChunk>();
    private ITerrainGenerator _terrainGenerator;


    protected override void Update()
    {
        base.Update();
        if (_timeUntilRegeneration > 0)
        {
            _timeUntilRegeneration -= Time.deltaTime;
            if (_timeUntilRegeneration <= 0 && checkWorldRegeneration)
            {
                ForceFullRegeneration();
            }
        }
    }

    public void TriggerRegenerationDelayed()
    {
        _timeUntilRegeneration = RegenerationDelay;
        Debug.Log("Regeneration queued...");
    }

    #region Overridden Base Methods

    protected override void InitializeServices()
    {
        _eroder = eroder;
        _terrainGenerator = chunkFactory.GetGenerator();
    }

    protected override void LoadChunk(GridCoordinates coords)
    {
        ErosionParticleChunk newChunk;

        if (_unloadedChunkCache.TryGetValue(coords, out var cachedData))
        {
            newChunk = new ErosionParticleChunk(coords, cachedData.Heightmap);
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
            if (_activeChunks.TryGetValue(neighborCoords, out ErosionParticleChunk neighbor))
            {
                newChunk.SetNeighbor(dir, neighbor);
                neighbor.SetNeighbor(WorldCoordinateUtils.GetOppositeDirection(dir), newChunk);
            }
        }

        MarkChunkAsDirty(newChunk);
    }

    protected override void UnloadChunk(GridCoordinates coords)
    {
        if (_activeChunks.TryGetValue(coords, out ErosionParticleChunk chunkToUnload))
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


    protected override void SpawnChunkGameObject(ErosionParticleChunk chunk)
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

    protected override void DespawnChunkGameObject(ErosionParticleChunk chunk)
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
            cachedData.PendingParticles.Clear();
        }
        _unloadedChunkCache.Clear();


        var chunksToRegenerate = _activeChunks.Values.ToList();

        foreach (var chunk in chunksToRegenerate)
        {
            var context = new TerrainGenerationContext
            {
                Coords = chunk.Coordinates,
                Resolution = (int)chunkFactory.heightmapResolution,
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

            if (_activeTerrainObjects.TryGetValue(chunk.Coordinates, out Terrain terrainToUpdate))
            {
                chunk.ApplyToTerrain(terrainToUpdate);
            }

            MarkChunkAsDirty(chunk);
        }
        Debug.Log("Regeneration complete. Chunks marked for re-erosion.");
    }

    #endregion

    #region IParticleWorldManager Implementation

    public void MarkChunkAsDirty(IChunk chunk)
    {
        if (chunk is ErosionParticleChunk typedChunk && !_chunksInQueue.Contains(typedChunk))
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
            ErosionParticleChunk chunkToProcess = _dirtyChunksQueue.Dequeue();
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

                SetTerrainNeighbors(chunkToProcess, terrainToUpdate);
            }

            chunkToProcess.IsDirty = false;
        }
    }

    private void SetTerrainNeighbors(ErosionParticleChunk centerChunk, Terrain centerTerrain)
    {
        Terrain GetActiveTerrainObject(IChunk chunk)
        {
            if (chunk != null && _activeTerrainObjects.TryGetValue(chunk.Coordinates, out Terrain terrain))
            {
                return terrain;
            }
            return null;
        }

        Terrain neighborWest = GetActiveTerrainObject(centerChunk.GetNeighbor(NeighborDirection.West));
        Terrain neighborSouth = GetActiveTerrainObject(centerChunk.GetNeighbor(NeighborDirection.South));
        Terrain neighborEast = GetActiveTerrainObject(centerChunk.GetNeighbor(NeighborDirection.East));
        Terrain neighborNorth = GetActiveTerrainObject(centerChunk.GetNeighbor(NeighborDirection.North));

        centerTerrain.SetNeighbors(
            left: neighborWest,
            top: neighborNorth,
            right: neighborEast,
            bottom: neighborSouth
        );

        if (neighborWest != null) neighborWest.SetNeighbors(null, null, centerTerrain, null);
        if (neighborEast != null) neighborEast.SetNeighbors(centerTerrain, null, null, null);
        if (neighborSouth != null) neighborSouth.SetNeighbors(null, centerTerrain, null, null);
        if (neighborNorth != null) neighborNorth.SetNeighbors(null, null, null, centerTerrain);
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

