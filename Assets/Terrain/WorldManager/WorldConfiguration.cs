using UnityEngine;

[CreateAssetMenu(fileName = "NewWorldConfig", menuName = "Terrain/World Configuration")]
public class WorldConfig : ScriptableObject
{
    [Header("World Generation")]
    public int worldSeed = 0;

    [Header("Chunk Loading")]
    [Tooltip("The number of chunks to be loaded in each direction from the center.")]
    public int viewDistanceInChunks = 3;
}