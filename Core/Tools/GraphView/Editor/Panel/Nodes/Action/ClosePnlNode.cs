using UnityEditor.Experimental.GraphView;

[NodeMenu("Action/ClosePnlNode")]
public class ClosePnlNode : BaseActionNode
{
    protected override void OnInitialize()
    {
        CreatePort(Direction.Input, "Input");
    }
    
    public override string GenerateCode()
    {
        return "CloseSelf();";
    }
}