using System;
using UnityEngine;

public static class FractalBrownianNoiseUtils
{
    /// <summary>
    /// Generates Fractal Brownian Motion using a provided noise function.
    /// </summary>
    /// <param name="x">The x-coordinate.</param>
    /// <param name="y">The y-coordinate.</param>
    /// <param name="noiseFunc">The noise function to use for each octave.</param>
    /// <param name="frequency">The base frequency/frequency.</param>
    /// <param name="octaves">The number of layers of detail.</param>
    /// <param name="persistence">The rate at which amplitude decreases per octave.</param>
    /// <param name="lacunarity">The rate at which frequency increases per octave.</param>
    /// <returns>A combined noise value.</returns>
    public static float FractalBrownianMotion(float x, float y, Func<float, float, float> noiseFunc, float frequency, int octaves, float persistence, float lacunarity)
    {
        float total = 0f;
        float amplitude = 1f;

        for (int i = 0; i < octaves; i++)
        {
            total += noiseFunc(x * frequency, y * frequency) * amplitude;
            frequency *= lacunarity;
            amplitude *= persistence;
        }


    return total;
    }
}
