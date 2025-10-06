using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class TerrainController : MonoBehaviour
{

    [Header("Erosion Settings")]
    public ComputeShader erosion;
    public int numErosionIterations = 50000;
    public int erosionBrushRadius = 3;

    public int maxLifetime = 30;
    public float sedimentCapacityFactor = 3;
    public float minSedimentCapacity = .01f;
    public float depositSpeed = 0.3f;
    public float erodeSpeed = 0.3f;

    public float evaporateSpeed = .01f;
    public float gravity = 4;
    public float startSpeed = 1;
    public float startWater = 1;
    [Range(0, 1)]
    public float inertia = 0.3f;


    [Header("Mesh Settings")]
    public float perlinOffsetX = 0f;
    public float perlinOffsetY = 0f;
    [Range(0, 1)]
    public float perlinXScale = 0.001f;
    [Range(0, 1)]
    public float perlinYScale = 0.001f;
    public float perlinHeightScale = 0.1f;
    public float perlinPersistance = 8;
    public float perlinLacunarity = 2f;
    [Min(1)]
    public int perlinOctaves = 3;

    int mapSizeWithBorder;

    public void OnValidate()
    {
        foreach (Terrain terrain in Terrain.activeTerrains)
        {
            if (terrain.terrainData == null)
            {
                continue;
            }
            int HeightMapResolution = terrain.terrainData.heightmapResolution;
            float[,] heightMap = new float[HeightMapResolution, HeightMapResolution];
            for (int y = 0; y < HeightMapResolution; ++y)
            {
                for (int x = 0; x < HeightMapResolution; ++x)
                {
                    float worldPositionX = ((float)x / (float)HeightMapResolution) * terrain.terrainData.size.x + terrain.transform.position.x;
                    float worldPositionY = ((float)y / (float)HeightMapResolution) * terrain.terrainData.size.z + terrain.transform.position.z;
                    //heightMap[x, y] = Mathf.PerlinNoise(sampleX, sampleY) * perlinHeightScale;

                      heightMap[y, x] += FractalBrownianNoiseUtils.FractalBrownianMotion((worldPositionX + perlinOffsetX) * perlinXScale, (worldPositionY + perlinOffsetY) * perlinYScale,
                          (fbmX, fbmY) => {
                        return Mathf.PerlinNoise(fbmX, fbmY);
                    }, 1, perlinOctaves, perlinPersistance, perlinLacunarity) * perlinHeightScale;
                    //Debug.Log("Coords x:" + worldPositionX + ", y:" + worldPositionY + " value: " + heightMap[x,y] );
                }
            }
            terrain.terrainData.SetHeights(0, 0, heightMap);
        }
    }


    public void Erode(int mapSize, int[][] map)
    {
        int numThreads = numErosionIterations / 1024;

        // Create brush
        List<int> brushIndexOffsets = new List<int>();
        List<float> brushWeights = new List<float>();

        float weightSum = 0;
        for (int brushY = -erosionBrushRadius; brushY <= erosionBrushRadius; brushY++)
        {
            for (int brushX = -erosionBrushRadius; brushX <= erosionBrushRadius; brushX++)
            {
                float sqrDst = brushX * brushX + brushY * brushY;
                if (sqrDst < erosionBrushRadius * erosionBrushRadius)
                {
                    brushIndexOffsets.Add(brushY * mapSize + brushX);
                    float brushWeight = 1 - Mathf.Sqrt(sqrDst) / erosionBrushRadius;
                    weightSum += brushWeight;
                    brushWeights.Add(brushWeight);
                }
            }
        }
        for (int i = 0; i < brushWeights.Count; i++)
        {
            brushWeights[i] /= weightSum;
        }

        // Send brush data to compute shader
        ComputeBuffer brushIndexBuffer = new ComputeBuffer(brushIndexOffsets.Count, sizeof(int));
        ComputeBuffer brushWeightBuffer = new ComputeBuffer(brushWeights.Count, sizeof(int));
        brushIndexBuffer.SetData(brushIndexOffsets);
        brushWeightBuffer.SetData(brushWeights);
        erosion.SetBuffer(0, "brushIndices", brushIndexBuffer);
        erosion.SetBuffer(0, "brushWeights", brushWeightBuffer);

        int[] randomIndices = new int[numErosionIterations];
        for (int i = 0; i < numErosionIterations; i++)
        {
            int randomX = Random.Range(erosionBrushRadius, mapSize + erosionBrushRadius);
            int randomY = Random.Range(erosionBrushRadius, mapSize + erosionBrushRadius);
            randomIndices[i] = randomY * mapSize + randomX;
        }

        ComputeBuffer randomIndexBuffer = new ComputeBuffer(randomIndices.Length, sizeof(int));
        randomIndexBuffer.SetData(randomIndices);
        erosion.SetBuffer(0, "randomIndices", randomIndexBuffer);

        ComputeBuffer mapBuffer = new ComputeBuffer(map.Length, sizeof(float));
        mapBuffer.SetData(map);
        erosion.SetBuffer(0, "map", mapBuffer);

        erosion.SetInt("borderSize", erosionBrushRadius);
        erosion.SetInt("mapSize", mapSizeWithBorder);
        erosion.SetInt("brushLength", brushIndexOffsets.Count);
        erosion.SetInt("maxLifetime", maxLifetime);
        erosion.SetFloat("inertia", inertia);
        erosion.SetFloat("sedimentCapacityFactor", sedimentCapacityFactor);
        erosion.SetFloat("minSedimentCapacity", minSedimentCapacity);
        erosion.SetFloat("depositSpeed", depositSpeed);
        erosion.SetFloat("erodeSpeed", erodeSpeed);
        erosion.SetFloat("evaporateSpeed", evaporateSpeed);
        erosion.SetFloat("gravity", gravity);
        erosion.SetFloat("startSpeed", startSpeed);
        erosion.SetFloat("startWater", startWater);

        erosion.Dispatch(0, numThreads, 1, 1);
        mapBuffer.GetData(map);

        mapBuffer.Release();
        randomIndexBuffer.Release();
        brushIndexBuffer.Release();
        brushWeightBuffer.Release();
    }
}
