using UnityEngine;

[CreateAssetMenu(fileName = "NewParticleErosionConfig", menuName = "Terrain/Particle Erosion Configuration")]
public class ErosionConfig : ScriptableObject
{
    [Header("Simulation Settings")]
    public int numErosionIterations = 50000;
    public int erosionBrushRadius = 3;

    [Header("Particle Lifetime & Physics")]
    public int maxLifetime = 30;
    [Range(0, 1)]
    public float inertia = 0.3f;
    public float gravity = 4;

    [Header("Water & Sediment")]
    public float startSpeed = 1;
    public float startWater = 1;
    public float evaporateSpeed = .01f;

    [Header("Erosion & Deposition")]
    public float sedimentCapacityFactor = 3;
    public float minSedimentCapacity = .01f;
    public float depositSpeed = 0.3f;
    public float erodeSpeed = 0.3f;
}
