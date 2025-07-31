
using UnityEditor.Experimental.GraphView;

[NodeMenu("Function/Awake")]
public class AwakeNode : BaseFunctionNode
{
    protected override void OnInitialize()
    {
        SortId = 0;
        title = "Awake";
        FunctionName = title;
    }
    
}