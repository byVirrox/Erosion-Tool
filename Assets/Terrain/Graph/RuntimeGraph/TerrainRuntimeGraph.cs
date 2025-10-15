using System.Collections.Generic;
using UnityEngine;

// Hält die zur Laufzeit ausführbare, lineare Liste von Knoten-Operationen.
// Dies ist das Asset, das du am Ende im WorldManager zuweisen wirst.
public class TerrainRuntimeGraph : ScriptableObject
{
    [SerializeReference]
    public List<TerrainRuntimeNode> nodes = new List<TerrainRuntimeNode>();
}