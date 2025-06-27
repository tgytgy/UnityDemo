using UnityEditor;
using UnityEngine;
using XNodeEditor;

[CustomNodeEditor(typeof(DialogueNode))]
public class DialogueNodeEditor : NodeEditor 
{
    public override void OnBodyGUI() 
    {
        // 获取节点引用
        DialogueNode node = target as DialogueNode;
        
        // 更新序列化对象
        serializedObject.Update();
        
        // 绘制输入端口
        NodeEditorGUILayout.PortField(node.GetInputPort("entry"));
        
        // 绘制字段
        GUILayout.Label("对话内容");
        node.dialogueText = EditorGUILayout.TextArea(node.dialogueText, GUILayout.Height(60));
        
        node.speakerName = EditorGUILayout.TextField("说话人", node.speakerName);
        node.speakerIcon = (Sprite)EditorGUILayout.ObjectField(
            "头像", 
            node.speakerIcon, 
            typeof(Sprite), 
            false);
        
        // 绘制输出端口
        NodeEditorGUILayout.PortField(node.GetOutputPort("exit"));
        
        // 应用修改
        serializedObject.ApplyModifiedProperties();
    }
}