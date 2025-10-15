using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class WorldManager : AbstractWorldManager<UnityChunk, UnityChunkFactory>, IParticleWorldManager
{
    [Header("System-Specific Dependencies")]
    [SerializeField] private ParticleTerrainEroder eroder;

    [Header("Visuals")]
    [SerializeField] private GameObject terrainPrefab;

    private ITerrainParticleEroder _eroder;

    private readonly Dictionary<GridCoordinates, Terrain> _activeTerrainObjects = new Dictionary<GridCoordinates, Terrain>();
    private readonly Dictionary<GridCoordinates, List<Particle>> _pendingParticles = new Dictionary<GridCoordinates, List<Particle>>();
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

        base.LoadChunk(coords);

        if (_activeChunks.TryGetValue(coords, out UnityChunk newChunk))
        {
            UploadPendingParticles(coords, newChunk);
            MarkChunkAsDirty(newChunk);
        }
    }

    protected override void SpawnChunkGameObject(UnityChunk chunk)
    {
        Vector3 position = new Vector3(chunk.Coordinates.X * chunkSizeInWorldUnits, 0, chunk.Coordinates.Y * chunkSizeInWorldUnits);
        GameObject terrainObj = Instantiate(terrainPrefab, position, Quaternion.identity, this.transform);
        terrainObj.name = $"Terrain Chunk ({chunk.Coordinates.X}, {chunk.Coordinates.Y})";

        Terrain terrain = terrainObj.GetComponent<Terrain>();
        TerrainCollider collider = terrainObj.GetComponent<TerrainCollider>();

        TerrainData clonedData = Instantiate(terrain.terrainData);
        terrain.terrainData = clonedData;
        collider.terrainData = clonedData;

        terrain.terrainData.heightmapResolution = chunkResolution;
        terrain.terrainData.size = new Vector3(chunkSizeInWorldUnits, terrain.terrainData.size.y, chunkSizeInWorldUnits);

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

    /**
    public override void ForceFullRegeneration()
    {
        Debug.Log($"Starting regeneration for {_activeChunks.Count} active chunks...");
        foreach (var chunk in _activeChunks.Values)
        {
            var regeneratedChunk = chunkFactory.CreateChunk(chunk.Coordinates, chunkResolution, worldSeed);
            RenderTexture newHeightmap = regeneratedChunk.GetHeightMapData();

            var oldHeightmap = chunk.GetHeightMapData();
            chunk.SetHeightMapData(newHeightmap);
            if (oldHeightmap != null) { RenderTexture.ReleaseTemporary(oldHeightmap); }

            chunk.InitialParticlesDropped = false;
            chunk.ClearIncomingParticles();

            MarkChunkAsDirty(chunk);
        }
    }
    **/

    public override void ForceFullRegeneration()
    {
        if (_terrainGenerator == null)
        {
            Debug.LogError("Terrain Generator is not initialized. Cannot regenerate.");
            return;
        }

        Debug.Log($"Starting regeneration for {_activeChunks.Count} active chunks...");

        var chunksToRegenerate = _activeChunks.Values.ToList();

        foreach (var chunk in chunksToRegenerate)
        {
            var context = new TerrainGenerationContext
            {
                Coords = chunk.Coordinates,
                Resolution = chunkResolution,
                BorderSize = 0,
                WorldSeed = worldSeed
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
        if (!_pendingParticles.ContainsKey(coords))
        {
            _pendingParticles[coords] = new List<Particle>();
        }
        _pendingParticles[coords].AddRange(particles);
    }


    #endregion

    #region System-Specific Logic

    private void ProcessDirtyChunks()
    {
        if (_dirtyChunksQueue.Count > 0)
        {
            UnityChunk chunkToProcess = _dirtyChunksQueue.Dequeue();

            int borderSize = eroder.config.erosionBrushRadius;

            var haloContext = new TerrainGenerationContext
            {
                Coords = chunkToProcess.Coordinates,
                Resolution = this.chunkResolution,
                BorderSize = borderSize,
                WorldSeed = this.worldSeed
            };

            RenderTexture haloMap = HaloMapUtility.BuildHaloMap(chunkToProcess, borderSize, _terrainGenerator, haloContext);

            var erosionResult = _eroder.Erode(chunkToProcess, haloMap, worldSeed);


            RenderTexture.ReleaseTemporary(haloMap);

            ParticleTransferService.ProcessTransfers(chunkToProcess, _eroder, this);

            if (erosionResult.DirtiedNeighbors != null)
            {
                foreach (var neighbor in erosionResult.DirtiedNeighbors)
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


    private void UploadPendingParticles(GridCoordinates coords, IChunk chunk)
    {
        if (_pendingParticles.TryGetValue(coords, out List<Particle> particles))
        {
            (chunk as IParticleErodibleChunk)?.AppendFromCPU(particles, eroder.GetTransferShader());
            _pendingParticles.Remove(coords);
        }
    }


    private void LogOutgoingCounts(GridCoordinates sourceCoords, Dictionary<int, int> counts)
    {
        bool hasOutgoing = false;
        foreach (var count in counts.Values) { if (count > 0) hasOutgoing = true; }

        if (hasOutgoing)
        {
            Debug.Log($"Chunk {sourceCoords} hat Partikel exportiert: " +
                      $"N({counts[0]}), E({counts[1]}), S({counts[2]}), W({counts[3]}), Corners({counts[4]})");
        }
    }



    #endregion

    /**
public void ForceFullRegeneration()
{
    if (_chunkFactory == null)
    {
        Debug.LogError("ChunkFactory ist nicht zugewiesen. Regeneration nicht möglich.");
        return;
    }

    var allActiveChunks = _activeChunks.Values.ToList();

    Debug.Log($"Starte Regeneration für {allActiveChunks.Count} aktive Chunks...");

    foreach (var chunk in allActiveChunks)
    {
        var regeneratedChunk = _chunkFactory.CreateChunk(chunk.Coordinates, chunkResolution, terrainGenerationGraph);
        RenderTexture newHeightmap = (regeneratedChunk as IChunk<RenderTexture>).GetHeightMapData();

        (chunk as IChunk<RenderTexture>)?.GetHeightMapData()?.Release();
        (chunk as IChunk<RenderTexture>)?.SetHeightMapData(newHeightmap);

        if (chunk is IParticleErodibleChunk erodibleChunk)
        {
            erodibleChunk.InitialParticlesDropped = false;
            erodibleChunk.ClearIncomingParticles();
        }

        MarkChunkAsDirty(chunk);
    }
}
**/


    /**
public void MarkChunkAsDirty(IChunk chunk)
{
    if (chunk == null) return;

    if (!_chunksInQueue.Contains(chunk))
    {
        _dirtyChunksQueue.Enqueue(chunk);
        _chunksInQueue.Add(chunk);
    }
}
**/

    /**
private void ProcessDirtyChunks()
{
    if (_dirtyChunksQueue.Count > 0)
    {
        IChunk chunkToProcess = _dirtyChunksQueue.Dequeue();
        _chunksInQueue.Remove(chunkToProcess);

        chunkToProcess.IsDirty = true;

        if (chunkToProcess is not IParticleErodibleChunk erodibleChunk)
        {
            chunkToProcess.IsDirty = false;
            return;
        }

        var erosionResult = _eroder.Erode(erodibleChunk, _graphGenerator, terrainGenerationGraph, worldSeed);

        ParticleTransferService.ProcessTransfers(erodibleChunk, _eroder, this);

        if (erosionResult.DirtiedNeighbors != null)
        {
            foreach (var neighbor in erosionResult.DirtiedNeighbors)
            {
                MarkChunkAsDirty(neighbor);
            }
        }

        if (_activeTerrainObjects.TryGetValue(erodibleChunk.Coordinates, out Terrain terrainToUpdate))
        {
            (erodibleChunk as UnityChunk)?.ApplyToTerrain(terrainToUpdate);
        }
        erodibleChunk.IsDirty = false;
    }
}
**/

    /**
private void LoadChunk(GridCoordinates coords)
{
    var newChunk = _chunkFactory.CreateChunk(coords, chunkResolution, terrainGenerationGraph);

    if (newChunk == null) return;

    _activeChunks.Add(coords, newChunk);

    Vector3 position = new Vector3(coords.X * chunkSizeInWorldUnits, 0, coords.Y * chunkSizeInWorldUnits);
    GameObject terrainObj = Instantiate(terrainPrefab, position, Quaternion.identity, this.transform);
    terrainObj.name = $"Terrain Chunk ({coords.X}, {coords.Y})";

    Terrain terrain = terrainObj.GetComponent<Terrain>();
    TerrainCollider collider = terrainObj.GetComponent<TerrainCollider>(); 

    TerrainData clonedData = Instantiate(terrain.terrainData);

    terrain.terrainData = clonedData;
    collider.terrainData = clonedData;

    terrain.terrainData.heightmapResolution = chunkResolution;
    terrain.terrainData.size = new Vector3(chunkSizeInWorldUnits, terrain.terrainData.size.y, chunkSizeInWorldUnits);

    _activeTerrainObjects.Add(coords, terrain);

    (newChunk as UnityChunk)?.ApplyToTerrain(terrain);

    foreach (NeighborDirection dir in System.Enum.GetValues(typeof(NeighborDirection)))
    {
        GridCoordinates neighborCoords = WorldCoordinateUtils.GetNeighborCoords(coords, dir);
        if (_activeChunks.TryGetValue(neighborCoords, out IChunk neighbor))
        {
            newChunk.SetNeighbor(dir, neighbor);
            neighbor.SetNeighbor(WorldCoordinateUtils.GetOppositeDirection(dir), newChunk);
        }
    }
    UploadPendingParticles(coords, newChunk);

    MarkChunkAsDirty(newChunk);
}
**/

    /**
    private void UnloadChunk(GridCoordinates coords)
    {
        if (_activeChunks.TryGetValue(coords, out IChunk chunkToUnload))
        {
            (chunkToUnload as UnityChunk)?.ReleaseResources();

            if (_activeTerrainObjects.TryGetValue(coords, out Terrain terrainToDestroy))
            {
                Destroy(terrainToDestroy.terrainData);
                Destroy(terrainToDestroy.gameObject);
                _activeTerrainObjects.Remove(coords);
            }

            foreach (NeighborDirection dir in System.Enum.GetValues(typeof(NeighborDirection)))
            {
                if (chunkToUnload.GetNeighbor(dir) is IChunk neighbor)
                {
                    neighbor.SetNeighbor(WorldCoordinateUtils.GetOppositeDirection(dir), null);
                }
            }

            _activeChunks.Remove(coords);
        }
    }
    **/
}
