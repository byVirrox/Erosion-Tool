using System.Collections.Generic;
using System.Diagnostics; 
using System.Globalization;
using System.IO; 
using System.Linq;
using UnityEngine;
using UnityEngine.Profiling; 
using Debug = UnityEngine.Debug;

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
    [Tooltip("Terrain Prefab")]
    [SerializeField] private GameObject terrainPrefab;
    [Tooltip("Check to try to connect Neighbors during runtime")]
    [SerializeField] private bool connectNeighbors = false;

    [Header("WorldRegeneration")]
    [Tooltip("Check to allow Realtimechanges by adjusting parameters")]
    [SerializeField] private bool checkWorldRegeneration = false;

    [Header("Performance Recording")]
    [Tooltip("Enable detailed Profiler Markers around key functions.")]
    [SerializeField] private bool enableProfilerMarkers = true;

    [Tooltip("Enable programmatic logging of FPS and CPU times to a CSV file.")]
    [SerializeField] private bool enableDataLogging = false;

    [Tooltip("How often (in seconds) to record performance data.")]
    [SerializeField] private float recordingInterval = 1.0f;

    [Tooltip("Filename for the performance log CSV (saved in Persistent Data Path).")]
    [SerializeField] private string logFilename = "performance_log.csv";

    [Tooltip("Check to log average FPS to the console every second.")]
    [SerializeField] private bool logFPS = false;

    private const float RegenerationDelay = 0.5f;
    private float _timeUntilRegeneration = -1f;

    private IParticleEroder _eroder;

    private readonly Dictionary<GridCoordinates, Terrain> _activeTerrainObjects = new Dictionary<GridCoordinates, Terrain>();

    private readonly Dictionary<GridCoordinates, UnloadedChunkData> _unloadedChunkCache = new Dictionary<GridCoordinates, UnloadedChunkData>();

    private readonly Queue<ErosionParticleChunk> _dirtyChunksQueue = new Queue<ErosionParticleChunk>();
    private readonly HashSet<ErosionParticleChunk> _chunksInQueue = new HashSet<ErosionParticleChunk>();


    #region Performance Recording Variables
    // For FPS calculation over the interval
    private float _intervalTimer;
    private int _frameCountInInterval;

    // For CPU time measurement
    private readonly Stopwatch _stopwatch = new Stopwatch();
    private float _maxProcessDirtyChunksMsInInterval = 0f;
    private float _maxApplyToTerrainMsInInterval = 0f; 

    // Lists to store recorded data
    private readonly List<float> _recordedTime = new List<float>();
    private readonly List<float> _recordedAvgFps = new List<float>();
    private readonly List<float> _recordedMaxCpuDirtyChunks = new List<float>();
    private readonly List<float> _recordedMaxCpuApplyTerrain = new List<float>(); // Optional
    private float _totalTimeElapsed = 0f;
    #endregion

    #region Old Performance Recording Variables
    // For FPS calculation over the interval
    private float pollingTime = 1f;
    private float timeBetween;
    private int frameCount;
    #endregion


    protected override void Update()
    {
        if (enableProfilerMarkers) Profiler.BeginSample("WorldManager.Update (Base)");
        base.Update(); 
        if (enableProfilerMarkers) Profiler.EndSample();

        // 2. Regenerierungs-Logik (NUR EINMAL!)
        if (_timeUntilRegeneration > 0)
        {
            _timeUntilRegeneration -= Time.deltaTime;
            if (_timeUntilRegeneration <= 0 && checkWorldRegeneration)
            {
                if (enableProfilerMarkers) Profiler.BeginSample("WorldManager.ForceFullRegeneration (Triggered)");
                ForceFullRegeneration();
                if (enableProfilerMarkers) Profiler.EndSample();
            }
        }

        // 3. Performance Logging Logik (wie zuvor)
        if (enableDataLogging)
        {
            _totalTimeElapsed += Time.unscaledDeltaTime;
            _intervalTimer += Time.unscaledDeltaTime;
            _frameCountInInterval++;

            if (_intervalTimer >= recordingInterval)
            {
                float avgFps = _frameCountInInterval / _intervalTimer;
                _recordedTime.Add(_totalTimeElapsed);
                _recordedAvgFps.Add(avgFps);
                _recordedMaxCpuDirtyChunks.Add(_maxProcessDirtyChunksMsInInterval);
                _recordedMaxCpuApplyTerrain.Add(_maxApplyToTerrainMsInInterval);

                // Reset für nächstes Intervall
                _intervalTimer %= recordingInterval;
                _frameCountInInterval = 0;
                _maxProcessDirtyChunksMsInInterval = 0f;
                _maxApplyToTerrainMsInInterval = 0f;
            }
        }

        if (logFPS && !enableDataLogging) 
        {
             timeBetween += UnityEngine.Time.deltaTime; 
             frameCount++;
             if (timeBetween >= pollingTime)
             {
                 int frameRate = Mathf.RoundToInt(frameCount / timeBetween);
                 Debug.Log($"FPS: {frameRate}");
                 timeBetween -= pollingTime;
                 frameCount = 0;
             }
        }

        
    }

    public void TriggerRegenerationDelayed()
    {
        _timeUntilRegeneration = RegenerationDelay;
        Debug.Log("Regeneration queued...");
    }

    private void OnApplicationQuit()
    {
        if (enableDataLogging && _recordedTime.Count > 0)
        {
            SaveRecordedData();
        }
    }

    // Public method to manually trigger saving (e.g., via a button)
    public void SavePerformanceData()
    {
        if (enableDataLogging && _recordedTime.Count > 0)
        {
            SaveRecordedData();
            Debug.Log($"Performance data saved to {GetLogFilePath()}");
        }
        else
        {
            Debug.LogWarning("Data logging was not enabled or no data recorded.");
        }
    }

    #region Overridden Base Methods

    protected override void InitializeServices()
    {
        _eroder = eroder;
    }


    protected override void LoadChunk(GridCoordinates coords)
    {
        if (enableProfilerMarkers) Profiler.BeginSample("WorldManager.LoadChunk");

        ErosionParticleChunk newChunk;

        if (_unloadedChunkCache.TryGetValue(coords, out var cachedData))
        {
            if (enableProfilerMarkers) Profiler.BeginSample("LoadChunk - CreateFromData");
            newChunk = chunkFactory.CreateChunkFromData(coords, cachedData.Heightmap);
            if (enableProfilerMarkers) Profiler.EndSample();

            newChunk.InitialParticlesDropped = cachedData.InitialParticlesDropped;

            if (cachedData.PendingParticles.Count > 0)
            {
                if (enableProfilerMarkers) Profiler.BeginSample("LoadChunk - AppendFromCPU");
                newChunk.AppendFromCPU(cachedData.PendingParticles);
                if (enableProfilerMarkers) Profiler.EndSample();
            }
            _unloadedChunkCache.Remove(coords);
        }
        else
        {
            if (enableProfilerMarkers) Profiler.BeginSample("LoadChunk - CreateChunk (Generate)");
            newChunk = chunkFactory.CreateChunk(coords, worldConfig.worldSeed);
            if (enableProfilerMarkers) Profiler.EndSample();
        }

        if (newChunk == null)
        {
            if (enableProfilerMarkers) Profiler.EndSample(); 
            return;
        }

        _activeChunks.Add(coords, newChunk);

        if (enableProfilerMarkers) Profiler.BeginSample("LoadChunk - SpawnGameObject");
        SpawnChunkGameObject(newChunk); 
        if (enableProfilerMarkers) Profiler.EndSample();

        if (enableProfilerMarkers) Profiler.BeginSample("LoadChunk - Set Neighbors");
        foreach (NeighborDirection dir in System.Enum.GetValues(typeof(NeighborDirection)))
        {
            GridCoordinates neighborCoords = WorldCoordinateUtils.GetNeighborCoords(coords, dir);
            if (_activeChunks.TryGetValue(neighborCoords, out ErosionParticleChunk neighbor))
            {
                newChunk.SetNeighbor(dir, neighbor);
                neighbor.SetNeighbor(WorldCoordinateUtils.GetOppositeDirection(dir), newChunk);
            }
        }
        if (enableProfilerMarkers) Profiler.EndSample();

        MarkChunkAsDirty(newChunk);

        if (enableProfilerMarkers) Profiler.EndSample(); 
    }


    protected override void UnloadChunk(GridCoordinates coords)
    {
        if (enableProfilerMarkers) Profiler.BeginSample("WorldManager.UnloadChunk");

        if (_activeChunks.TryGetValue(coords, out ErosionParticleChunk chunkToUnload))
        {
            if (enableProfilerMarkers) Profiler.BeginSample("UnloadChunk - Caching Logic");
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
            if (enableProfilerMarkers) Profiler.EndSample();

            if (enableProfilerMarkers) Profiler.BeginSample("UnloadChunk - Base Unload");
            base.UnloadChunk(coords); 
            if (enableProfilerMarkers) Profiler.EndSample();
        }

        if (enableProfilerMarkers) Profiler.EndSample(); 
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

        terrain.terrainData.heightmapResolution = (int)chunkFactory.heightmapResolution;
        terrain.terrainData.size = new Vector3(chunkFactory.chunkSizeInWorldUnits, terrain.terrainData.size.y, chunkFactory.chunkSizeInWorldUnits);

        _activeTerrainObjects.Add(chunk.Coordinates, terrain);

        // Optional: Measure ApplyToTerrain specifically if it's a bottleneck
        if (enableProfilerMarkers) Profiler.BeginSample("SpawnChunk - ApplyToTerrain");
        if (enableDataLogging) _stopwatch.Restart();

        chunk.ApplyToTerrain(terrain); // The GPU->CPU readback happens here

        if (enableDataLogging)
        {
            _stopwatch.Stop();
            _maxApplyToTerrainMsInInterval = Mathf.Max(_maxApplyToTerrainMsInInterval, (float)_stopwatch.Elapsed.TotalMilliseconds);
        }
        if (enableProfilerMarkers) Profiler.EndSample();
    }

    protected override void DespawnChunkGameObject(ErosionParticleChunk chunk)
    {
        if (enableProfilerMarkers) Profiler.BeginSample("WorldManager.DespawnChunkGameObject");
        if (_activeTerrainObjects.TryGetValue(chunk.Coordinates, out Terrain terrainToDestroy))
        {
            chunk.ReleaseResources();

            Destroy(terrainToDestroy.terrainData);
            Destroy(terrainToDestroy.gameObject);
            _activeTerrainObjects.Remove(chunk.Coordinates);
        }
        if (enableProfilerMarkers) Profiler.EndSample();
    }

    protected override void ProcessQueues()
    {
        // ProcessDirtyChunks is called within base.Update(), measure it there
        if (enableProfilerMarkers) Profiler.BeginSample("WorldManager.ProcessDirtyChunks");
        if (enableDataLogging) _stopwatch.Restart();

        ProcessDirtyChunks(); // Your core erosion loop

        if (enableDataLogging)
        {
            _stopwatch.Stop();
            // Track the MAX time spent in this method during the current interval
            _maxProcessDirtyChunksMsInInterval = Mathf.Max(_maxProcessDirtyChunksMsInInterval, (float)_stopwatch.Elapsed.TotalMilliseconds);
        }
        if (enableProfilerMarkers) Profiler.EndSample();
    }

    public override void ForceFullRegeneration()
    {
        // We already measure the trigger in Update, let's measure the content here
        if (enableProfilerMarkers) Profiler.BeginSample("ForceFullRegeneration - Content");

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
            if (enableProfilerMarkers) Profiler.BeginSample("Regen - GenerateBaseHeightmap");
            RenderTexture newHeightmap = chunkFactory.GenerateBaseHeightmap(chunk.Coordinates, worldConfig.worldSeed);
            if (enableProfilerMarkers) Profiler.EndSample();
            if (newHeightmap == null)
            {
                UnityEngine.Debug.LogError($"Failed to regenerate heightmap for chunk at {chunk.Coordinates}");
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
                if (enableProfilerMarkers) Profiler.BeginSample("Regen - ApplyToTerrain");
                chunk.ApplyToTerrain(terrainToUpdate);
                if (enableProfilerMarkers) Profiler.EndSample();
            }
            MarkChunkAsDirty(chunk);
        }
        Debug.Log("Regeneration complete. Chunks marked for re-erosion.");

        if (enableProfilerMarkers) Profiler.EndSample(); // End Content sample
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
            if (enableProfilerMarkers) Profiler.BeginSample("ProcessDirtyChunks - Single Chunk Loop");

            ErosionParticleChunk chunkToProcess = _dirtyChunksQueue.Dequeue();
            _chunksInQueue.Remove(chunkToProcess);

            chunkToProcess.IsDirty = true;

            EnsureNeighborChunksExist(chunkToProcess.Coordinates);

            int borderSize = Mathf.Max(eroder.config.erosionBrushRadius, eroder.config.haloZoneWidth);

            if (enableProfilerMarkers) Profiler.BeginSample("ProcessDirtyChunks - BuildHaloMap");
            RenderTexture haloMap = HaloMapUtility.BuildHaloMap(chunkToProcess, borderSize, _activeChunks, _unloadedChunkCache);
            if (enableProfilerMarkers) Profiler.EndSample();

            if (haloMap == null)
            {
                chunkToProcess.IsDirty = false;
                if (enableProfilerMarkers) Profiler.EndSample(); 
                continue; 
            }

            if (enableProfilerMarkers) Profiler.BeginSample("ProcessDirtyChunks - Erode");
            Particle[] outgoingParticles = _eroder.Erode(chunkToProcess, haloMap, worldConfig.worldSeed);
            if (enableProfilerMarkers) Profiler.EndSample();

            if (enableProfilerMarkers) Profiler.BeginSample("ProcessDirtyChunks - DeconstructHaloMap");
            List<IChunk> dirtiedNeighbors = HaloMapUtility.DeconstructHaloMap(haloMap, chunkToProcess, borderSize, _activeChunks, _unloadedChunkCache);
            if (enableProfilerMarkers) Profiler.EndSample();

            RenderTexture.ReleaseTemporary(haloMap);

            if (enableProfilerMarkers) Profiler.BeginSample("ProcessDirtyChunks - ProcessTransfers");
            ParticleTransferUtility.ProcessTransfers(chunkToProcess, outgoingParticles, this);
            if (enableProfilerMarkers) Profiler.EndSample();

            if (dirtiedNeighbors != null)
            {
                foreach (var neighbor in dirtiedNeighbors)
                {
                    MarkChunkAsDirty(neighbor);
                }
            }

            if (_activeTerrainObjects.TryGetValue(chunkToProcess.Coordinates, out Terrain terrainToUpdate))
            {
                // ApplyToTerrain wird bereits in SpawnChunkGameObject gemessen,
                // aber hier passiert es auch nach der Erosion.
                if (enableProfilerMarkers) Profiler.BeginSample("ProcessDirtyChunks - ApplyToTerrain");
                if (enableDataLogging) _stopwatch.Restart(); // Starte Timer für ApplyToTerrain Messung

                chunkToProcess.ApplyToTerrain(terrainToUpdate);

                if (enableDataLogging)
                {
                    _stopwatch.Stop();
                    _maxApplyToTerrainMsInInterval = Mathf.Max(_maxApplyToTerrainMsInInterval, (float)_stopwatch.Elapsed.TotalMilliseconds);
                }
                if (enableProfilerMarkers) Profiler.EndSample();

                if (connectNeighbors)
                {
                    if (enableProfilerMarkers) Profiler.BeginSample("ProcessDirtyChunks - SetTerrainNeighbors");
                    SetTerrainNeighbors(chunkToProcess, terrainToUpdate);
                    if (enableProfilerMarkers) Profiler.EndSample();
                }
            }

            chunkToProcess.IsDirty = false;
            if (enableProfilerMarkers) Profiler.EndSample(); // <-- Ende für "Single Chunk Loop"
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
            newCacheEntry.Heightmap = chunkFactory.GenerateBaseHeightmap(coords, worldConfig.worldSeed);
            newCacheEntry.InitialParticlesDropped = false;

            _unloadedChunkCache[coords] = newCacheEntry;
            return newCacheEntry;
        }
    }

    #endregion

    #region Performance Recording Helpers
    private string GetLogFilePath()
    {
        return DebuggingFilePaths.GetPerformanceLogPath(logFilename);
    }

    private void SaveRecordedData()
    {
        string filePath = GetLogFilePath();
        try
        {
            using (StreamWriter writer = new StreamWriter(filePath, false))
            {
                writer.WriteLine("Time(s),AvgFPS,MaxProcessDirtyChunks(ms),MaxApplyToTerrain(ms)");

                for (int i = 0; i < _recordedTime.Count; i++)
                {
                    writer.WriteLine(string.Format(CultureInfo.InvariantCulture,
                        "{0:F2},{1:F2},{2:F3},{3:F3}",
                        _recordedTime[i],
                        _recordedAvgFps[i],
                        _recordedMaxCpuDirtyChunks[i],
                        _recordedMaxCpuApplyTerrain[i]));
                }
            }
            Debug.Log($"Performance-Daten erfolgreich gespeichert unter: {filePath}");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Fehler beim Speichern der Performance-Daten nach {filePath}: {ex.Message}");
        }
    }
    #endregion
}

