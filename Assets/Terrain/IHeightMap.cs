// An interface that defines the basic operations of a heightmap.
// This allows for the use of different implementations (e.g., Unity Terrain, a simple
// array-based map, or a GPU-based map) without having to change the code that
// accesses it.
public interface IHeightmap
{
    /// <summary>
    /// The resolution of the heightmap (width and height are equal).
    /// </summary>
    int Size { get; }

    /// <summary>
    /// Returns the height at the specified coordinates.
    /// This corresponds to retrieving a single element from the internal height array.
    /// </summary>
    /// <param name="x">The X-coordinate.</param>
    /// <param name="y">The Y-coordinate.</param>
    /// <returns>The height at the specified position.</returns>
    float GetHeight(int x, int y);

    /// <summary>
    /// Sets the height at the specified coordinates.
    /// This corresponds to setting a single element in the internal height array.
    /// </summary>
    /// <param name="x">The X-coordinate.</param>
    /// <param name="y">The Y-coordinate.</param>
    /// <param name="height">The new height value.</param>
    void SetHeight(int x, int y, float height);

    /// <summary>
    /// Returns the entire height array.
    /// This allows for the efficient retrieval of all data for procedural generation.
    /// The data is returned in [y, x] or [x, y] format, depending on the implementation.
    /// </summary>
    /// <returns>A two-dimensional float array representing the entire heightmap.</returns>
    float[,] GetHeights();

    /// <summary>
    /// Sets the entire heightmap based on the passed-in array.
    /// This is the standard method for applying a complete generated heightmap
    /// to the underlying data structure.
    /// </summary>
    /// <param name="heights">The two-dimensional float array representing the new heightmap.</param>
    void SetHeights(float[,] heights);
}
