using UnityEngine;

[CreateAssetMenu(fileName = "NewUnityChunkFactory", menuName = "Terrain/Unity Chunk Factory")]
public class ErosionParticleChunkFactory : ScriptableObject, IChunkFactory<ErosionParticleChunk>
{
    [Header("Chunk Blueprint")]
    [Tooltip("The resolution of the heightmap for each chunk (must be 2^n + 1).")]
    public HeightmapResolution heightmapResolution = HeightmapResolution.Res_129;

    [Tooltip("The size of a chunk in world units (meters).")]
    public float chunkSizeInWorldUnits = 128f;

    [Header("Generator Asset")]
    [Tooltip("Drag the generator asset here (e.g., a GraphGenerator or FbmNoiseGenerator).")]
    [SerializeField] private TerrainGenerator generatorAsset;
    [SerializeField] private ComputeShader particleTransferShader;
    private ITerrainGenerator m_TerrainGenerator;

    float IChunkFactory<ErosionParticleChunk>.chunkSizeInWorldUnits => chunkSizeInWorldUnits;

    HeightmapResolution IChunkFactory<ErosionParticleChunk>.heightmapResolution => heightmapResolution;

    private void OnEnable()
    {
        m_TerrainGenerator = generatorAsset as ITerrainGenerator;

        if (m_TerrainGenerator == null && generatorAsset != null)
        {
            Debug.LogError("Das in der ChunkFactory zugewiesene Asset implementiert nicht ITerrainGenerator!");
        }
    }

    public ErosionParticleChunk CreateChunk(GridCoordinates coords, int worldSeed)
    {
        if (m_TerrainGenerator == null)
        {
            Debug.LogError("Kein gültiger ITerrainGenerator in der ChunkFactory zugewiesen.");
            return null;
        }

        var context = new TerrainGenerationContext
        {
            Coords = coords,
            Resolution = (int)heightmapResolution,
            BorderSize = 0,
            WorldSeed = worldSeed
        };

        RenderTexture heightmap = m_TerrainGenerator.Generate(context);

        return new ErosionParticleChunk(coords, heightmap, particleTransferShader);
    }

    public ErosionParticleChunk CreateChunkFromData(GridCoordinates coords, RenderTexture heightmap)
    {
        return new ErosionParticleChunk(coords, heightmap, particleTransferShader);
    }

    public RenderTexture GenerateBaseHeightmap(GridCoordinates coords, int worldSeed)
    {
        if (m_TerrainGenerator == null)
        {
            Debug.LogError("No valid ITerrainGenerator assigned in the ChunkFactory.");
            return null;
        }

        var context = new TerrainGenerationContext
        {
            Coords = coords,
            Resolution = (int)heightmapResolution,
            BorderSize = 0,
            WorldSeed = worldSeed
        };

        return m_TerrainGenerator.Generate(context);
    }

    public ITerrainGenerator GetGenerator()
    {
        return m_TerrainGenerator;
    }


}