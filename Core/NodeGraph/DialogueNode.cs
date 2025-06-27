using UnityEngine;
using XNode;

[Node.NodeTint("#4488ff")] // 节点颜色
[NodeWidth(300)]      // 默认宽度
public class DialogueNode : Node 
{
    [Input] 
    public object entry; // 输入端口
    
    [Output]
    public object exit;  // 输出端口
    
    [TextArea(3,5)]
    public string dialogueText;
    
    public string speakerName;
    public Sprite speakerIcon;
    
    // 获取下一个节点
    public DialogueNode GetNextNode() 
    {
        NodePort exitPort = GetOutputPort("exit");
        if (!exitPort.IsConnected) return null;
        return exitPort.Connection.node as DialogueNode;
    }

    //main 65
    //Plot Editor 30
    //face 25
    //
    
    //返回节点输出值
    public override object GetValue(NodePort port)
    {
        // 返回端口请求的数据
        if (port.fieldName == "exit") 
        {
            return this; // 返回当前节点实例
        }
        
        // 没有数据则返回null
        return null;
    }
}