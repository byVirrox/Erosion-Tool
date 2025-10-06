using UnityEngine;

public class ThermalTerrainEroder : ScriptableObject
{
    public ComputeShader thermalErosionShader;
    public int iterations = 50;
    public float talusAngle = 0.2f;

    public void Erode(IChunk<RenderTexture> chunk)
    {
        int kernel = thermalErosionShader.FindKernel("ThermalErosion");
        int resolution = chunk.GetHeightMapData().width;

        RenderTexture readMap = RenderTexture.GetTemporary(resolution, resolution, 0, RenderTextureFormat.RFloat);
        Graphics.CopyTexture(chunk.GetHeightMapData(), readMap); 

        RenderTexture writeMap = RenderTexture.GetTemporary(resolution, resolution, 0, RenderTextureFormat.RFloat);
        writeMap.enableRandomWrite = true;

        for (int i = 0; i < iterations; i++)
        {
            thermalErosionShader.SetInt("resolution", resolution);
            thermalErosionShader.SetFloat("talusAngle", talusAngle);
            thermalErosionShader.SetTexture(kernel, "ReadMap", readMap);
            thermalErosionShader.SetTexture(kernel, "WriteMap", writeMap);

            int threadGroups = Mathf.CeilToInt(resolution / 8.0f);
            thermalErosionShader.Dispatch(kernel, threadGroups, threadGroups, 1);

            (readMap, writeMap) = (writeMap, readMap);
        }

        Graphics.CopyTexture(readMap, chunk.GetHeightMapData());

        RenderTexture.ReleaseTemporary(readMap);
        RenderTexture.ReleaseTemporary(writeMap);
    }
}
