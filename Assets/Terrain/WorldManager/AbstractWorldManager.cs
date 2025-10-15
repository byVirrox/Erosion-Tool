using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public abstract class AbstractWorldManager<TChunk, TFactory> : MonoBehaviour, IWorldManager
    where TChunk : class, IChunk
    where TFactory : ScriptableObject, IChunkFactory<TChunk>
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
    [SerializeField] protected TFactory chunkFactory; 

    protected Dictionary<GridCoordinates, TChunk> _activeChunks = new Dictionary<GridCoordinates, TChunk>();
    private GridCoordinates _currentCenterCoords;

    #region Unity Lifecycle

    protected virtual void Awake()
    {
        _currentCenterCoords = new GridCoordinates(int.MaxValue, int.MaxValue);
        InitializeServices();
    }

    protected virtual void Update()
    {
        if (player == null) return;
        UpdateViewPosition(player.position);
        ProcessQueues();
    }

    #endregion

    #region Abstract & Virtual Methods

    /// <summary>
    /// Initialisiert alle systemspezifischen Services (z.B. GraphGenerator, Eroder).
    /// Wird in Awake aufgerufen.
    /// </summary>
    protected abstract void InitializeServices();

    /// <summary>
    /// Enthält die spezifische Logik zum Instanziieren des Chunk-GameObjects.
    /// </summary>
    protected abstract void SpawnChunkGameObject(TChunk chunk);

    /// <summary>
    /// Enthält die spezifische Logik zum Zerstören des Chunk-GameObjects.
    /// </summary>
    protected abstract void DespawnChunkGameObject(TChunk chunk);

    /// <summary>
    /// Verarbeitet systemspezifische Queues (z.B. für Erosion).
    /// </summary>
    protected abstract void ProcessQueues();

    public abstract void ForceFullRegeneration();

    #endregion

    #region Core Logic (now in base class)

    protected virtual void LoadChunk(GridCoordinates coords)
    {
        TChunk newChunk = chunkFactory.CreateChunk(coords, chunkResolution, worldSeed);
        if (newChunk == null) return;

        _activeChunks.Add(coords, newChunk);

        SpawnChunkGameObject(newChunk);

        foreach (NeighborDirection dir in System.Enum.GetValues(typeof(NeighborDirection)))
        {
            GridCoordinates neighborCoords = WorldCoordinateUtils.GetNeighborCoords(coords, dir);
            if (_activeChunks.TryGetValue(neighborCoords, out TChunk neighbor))
            {
                newChunk.SetNeighbor(dir, neighbor);
                neighbor.SetNeighbor(WorldCoordinateUtils.GetOppositeDirection(dir), newChunk);
            }
        }
    }

    protected virtual void UnloadChunk(GridCoordinates coords)
    {
        if (_activeChunks.TryGetValue(coords, out TChunk chunkToUnload))
        {
            DespawnChunkGameObject(chunkToUnload);

            foreach (NeighborDirection dir in System.Enum.GetValues(typeof(NeighborDirection)))
            {
                if (chunkToUnload.GetNeighbor(dir) is TChunk neighbor)
                {
                    neighbor.SetNeighbor(WorldCoordinateUtils.GetOppositeDirection(dir), null);
                }
            }

            _activeChunks.Remove(coords);
        }
    }

    #endregion

    #region IWorldManager Implementation

    public IReadOnlyDictionary<GridCoordinates, IChunk> ActiveChunks => _activeChunks.ToDictionary(kvp => kvp.Key, kvp => (IChunk)kvp.Value);

    public IChunk GetChunk(GridCoordinates coordinates)
    {
        _activeChunks.TryGetValue(coordinates, out TChunk chunk);
        return chunk;
    }


    public void UpdateViewPosition(Vector3 newViewPosition)
    {
        GridCoordinates playerCoords = WorldCoordinateUtils.WorldPositionToGridCoordinates(newViewPosition, chunkSizeInWorldUnits);
        if (playerCoords.Equals(_currentCenterCoords))
        {
            return;
        }

        _currentCenterCoords = playerCoords;

        HashSet<GridCoordinates> requiredCoords = new HashSet<GridCoordinates>();
        for (int y = -viewDistanceInChunks; y <= viewDistanceInChunks; y++)
        {
            for (int x = -viewDistanceInChunks; x <= viewDistanceInChunks; x++)
            {
                requiredCoords.Add(new GridCoordinates(_currentCenterCoords.X + x, _currentCenterCoords.Y + y));
            }
        }

        List<GridCoordinates> toUnload = _activeChunks.Keys.Where(c => !requiredCoords.Contains(c)).ToList();
        foreach (var coords in toUnload)
        {
            UnloadChunk(coords);
        }

        foreach (var coords in requiredCoords)
        {
            if (!_activeChunks.ContainsKey(coords))
            {
                LoadChunk(coords);
            }
        }
    }


    #endregion
}