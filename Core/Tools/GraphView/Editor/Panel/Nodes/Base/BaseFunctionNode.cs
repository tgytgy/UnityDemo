
using UnityEditor.Experimental.GraphView;

public class BaseFunctionNode : BasePanelGraphNode
{
    public int SortId;
    protected string FunctionName;
    protected Port BaseInputPort;
    protected Port BaseOutputPort;
    
    protected override void OnInitDefaultPort()
    {
        BaseInputPort = CreatePort(Direction.Input, "Input");
        BaseOutputPort = CreatePort(Direction.Output, "Content");
    }
    
    public override string GenerateCode()
    {
        var ret = "";
        foreach (var nodeItem in GraphViewUtils.GetConnectNodesByPort(BaseOutputPort))
        {
            if (nodeItem is BaseFunctionNode item)
            {
                ret += $"{item.GetFunctionName()}()\n";
            }else if (nodeItem is BaseActionNode actionItem)
            {
                ret += $"{actionItem.GenerateCode()}\n";
            }
        }
        return $"{FunctionName}(){{{ret}}}";
    }

    public string GetFunctionName()
    {
        return FunctionName;
    }
}

