using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.GraphView;

public static class GraphViewUtils
{
    public static List<Node> GetConnectNodesByPort(Port port)
    {
        var list = new List<Node>();
        var targetPortType = port.direction == Direction.Input ? Direction.Output : Direction.Input;
        switch (targetPortType)
        {
            case Direction.Input:
                list.AddRange(port.connections.Select(edge => edge.input.node));
                return list;
            case Direction.Output:
                list.AddRange(port.connections.Select(edge => edge.output.node));
                return list;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
}