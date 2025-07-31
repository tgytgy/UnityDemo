using System.Collections.Generic;
using UnityEngine;

public class BaseGraphAsset : ScriptableObject
{
    public List<BaseNodeData> nodes = new List<BaseNodeData>();
    public List<GraphEdgeData> edges = new List<GraphEdgeData>();

    public void Clear()
    {
        nodes.Clear();
        edges.Clear();
    }
}