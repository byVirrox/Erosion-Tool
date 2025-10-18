using UnityEngine;

[CreateAssetMenu(fileName = "NewFbmGenerator", menuName = "Terrain/FBM Generator Asset")]
public class FbmNoiseGenerator : TerrainGenerator
{
    [Header("Dependencies")]
    public ComputeShader fbmShader;
    public FbmConfig config;
    public PermutationTable permutationTable;

    private int _lastUsedSeed = -1;
    private ComputeBuffer _permBuffer;

    private void OnEnable()
    {
        if (_permBuffer == null)
        {
            _permBuffer = new ComputeBuffer(512, sizeof(int));
        }
    }

    private void OnDisable()
    {
        _permBuffer?.Release();
        _permBuffer = null;
    }


    /// <summary>
    /// Generates the base FBM heightmap for the given chunk using a compute shader.
    /// </summary>
    public override RenderTexture Generate(TerrainGenerationContext context)
    {
        if (fbmShader == null || config == null)
        {
            Debug.LogError("FBM Shader or Config is not assigned to the FbmNoiseGenerator.");
            return null;
        }

        UpdatePermutationData(context.WorldSeed);

        int kernel = fbmShader.FindKernel("GenerateFbmNoise");

        int targetResolution = context.Resolution + context.BorderSize * 2;
        RenderTexture targetTexture = RenderTexture.GetTemporary(targetResolution, targetResolution, 0, RenderTextureFormat.RFloat);
        targetTexture.enableRandomWrite = true;

        fbmShader.SetInts("chunkCoord", new int[] { context.Coords.X, context.Coords.Y });
        fbmShader.SetInt("resolution", context.Resolution);
        fbmShader.SetInt("borderSize", context.BorderSize);

        fbmShader.SetVector("offset", config.offset);
        fbmShader.SetVector("scale", new Vector2(config.scale * config.xScale, config.scale * config.yScale));
        fbmShader.SetFloat("persistence", config.persistence);
        fbmShader.SetFloat("lacunarity", config.lacunarity);
        fbmShader.SetFloat("heightScale", config.scale * config.heightScale);
        fbmShader.SetInt("octaves", config.octaves);
        fbmShader.SetInt("ridgedOctaves", config.ridgedOctaves);
        fbmShader.SetInt("noiseType", (int)config.noiseType);
        fbmShader.SetInt("fbmType", (int)config.fbmType);

        if (_permBuffer != null) fbmShader.SetBuffer(kernel, "perm", _permBuffer);
        fbmShader.SetTexture(kernel, "Result", targetTexture);

        int threadGroups = Mathf.CeilToInt(targetResolution / 8.0f);
        fbmShader.Dispatch(kernel, threadGroups, threadGroups, 1);

        return targetTexture;
    }

    private void UpdatePermutationData(int seed)
    {
        if (seed == _lastUsedSeed && _permBuffer != null)
        {
            return; 
        }

        if (permutationTable == null)
        {
            Debug.LogError("PermutationTable is not assigned.");
            return;
        }

        if (_permBuffer == null)
        {
            _permBuffer = new ComputeBuffer(512, sizeof(int));
        }

        int[] perm = permutationTable.GetPermutationTable(seed);
        _permBuffer.SetData(perm);
        _lastUsedSeed = seed;
    }
}
