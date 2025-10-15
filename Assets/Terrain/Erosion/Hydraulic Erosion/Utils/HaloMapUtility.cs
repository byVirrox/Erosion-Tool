using UnityEngine;

public static class HaloMapUtility
{
    /// <summary>
    /// Erstellt eine vollständige HaloMap für einen Chunk.
    /// </summary>
    public static RenderTexture BuildHaloMap(IChunk<RenderTexture> chunk, int borderSize, ITerrainGenerator generator, TerrainGenerationContext context)
    {
        RenderTexture sourceMap = chunk.GetHeightMapData();
        if (sourceMap == null) return null;

        RenderTexture haloMap = generator.Generate(context);
        if (haloMap == null) return null;

        foreach (NeighborDirection dir in System.Enum.GetValues(typeof(NeighborDirection)))
        {
            if (chunk.GetNeighbor(dir) is IChunk<RenderTexture> neighbor)
            {
                CopyNeighborBorder(haloMap, neighbor.GetHeightMapData(), dir, borderSize);
            }
        }

        int resolution = sourceMap.width;
        Graphics.CopyTexture(sourceMap, 0, 0, 0, 0, resolution, resolution, haloMap, 0, 0, borderSize, borderSize);

        return haloMap;
    }

    /// <summary>
    /// Kopiert einen Rand von einer Nachbar-Textur auf die HaloMap.
    /// </summary>
    private static void CopyNeighborBorder(RenderTexture haloMap, RenderTexture neighborMap, NeighborDirection dir, int borderSize)
    {
        if (neighborMap == null) return;
        BorderCopyData copyData = GetBorderCopyData(dir, neighborMap.width, borderSize);

        Graphics.CopyTexture(
            neighborMap, 0, 0,
            copyData.SourceRect.x, copyData.SourceRect.y,
            copyData.SourceRect.width, copyData.SourceRect.height,
            haloMap, 0, 0,
            copyData.DestinationOrigin.x, copyData.DestinationOrigin.y
        );
    }

    /// <summary>
    /// Berechnet die Quell- und Ziel-Rechtecke für das Kopieren von Rändern.
    /// </summary>
    private static BorderCopyData GetBorderCopyData(NeighborDirection dir, int resolution, int borderSize)
    {
        int borderedResolution = resolution + borderSize * 2;

        return dir switch
        {
            NeighborDirection.North => new BorderCopyData
            {
                SourceRect = new RectInt(0, 0, resolution, borderSize),
                DestinationOrigin = new Vector2Int(borderSize, borderedResolution - borderSize)
            },
            NeighborDirection.East => new BorderCopyData
            {
                SourceRect = new RectInt(0, 0, borderSize, resolution),
                DestinationOrigin = new Vector2Int(borderedResolution - borderSize, borderSize)
            },
            NeighborDirection.South => new BorderCopyData
            {
                SourceRect = new RectInt(0, resolution - borderSize, resolution, borderSize),
                DestinationOrigin = new Vector2Int(borderSize, 0)
            },
            NeighborDirection.West => new BorderCopyData
            {
                SourceRect = new RectInt(resolution - borderSize, 0, borderSize, resolution),
                DestinationOrigin = new Vector2Int(0, borderSize)
            },
            NeighborDirection.NorthEast => new BorderCopyData
            {
                SourceRect = new RectInt(0, 0, borderSize, borderSize),
                DestinationOrigin = new Vector2Int(borderedResolution - borderSize, borderedResolution - borderSize)
            },
            NeighborDirection.SouthEast => new BorderCopyData
            {
                SourceRect = new RectInt(0, resolution - borderSize, borderSize, borderSize),
                DestinationOrigin = new Vector2Int(borderedResolution - borderSize, 0)
            },
            NeighborDirection.SouthWest => new BorderCopyData
            {
                SourceRect = new RectInt(resolution - borderSize, resolution - borderSize, borderSize, borderSize),
                DestinationOrigin = new Vector2Int(0, 0)
            },
            NeighborDirection.NorthWest => new BorderCopyData
            {
                SourceRect = new RectInt(resolution - borderSize, 0, borderSize, borderSize),
                DestinationOrigin = new Vector2Int(0, borderedResolution - borderSize)
            },
            _ => new BorderCopyData
            {
                SourceRect = new RectInt(0, 0, 0, 0),
                DestinationOrigin = new Vector2Int(0, 0)
            }
        };
    }

    private struct BorderCopyData
    {
        public RectInt SourceRect;
        public Vector2Int DestinationOrigin;
    }
}