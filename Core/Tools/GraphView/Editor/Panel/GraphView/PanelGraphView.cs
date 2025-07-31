
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;

public class PanelGraphView : BaseGraphView
{
    public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
    {
        base.BuildContextualMenu(evt);
        var mousePos = this.ChangeCoordinatesTo(contentViewContainer, evt.localMousePosition);
        foreach (var entry in NodeRegistry.LoadNodes(typeof(BasePanelGraphNode)))
        {
            evt.menu.AppendAction(entry.MenuPath, (a) => CreateNode(entry.Type, mousePos));
        }
    }
    
    public override string GenerateCode()
    {
        var ret = $"public class {AssetName} : BasePanel {{";
        var functionArr = new List<BaseFunctionNode>();
        foreach (var node in nodes)
        {
            if (node is BaseFunctionNode functionNode)
            {
                functionArr.Add(functionNode);
            }
        }
        functionArr.Sort((a, b) => a.SortId.CompareTo(b.SortId));
        foreach (var node in functionArr)
        {
            ret += node.GenerateCode();
        }

        ret += "}";
        return ret;
    }
}
