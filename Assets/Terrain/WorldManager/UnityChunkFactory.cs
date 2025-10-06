using UnityEngine;

[CreateAssetMenu(fileName = "NewUnityChunkFactory", menuName = "Terrain/Unity Chunk Factory")]
public class UnityChunkFactory : ScriptableObject, IChunkFactory
{
    [Header("Dependencies")]

    [SerializeField] private FbmNoiseGenerator noiseGenerator;

    private INoiseGenerator _noiseGenerator;

    private void OnEnable()
    {
        _noiseGenerator = noiseGenerator;
    }


    public IChunk CreateChunk(GridCoordinates coords, int resolution)
    {
        if (_noiseGenerator == null)
        {
            Debug.LogError("NoiseGenerator is not assigned to the ChunkFactory.");
            return null;
        }

        RenderTexture heightmap = _noiseGenerator.GenerateTexture(coords, resolution, 0);


        var newChunk = new UnityChunk(coords, heightmap);

        return newChunk;
    }

    public INoiseGenerator GetNoiseGenerator()
    {
        return _noiseGenerator;
    }
}