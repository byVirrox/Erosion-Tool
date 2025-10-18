using System.Collections.Generic;
using UnityEngine;

public class GraphExecutorContext
{
    public GridCoordinates Coords { get; set; }
    public int Resolution { get; set; }
    public int BorderSize { get; set; }
    public Dictionary<int, RenderTexture> NodeResults { get; } = new Dictionary<int, RenderTexture>();
}
