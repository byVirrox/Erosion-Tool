using System.Collections.Generic;
using UnityEngine;

public class TerrainRuntimeGraph : ScriptableObject
{
    [SerializeReference]
    public List<TerrainRuntimeNode> nodes = new List<TerrainRuntimeNode>();
}