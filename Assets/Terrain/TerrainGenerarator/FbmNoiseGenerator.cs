using Codice.CM.Common;
using UnityEngine;

[CreateAssetMenu(fileName = "NewFbmGenerator", menuName = "Terrain/FBM Generator Asset")]
public class FbmNoiseGenerator : ScriptableObject, INoiseGenerator
{
    [Header("Dependencies")]
    public ComputeShader fbmShader;
    public FbmConfig config;

    [Header("Generation Seed")]
    public int seed = 0;

    private int _lastUsedSeed = -1;

    private ComputeBuffer _permBuffer;

    public PermutationTable permutationTable;

    private void OnEnable()
    {
        if (_permBuffer == null)
        {
            _permBuffer = new ComputeBuffer(512, sizeof(int));
        }
        UpdatePermutationData();
    }

    private void OnValidate()
    {

        if (seed != _lastUsedSeed)
        {
            UpdatePermutationData();
        }
    }

    private void UpdatePermutationData()
    {
        if (permutationTable == null)
        {
            return;
        }

        if (_permBuffer == null)
        {
            _permBuffer = new ComputeBuffer(512, sizeof(int));
        }

        Debug.Log($"Permutationstabelle wird neu generiert mit Seed: {seed}");

        int[] perm = (seed != 0)
            ? permutationTable.GetPermutationTable(seed)
            : permutationTable.GetPermutationTable();

        _permBuffer.SetData(perm);

        _lastUsedSeed = seed;
    }

    private void OnDisable()
    {
        _permBuffer?.Release();
        _permBuffer = null;
    }


    /// <summary>
    /// Generates the base FBM heightmap for the given chunk using a compute shader.
    /// </summary>
    public RenderTexture GenerateTexture(GridCoordinates coords, int resolution, int borderSize)
    {
        if (fbmShader == null || config == null)
        {
            Debug.LogError("FBM Shader or Config is not assigned.");
            return null;
        }

        int kernel = fbmShader.FindKernel("GenerateFbmNoise");

        int targetResolution = resolution + borderSize * 2;
        RenderTexture targetTexture = RenderTexture.GetTemporary(targetResolution, targetResolution, 0, RenderTextureFormat.RFloat);
        targetTexture.enableRandomWrite = true;

        fbmShader.SetInts("chunkCoord", new int[] { coords.X, coords.Y });
        fbmShader.SetInt("resolution", resolution);
        fbmShader.SetInt("borderSize", borderSize); 

        fbmShader.SetVector("offset", config.offset);
        fbmShader.SetVector("scale", new Vector2(config.scale * config.xScale, config.scale * config.yScale));
        fbmShader.SetFloat("persistence", config.persistence);
        fbmShader.SetFloat("lacunarity", config.lacunarity);
        fbmShader.SetFloat("heightScale", config.scale * config.heightScale);
        fbmShader.SetInt("octaves", config.octaves);
        fbmShader.SetInt("noiseType", (int)config.noiseType);
        fbmShader.SetInt("fbmType", (int)config.fbmType);

        if (_permBuffer != null) fbmShader.SetBuffer(kernel, "perm", _permBuffer);
        fbmShader.SetTexture(kernel, "Result", targetTexture);

        int threadGroups = Mathf.CeilToInt(targetResolution / 8.0f);
        fbmShader.Dispatch(kernel, threadGroups, threadGroups, 1);

        return targetTexture;
    }

}
