using UnityEngine;

public static class WorldCoordinateUtils
{
    /// <summary>
    /// Converts a world space position into grid coordinates based on chunk size.
    /// </summary>
    public static GridCoordinates WorldPositionToGridCoordinates(Vector3 position, float chunkSize)
    {
        int x = Mathf.FloorToInt(position.x / chunkSize);
        int y = Mathf.FloorToInt(position.z / chunkSize);
        return new GridCoordinates(x, y);
    }

    /// <summary>
    /// Calculates the grid coordinates of a neighbor in a given direction.
    /// </summary>
    public static GridCoordinates GetNeighborCoords(GridCoordinates currentCoords, NeighborDirection dir)
    {
        return dir switch
        {
            NeighborDirection.North => new GridCoordinates(currentCoords.X, currentCoords.Y + 1),
            NeighborDirection.NorthEast => new GridCoordinates(currentCoords.X + 1, currentCoords.Y + 1),
            NeighborDirection.East => new GridCoordinates(currentCoords.X + 1, currentCoords.Y),
            NeighborDirection.SouthEast => new GridCoordinates(currentCoords.X + 1, currentCoords.Y - 1),
            NeighborDirection.South => new GridCoordinates(currentCoords.X, currentCoords.Y - 1),
            NeighborDirection.SouthWest => new GridCoordinates(currentCoords.X - 1, currentCoords.Y - 1),
            NeighborDirection.West => new GridCoordinates(currentCoords.X - 1, currentCoords.Y),
            NeighborDirection.NorthWest => new GridCoordinates(currentCoords.X - 1, currentCoords.Y + 1),
            _ => currentCoords
        };
    }

    /// <summary>
    /// Returns the opposite direction for a given neighbor direction.
    /// </summary>
    public static NeighborDirection GetOppositeDirection(NeighborDirection dir)
    {
        return dir switch
        {
            NeighborDirection.North => NeighborDirection.South,
            NeighborDirection.NorthEast => NeighborDirection.SouthWest,
            NeighborDirection.East => NeighborDirection.West,
            NeighborDirection.SouthEast => NeighborDirection.NorthWest,
            NeighborDirection.South => NeighborDirection.North,
            NeighborDirection.SouthWest => NeighborDirection.NorthEast,
            NeighborDirection.West => NeighborDirection.East,
            NeighborDirection.NorthWest => NeighborDirection.SouthEast,
            _ => dir
        };
    }
}