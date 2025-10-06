using UnityEngine;

/// <summary>
/// A interface for any procedural noise generator.
/// </summary>
public interface INoiseGenerator
{
    /// <summary>
    /// Gets a single noise value at a given world coordinate.
    /// This is the core function that your chunk loader will call.
    /// </summary>
    /// <param name="x">The x-coordinate in world space.</param>
    /// <param name="z">The z-coordinate in world space.</param>
    /// <returns>A float value, typically between 0 and 1.</returns>
    float GetValue(float x, float z);

    /// <summary>
    /// The seed for the noise.
    /// </summary>
    int Seed { get; set; }

    /// <summary>
    /// The scale or frequency of the noise.
    /// </summary>
    float Scale { get; set; }
}
