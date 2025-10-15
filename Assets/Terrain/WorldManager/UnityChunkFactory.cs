using UnityEngine;

[CreateAssetMenu(fileName = "NewUnityChunkFactory", menuName = "Terrain/Unity Chunk Factory")]
public class UnityChunkFactory : ScriptableObject, IChunkFactory<UnityChunk>
{
    [Header("Generator Asset")]
    [Tooltip("Ziehe hier das Generator-Asset rein (z.B. ein GraphGenerator oder FbmNoiseGenerator).")]
    [SerializeField] private TerrainGenerator generatorAsset;
    private ITerrainGenerator m_TerrainGenerator;


    private void OnEnable()
    {
        m_TerrainGenerator = generatorAsset as ITerrainGenerator;

        if (m_TerrainGenerator == null && generatorAsset != null)
        {
            Debug.LogError("Das in der ChunkFactory zugewiesene Asset implementiert nicht ITerrainGenerator!");
        }
    }

    public UnityChunk CreateChunk(GridCoordinates coords, int resolution, int worldSeed)
    {
        if (m_TerrainGenerator == null)
        {
            Debug.LogError("Kein gültiger ITerrainGenerator in der ChunkFactory zugewiesen.");
            return null;
        }

        // Erstelle den Kontext für diesen Aufruf
        var context = new TerrainGenerationContext
        {
            Coords = coords,
            Resolution = resolution,
            BorderSize = 0,
            WorldSeed = worldSeed
        };

        RenderTexture heightmap = m_TerrainGenerator.Generate(context);

        return new UnityChunk(coords, heightmap);
    }

    public ITerrainGenerator GetGenerator()
    {
        return m_TerrainGenerator;
    }
}



/**
[CreateAssetMenu(fileName = "NewUnityChunkFactory", menuName = "Terrain/Unity Chunk Factory")]
public class UnityChunkFactory : ScriptableObject, IChunkFactory<UnityChunk>
{
    private IGraphGenerator _graphGenerator;

    public void Initialize(IGraphGenerator graphGenerator)
    {
        _graphGenerator = graphGenerator;
    }

    public UnityChunk CreateChunk(GridCoordinates coords, int resolution, TerrainRuntimeGraph graph)
    {
        if (_graphGenerator == null)
        {
            Debug.LogError("ChunkFactory has not been initialized with a GraphGenerator.");
            return null;
        }
        if (graph == null)
        {
            Debug.LogError("A valid TerrainRuntimeGraph asset must be provided to create a chunk.");
            return null;
        }
        RenderTexture heightmap = _graphGenerator.Execute(graph, coords, resolution, 0);

        var newChunk = new UnityChunk(coords, heightmap);
        return newChunk;
    }

    public UnityChunk CreateChunk(GridCoordinates coords, int resolution, TerrainRuntimeGraph graph, int worldSeed)
    {
        if (_graphGenerator == null)
        {
            Debug.LogError("ChunkFactory has not been initialized with a GraphGenerator.");
            return null;
        }
        if (graph == null)
        {
            Debug.LogError("A valid TerrainRuntimeGraph asset must be provided to create a chunk.");
            return null;
        }

        var context = new TerrainGenerationContext
        {
            Graph = graph,
            Coords = coords,
            Resolution = resolution,
            BorderSize = 0,
            WorldSeed = worldSeed
        };

        RenderTexture heightmap = _graphGenerator.Execute(context);

        if (heightmap == null)
        {
            Debug.LogError($"Graph execution failed for chunk at {coords}.");
            return null;
        }

        var newChunk = new UnityChunk(coords, heightmap);
        return newChunk;
    }

}
**/