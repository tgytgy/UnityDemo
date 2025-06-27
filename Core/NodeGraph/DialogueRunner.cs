using System;
using UnityEngine;
using UnityEngine.InputSystem;
using XNode;

public class DialogueRunner : MonoBehaviour 
{
    private MainNodeGraph storyGraph;
    private DialogueNode current;
    
    void Start()
    {
        storyGraph = AssetManager.LoadAssetSync<MainNodeGraph>("MainNodeGraph.asset");
        // 查找起始节点
        foreach (Node node in storyGraph.nodes) 
        {
            if (node is DialogueNode && node.GetInputPort("entry").ConnectionCount == 0) 
            {
                current = node as DialogueNode;
                break;
            }
        }
        ShowCurrentNode();
    }
    
    void ShowCurrentNode() 
    {
        Debug.Log($"{current.speakerName}: {current.dialogueText}");
    }
    
    public void ContinueDialogue() 
    {
        current = current.GetNextNode();
        if (current != null) ShowCurrentNode();
        else Debug.Log("对话结束");
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.A))
        {
            //ContinueDialogue();
            //    public int ID;
            // public int SysTp;
            // public int OID;
            // public string Class;
            // public List<string> LoginUrl;
            // public List<string> SyncUrl;
            var list = JsonManager.ReadJsonArr<TestSys>("GD_System.json");
            foreach (var item in list)
            {
                if (item.LoginUrl == null)
                {
                    var i = item;
                }
                else
                {
                    var i = item;
                }
                //Debug.Log($"ID: {item.ID}  SysTp: {item.SysTp} OID: {item.OID} Class: {item.Class} LoginUrl: {item.LoginUrl[0]}");
                //Debug.Log($"ID: {item.ID}  SysTp: {item.SysTp}");
            }
        }
    }
}