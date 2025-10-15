using System.Collections.Generic;
using UnityEngine;

// H�lt die zur Laufzeit ausf�hrbare, lineare Liste von Knoten-Operationen.
// Dies ist das Asset, das du am Ende im WorldManager zuweisen wirst.
public class TerrainRuntimeGraph : ScriptableObject
{
    [SerializeReference]
    public List<TerrainRuntimeNode> nodes = new List<TerrainRuntimeNode>();
}