using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

public abstract class BaseNode : Node
{
    protected BaseGraphView RootGraphView;
    protected Dictionary<string, Port> PortDic;
    public void Initialize(BaseGraphView baseGraphView)
    {
        RootGraphView = baseGraphView;
        title = GetType().Name;
        capabilities |= Capabilities.Movable | Capabilities.Deletable | Capabilities.Selectable;
        if (string.IsNullOrEmpty(viewDataKey))
        {
            viewDataKey = GUID.Generate().ToString();
        }
        PortDic = new Dictionary<string, Port>();
        OnInitDefaultPort();
        OnInitialize();
        OnInitCssStyle();
        RefreshPorts();
        RefreshExpandedState();
    }
    
    protected virtual void OnInitDefaultPort()
    {
        //AddToClassList("math-node");
    }
    
    protected virtual void OnInitCssStyle()
    {
        //AddToClassList("math-node");
    }

    protected virtual void OnInitialize()
    {
        
    }
    
    public virtual object GetSerializableData()
    {
        return null;
    }

    public void LoadFromData(object data, BaseGraphView baseGraphView)
    {
        InitFromLoadData(data);
        Initialize(baseGraphView);
    }
    
    public virtual void InitFromLoadData(object data) { }
    
    protected Port CreatePort(Direction direction, string portName, Port.Capacity capacity = Port.Capacity.Multi, Type type = null)
    {
        var port = Port.Create<Edge>(Orientation.Horizontal, direction, capacity, type ?? typeof(object));
        port.portName = portName;
        port.name = portName;
        switch (direction)
        {
            case Direction.Input:
                if (PortDic.ContainsKey(portName))
                {
                    Debug.LogError($"{portName} port is already exists in {title}, port name can't have same name");
                    return null;
                }
                inputContainer.Add(port);
                PortDic.Add(portName, port);
                break;
            case Direction.Output:
                if (PortDic.ContainsKey(portName))
                {
                    Debug.LogError($"{portName} port is already exists in {title}, port name can't have same name");
                    return null;
                }
                outputContainer.Add(port);
                PortDic.Add(portName, port);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(direction), direction, null);
        }
        return port;
    }

    public Port GetPortByName(string portName)
    {
        return PortDic.GetValueOrDefault(portName);
    }
    
    public virtual string GenerateCode()
    {
        return null;
    }
    
    private List<BaseNode> GetAllConnectedOutputNodes(BaseNode node)
    {
        var connectedNodes = new List<BaseNode>();
        foreach (var child in node.outputContainer.Children())
        {
            if (child is not Port port) continue;
            foreach (var edge in port.connections)
            {
                if (edge.input == null || edge.input.node == null) continue;
                var targetNode = edge.input.node as BaseNode;
                if (targetNode != null && !connectedNodes.Contains(targetNode))
                {
                    connectedNodes.Add(targetNode);
                }
            }
        }
        return connectedNodes;
    }

}