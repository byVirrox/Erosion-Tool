using UnityEngine;

[CreateAssetMenu(fileName = "NewParticleErosionConfig", menuName = "Terrain/Particle Erosion Configuration")]
public class ErosionConfig : ScriptableObject
{
    [Header("Simulation Settings")]
    public int numErosionIterations = 50000;
    [Tooltip("Der Radius, in dem abgetragenes/abgelagertes Material verteilt wird. Definiert die MINDESTGRÖSSE der Halo-Zone.")]
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

    [Tooltip("Die gewünschte Breite der 'Pufferzone' für Partikel. Die tatsächliche Halo-Zone ist max(depositionBrushRadius, haloZoneWidth).")]
    public int haloZoneWidth = 8;
    [Tooltip("Activates the creation of Debug-Textures, which show the death position of particles")]
    public bool enableDebugTexture = false;
    [Tooltip("Activates a Debug Message with a Counter for Incoming Particles of the current Chunk processed")]
    public bool enableDebugParticleCount = false;

}
