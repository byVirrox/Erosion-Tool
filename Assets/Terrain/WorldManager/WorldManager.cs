using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class WorldManager : MonoBehaviour, IWorldManager
{
    [Header("World Settings")]
    [Tooltip("The transform to track as the center of the world (usually the player).")]
    public Transform player;
    [Tooltip("The number of chunks to load in each direction from the center.")]
    public int viewDistanceInChunks = 8;
    [Tooltip("The resolution of each chunk's heightmap (e.g., 129, 257).")]
    public int chunkResolution = 129;
    [Tooltip("Die Größe eines Chunks in Welt-Einheiten (Metern).")]
    public float chunkSizeInWorldUnits = 128f;
    [Header("World Settings")]
    public int worldSeed = 0;


    [Header("Services & Data")]
    [SerializeField] private UnityChunkFactory chunkFactory;
    [SerializeField] private ParticleTerrainEroder eroder;


    [Header("Visuals")]
    public GameObject terrainPrefab;

    private ITerrainParticleEroder _eroder;
    private IChunkFactory _chunkFactory;

    private Dictionary<GridCoordinates, IChunk> _activeChunks = new Dictionary<GridCoordinates, IChunk>();
    private Queue<IChunk> _dirtyChunksQueue = new Queue<IChunk>();
    private HashSet<IChunk> _chunksInQueue = new HashSet<IChunk>();
    private GridCoordinates _currentCenterCoords;

    private Dictionary<GridCoordinates, Terrain> _activeTerrainObjects = new Dictionary<GridCoordinates, Terrain>();
    private Dictionary<GridCoordinates, List<OutgoingParticle>> _pendingParticles = new Dictionary<GridCoordinates, List<OutgoingParticle>>();

    public IReadOnlyDictionary<GridCoordinates, IChunk> ActiveChunks => _activeChunks;

    #region Unity Lifecycle

    private void Awake()
    {
        _chunkFactory = chunkFactory;
        _eroder = eroder;
        _currentCenterCoords = new GridCoordinates(int.MaxValue, int.MaxValue);
    }

    private void Update()
    {
        if (player == null) return;

        UpdateViewPosition(player.position);
        ProcessDirtyChunks();
    }

    #endregion

    #region IWorldManager Implementation

    public void UpdateViewPosition(Vector3 newViewPosition)
    {
        GridCoordinates playerCoords = WorldPositionToGridCoordinates(newViewPosition);

        if (playerCoords.Equals(_currentCenterCoords))
        {
            return;
        }
        _currentCenterCoords = playerCoords;

        // chunks laden die gebraucht werden
        HashSet<GridCoordinates> requiredCoords = new HashSet<GridCoordinates>();
        for (int y = -viewDistanceInChunks; y <= viewDistanceInChunks; y++)
        {
            for (int x = -viewDistanceInChunks; x <= viewDistanceInChunks; x++)
            {
                requiredCoords.Add(new GridCoordinates(_currentCenterCoords.X + x, _currentCenterCoords.Y + y));
            }
        }

        // entladen von chunks
        List<GridCoordinates> toUnload = _activeChunks.Keys.Where(c => !requiredCoords.Contains(c)).ToList();
        foreach (var coords in toUnload)
        {
            UnloadChunk(coords);
        }

        // neue chunks laden
        foreach (var coords in requiredCoords)
        {
            if (!_activeChunks.ContainsKey(coords))
            {
                LoadChunk(coords);
            }
        }
    }

    public IChunk GetChunk(GridCoordinates coordinates)
    {
        _activeChunks.TryGetValue(coordinates, out IChunk chunk);
        return chunk;
    }

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
            INoiseGenerator generator = _chunkFactory.GetNoiseGenerator();
            if (generator == null) continue;

            RenderTexture newHeightmap = generator.GenerateTexture(chunk.Coordinates, this.chunkResolution, 0);

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

    public void MarkChunkAsDirty(IChunk chunk)
    {
        if (chunk == null) return;

        if (!_chunksInQueue.Contains(chunk))
        {
            _dirtyChunksQueue.Enqueue(chunk);
            _chunksInQueue.Add(chunk);
        }
    }

    public void AddPendingParticles(GridCoordinates coords, List<OutgoingParticle> particles)
    {
        if (!_pendingParticles.ContainsKey(coords))
        {
            _pendingParticles[coords] = new List<OutgoingParticle>();
        }
        _pendingParticles[coords].AddRange(particles);
    }


    //TODO möglicher Fehler
    public GridCoordinates GetNeighborCoords(GridCoordinates currentCoords, Direction dir)
    {
        return dir switch
        {
            Direction.North => new GridCoordinates(currentCoords.X, currentCoords.Y + 1),
            Direction.NorthEast => new GridCoordinates(currentCoords.X + 1, currentCoords.Y + 1),
            Direction.East => new GridCoordinates(currentCoords.X + 1, currentCoords.Y),
            Direction.SouthEast => new GridCoordinates(currentCoords.X + 1, currentCoords.Y - 1),
            Direction.South => new GridCoordinates(currentCoords.X, currentCoords.Y - 1),
            Direction.SouthWest => new GridCoordinates(currentCoords.X - 1, currentCoords.Y - 1),
            Direction.West => new GridCoordinates(currentCoords.X - 1, currentCoords.Y),
            Direction.NorthWest => new GridCoordinates(currentCoords.X - 1, currentCoords.Y + 1),
            _ => currentCoords
        };
    }


    #endregion

    #region Internal Logic

    private void LoadChunk(GridCoordinates coords)
    {
        var newChunk = _chunkFactory.CreateChunk(coords, chunkResolution);
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

        // Nachbarn verlinken
        foreach (Direction dir in System.Enum.GetValues(typeof(Direction)))
        {
            GridCoordinates neighborCoords = GetNeighborCoords(coords, dir);
            if (_activeChunks.TryGetValue(neighborCoords, out IChunk neighbor))
            {
                newChunk.SetNeighbor(dir, neighbor);
                neighbor.SetNeighbor(GetOppositeDirection(dir), newChunk);
            }
        }
        UploadPendingParticles(coords, newChunk);

        MarkChunkAsDirty(newChunk);
    }

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

            foreach (Direction dir in System.Enum.GetValues(typeof(Direction)))
            {
                if (chunkToUnload.GetNeighbor(dir) is IChunk neighbor)
                {
                    neighbor.SetNeighbor(GetOppositeDirection(dir), null);
                }
            }

            _activeChunks.Remove(coords);
        }
    }

    private void UploadPendingParticles(GridCoordinates coords, IChunk chunk)
    {
        if (_pendingParticles.TryGetValue(coords, out List<OutgoingParticle> particles))
        {
            (chunk as UnityChunk)?.AppendFromCPU(particles, eroder.GetTransferShader());
            _pendingParticles.Remove(coords);
        }
    }

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

            var erosionResult = _eroder.Erode(erodibleChunk, _chunkFactory.GetNoiseGenerator(), worldSeed);

            ParticleTransferService.ProcessTransfers(erodibleChunk, _eroder, this, erosionResult.OutgoingParticleCounts);

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

    // Hilfsmethoden, um Koordinaten zu berechnen etc.
    private GridCoordinates WorldPositionToGridCoordinates(Vector3 position)
    {
        int x = Mathf.FloorToInt(position.x / chunkSizeInWorldUnits);
        int y = Mathf.FloorToInt(position.z / chunkSizeInWorldUnits);
        return new GridCoordinates(x, y);
    }

    private Direction GetOppositeDirection(Direction dir)
    {
        return dir switch
        {
            Direction.North => Direction.South,
            Direction.NorthEast => Direction.SouthWest,
            Direction.East => Direction.West,
            Direction.SouthEast => Direction.NorthWest,
            Direction.South => Direction.North,
            Direction.SouthWest => Direction.NorthEast,
            Direction.West => Direction.East,
            Direction.NorthWest => Direction.SouthEast,
            _ => dir 
        };
    }

    #endregion
}
