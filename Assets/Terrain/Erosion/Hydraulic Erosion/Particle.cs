using UnityEngine;

[System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
public struct Particle
{
    public Vector2 pos;
    public Vector2 dir;
    public float speed;
    public float water;
    public float sediment;
    public int lifetime;
}
