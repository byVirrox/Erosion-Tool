using UnityEngine;

[CreateAssetMenu(fileName = "NewGraphGenerator", menuName = "Terrain/Graph Generator")]
public class GraphGenerator : TerrainGenerator
{
    [Header("Graph Asset")]
    [SerializeField] private TerrainRuntimeGraph terrainGraph;

    [Header("Shader Dependencies")]
    [SerializeField] private ComputeShader fbmShader;
    [SerializeField] private ComputeShader addShader;
    [SerializeField] private ComputeShader operationShader;
    [SerializeField] private ComputeShader splineShader;
    [SerializeField] private PermutationTable permutationTable;

    private GraphExecutor m_GraphExecutor;
    private ComputeBuffer m_PermBuffer;
    private int m_LastSeed = -1;

    private void OnEnable()
    {
        m_PermBuffer = new ComputeBuffer(512, sizeof(int));
    }

    private void OnDisable()
    {
        m_PermBuffer?.Release();
        m_PermBuffer = null;
    }

    public override RenderTexture Generate(TerrainGenerationContext context)
    {
        if (terrainGraph == null)
        {
            Debug.LogError("No TerrainRuntimeGraph assigned to the GraphGenerator!");
            return null;
        }

        if (m_GraphExecutor == null)
        {
            var executorConfig = new GraphExecutorConfig
            {
                FbmShader = fbmShader,
                AddShader = addShader, 
                OperationShader = operationShader,
                SplineShader = splineShader,
                PermutationBuffer = m_PermBuffer
            };

            m_GraphExecutor = new GraphExecutor(executorConfig);
        }

        if (context.WorldSeed != m_LastSeed)
        {
            if (permutationTable == null)
            {
                Debug.LogError("PermutationTable is not assigned to the GraphGenerator!");
                return null;
            }
            int[] perm = permutationTable.GetPermutationTable(context.WorldSeed);
            m_PermBuffer.SetData(perm);
            m_LastSeed = context.WorldSeed;
        }

        return m_GraphExecutor.Execute(terrainGraph, context);
    }
}
