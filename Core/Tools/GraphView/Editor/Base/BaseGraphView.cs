using System;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using System.Linq;
using UnityEditor;
using UnityEngine.UIElements;

public class BaseGraphView : GraphView
{
    protected string AssetName;
    public BaseGraphView()
    {
        this.AddManipulator(new ContentZoomer());
        this.AddManipulator(new ContentDragger());
        this.AddManipulator(new SelectionDragger());
        this.AddManipulator(new RectangleSelector());
        var grid = new GridBackground();
        Insert(0, grid);
        grid.StretchToParentSize();
        SetupZoom(0.05f, 2.0f);
    }
    
    public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
    {
        var compatiblePorts = new List<Port>();

        ports.ForEach((port) =>
        {
            if (startPort == port) return;
            if (startPort.node == port.node) return; // 不连自己
            if (startPort.direction == port.direction) return; // 不连同向端口
            if (startPort.portType != port.portType) return; // 类型必须一致

            compatiblePorts.Add(port);
        });

        return compatiblePorts;
    }
    
    protected void CreateNode(Type nodeType, Vector2 position)
    {
        var node = Activator.CreateInstance(nodeType) as BaseNode;
        if (node == null) return;

        node.Initialize(this);
        node.SetPosition(new Rect(position, new Vector2(200, 150)));
        AddElement(node);
    }

    public virtual string GenerateCode()
    {
        return "";
    }
    
    public void SaveGraph(BaseGraphAsset graphData)
    {
        graphData.Clear();
        foreach (var node in nodes.ToList().OfType<BaseNode>()) // 遍历 GraphView 中所有节点
        {
            var data = new BaseNodeData
            {
                Guid = node.viewDataKey,
                TypeName = node.GetType().FullName,
                Position = node.GetPosition().position,
                CustomData = node.GetSerializableData() // 自定义方法
            };

            graphData.nodes.Add(data);
        }

        foreach (var edge in edges)
        {
            var edgeData = new GraphEdgeData
            {
                FromNodeGuid = edge.output.node.viewDataKey,
                FromPortName = edge.output.portName,
                ToNodeGuid = edge.input.node.viewDataKey,
                ToPortName = edge.input.portName
            };

            graphData.edges.Add(edgeData);
        }
    }

    public void LoadGraph(BaseGraphAsset graphData)
    {
        AssetName = graphData.name;
        DeleteElements(graphElements.ToList());
        var guidToNodeMap = new Dictionary<string, BaseNode>();
        foreach (var nodeData in graphData.nodes.ToList().OfType<BaseNodeData>())
        {
            var type = Type.GetType(nodeData.TypeName);
            if (type == null) continue;
            if (Activator.CreateInstance(type) is not BaseNode node) continue;
            node.viewDataKey = nodeData.Guid;
            node.SetPosition(new Rect(nodeData.Position, new Vector2(200, 100)));
            node.LoadFromData(nodeData.CustomData, this); // 反序列化自定义字段
            AddElement(node);
            guidToNodeMap[node.viewDataKey] = node;
        }
        
        foreach (var edgeData in graphData.edges)
        {
            var outputNode = guidToNodeMap[edgeData.FromNodeGuid];
            var inputNode = guidToNodeMap[edgeData.ToNodeGuid];

            var outputPort = outputNode.GetPortByName(edgeData.FromPortName);
            var inputPort = inputNode.GetPortByName(edgeData.ToPortName);

            var edge = outputPort.ConnectTo(inputPort);
            AddElement(edge);
        }
    }
}
