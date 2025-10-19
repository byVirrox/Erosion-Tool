using System.Collections.Generic;
using UnityEngine;

public static class HaloMapUtility
{
    public static RenderTexture BuildHaloMap(IChunk<RenderTexture> chunk, int borderSize, IReadOnlyDictionary<GridCoordinates, UnityChunk> activeChunks, IReadOnlyDictionary<GridCoordinates, WorldManager.UnloadedChunkData> unloadedChunkCache)
    {
        RenderTexture sourceMap = chunk.GetHeightMapData();
        if (sourceMap == null) return null;

        int resolution = sourceMap.width;
        int borderedResolution = resolution + borderSize * 2;
        var haloMapDescriptor = sourceMap.descriptor;
        haloMapDescriptor.width = borderedResolution;
        haloMapDescriptor.height = borderedResolution;
        RenderTexture haloMap = RenderTexture.GetTemporary(haloMapDescriptor);

        Graphics.CopyTexture(sourceMap, 0, 0, 0, 0, resolution, resolution, haloMap, 0, 0, borderSize, borderSize);

        foreach (NeighborDirection dir in System.Enum.GetValues(typeof(NeighborDirection)))
        {
            GridCoordinates neighborCoords = WorldCoordinateUtils.GetNeighborCoords(chunk.Coordinates, dir);
            RenderTexture neighborMap = null;

            if (activeChunks.TryGetValue(neighborCoords, out var activeNeighbor))
            {
                neighborMap = activeNeighbor.GetHeightMapData();
            }
            else if (unloadedChunkCache.TryGetValue(neighborCoords, out var cachedNeighbor) && cachedNeighbor.Heightmap != null)
            {
                neighborMap = cachedNeighbor.Heightmap;
            }

            if (neighborMap != null)
            {
                CopyNeighborBorder(haloMap, neighborMap, dir, borderSize);
            }
        }
        return haloMap;
    }

    public static List<IChunk> DeconstructHaloMap(RenderTexture haloMap, IParticleErodibleChunk sourceChunk, int borderSize, IReadOnlyDictionary<GridCoordinates, UnityChunk> activeChunks, Dictionary<GridCoordinates, WorldManager.UnloadedChunkData> unloadedChunkCache)
    {
        var dirtiedNeighbors = new List<IChunk>();

        foreach (NeighborDirection dir in System.Enum.GetValues(typeof(NeighborDirection)))
        {
            GridCoordinates neighborCoords = WorldCoordinateUtils.GetNeighborCoords(sourceChunk.Coordinates, dir);

            if (activeChunks.TryGetValue(neighborCoords, out var activeNeighbor))
            {
                CommitBorderToNeighbor(haloMap, activeNeighbor.GetHeightMapData(), dir, borderSize);
                dirtiedNeighbors.Add(activeNeighbor);
            }

            else if (unloadedChunkCache.TryGetValue(neighborCoords, out var cachedNeighborData) && cachedNeighborData.Heightmap != null)
            {
                CommitBorderToNeighbor(haloMap, cachedNeighborData.Heightmap, dir, borderSize);
            }
        }


        int resolution = sourceChunk.GetHeightMapData().width;
        Graphics.CopyTexture(haloMap, 0, 0, borderSize, borderSize, resolution, resolution, sourceChunk.GetHeightMapData(), 0, 0, 0, 0);

        return dirtiedNeighbors;
    }

    private static void CopyNeighborBorder(RenderTexture haloMap, RenderTexture neighborMap, NeighborDirection dir, int borderSize)
    {
        if (neighborMap == null) return;
        BorderCopyData copyData = GetBorderCopyDataForBuild(dir, neighborMap.width, borderSize);

        Graphics.CopyTexture(
            neighborMap, 0, 0,
            copyData.SourceRect.x, copyData.SourceRect.y,
            copyData.SourceRect.width, copyData.SourceRect.height,
            haloMap, 0, 0,
            copyData.DestinationOrigin.x, copyData.DestinationOrigin.y
        );
    }

    private static void CommitBorderToNeighbor(RenderTexture haloMap, RenderTexture neighborMap, NeighborDirection directionOfNeighbor, int borderSize)
    {
        if (neighborMap == null) return;
        BorderCopyData copyData = GetBorderCopyDataForDeconstruct(directionOfNeighbor, neighborMap.width, borderSize);

        Graphics.CopyTexture(
            haloMap, 0, 0,
            copyData.SourceRect.x, copyData.SourceRect.y,
            copyData.SourceRect.width, copyData.SourceRect.height,
            neighborMap, 0, 0,
            copyData.DestinationOrigin.x, copyData.DestinationOrigin.y
        );
    }

    // Berechnet die Koordinaten für das ZUSAMMENBAUEN der HaloMap (Lesen von Nachbarn)
    private static BorderCopyData GetBorderCopyDataForBuild(NeighborDirection dir, int resolution, int borderSize)
    {
        int borderedResolution = resolution + borderSize * 2;
        return dir switch
        {
            NeighborDirection.North => new BorderCopyData { SourceRect = new RectInt(0, 0, resolution, borderSize), DestinationOrigin = new Vector2Int(borderSize, borderedResolution - borderSize) },
            NeighborDirection.East => new BorderCopyData { SourceRect = new RectInt(0, 0, borderSize, resolution), DestinationOrigin = new Vector2Int(borderedResolution - borderSize, borderSize) },
            NeighborDirection.South => new BorderCopyData { SourceRect = new RectInt(0, resolution - borderSize, resolution, borderSize), DestinationOrigin = new Vector2Int(borderSize, 0) },
            NeighborDirection.West => new BorderCopyData { SourceRect = new RectInt(resolution - borderSize, 0, borderSize, resolution), DestinationOrigin = new Vector2Int(0, borderSize) },
            NeighborDirection.NorthEast => new BorderCopyData { SourceRect = new RectInt(0, 0, borderSize, borderSize), DestinationOrigin = new Vector2Int(borderedResolution - borderSize, borderedResolution - borderSize) },
            NeighborDirection.SouthEast => new BorderCopyData { SourceRect = new RectInt(0, resolution - borderSize, borderSize, borderSize), DestinationOrigin = new Vector2Int(borderedResolution - borderSize, 0) },
            NeighborDirection.SouthWest => new BorderCopyData { SourceRect = new RectInt(resolution - borderSize, resolution - borderSize, borderSize, borderSize), DestinationOrigin = new Vector2Int(0, 0) },
            NeighborDirection.NorthWest => new BorderCopyData { SourceRect = new RectInt(resolution - borderSize, 0, borderSize, borderSize), DestinationOrigin = new Vector2Int(0, borderedResolution - borderSize) },
            _ => new BorderCopyData()
        };
    }

    // Berechnet die Koordinaten für das ZERLEGEN der HaloMap (Schreiben auf Nachbarn)
    private static BorderCopyData GetBorderCopyDataForDeconstruct(NeighborDirection dir, int resolution, int borderSize)
    {
        int borderedResolution = resolution + borderSize * 2;
        return dir switch
        {
            NeighborDirection.North => new BorderCopyData { SourceRect = new RectInt(borderSize, borderedResolution - borderSize, resolution, borderSize), DestinationOrigin = new Vector2Int(0, 0) },
            NeighborDirection.East => new BorderCopyData { SourceRect = new RectInt(borderedResolution - borderSize, borderSize, borderSize, resolution), DestinationOrigin = new Vector2Int(0, 0) },
            NeighborDirection.South => new BorderCopyData { SourceRect = new RectInt(borderSize, 0, resolution, borderSize), DestinationOrigin = new Vector2Int(0, resolution - borderSize) },
            NeighborDirection.West => new BorderCopyData { SourceRect = new RectInt(0, borderSize, borderSize, resolution), DestinationOrigin = new Vector2Int(resolution - borderSize, 0) },
            NeighborDirection.NorthEast => new BorderCopyData { SourceRect = new RectInt(borderedResolution - borderSize, borderedResolution - borderSize, borderSize, borderSize), DestinationOrigin = new Vector2Int(0, 0) },
            NeighborDirection.SouthEast => new BorderCopyData { SourceRect = new RectInt(borderedResolution - borderSize, 0, borderSize, borderSize), DestinationOrigin = new Vector2Int(0, resolution - borderSize) },
            NeighborDirection.SouthWest => new BorderCopyData { SourceRect = new RectInt(0, 0, borderSize, borderSize), DestinationOrigin = new Vector2Int(resolution - borderSize, resolution - borderSize) },
            NeighborDirection.NorthWest => new BorderCopyData { SourceRect = new RectInt(0, borderedResolution - borderSize, borderSize, borderSize), DestinationOrigin = new Vector2Int(resolution - borderSize, 0) },
            _ => new BorderCopyData()
        };
    }

    private struct BorderCopyData
    {
        public RectInt SourceRect;
        public Vector2Int DestinationOrigin;
    }
}
