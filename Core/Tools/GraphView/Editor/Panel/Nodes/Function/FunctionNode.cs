using System;
using System.Collections.Generic;
using Sirenix.Serialization;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

[NodeMenu("Function/BaseFunction")]
public class FunctionNode : BaseFunctionNode
{
    protected override void OnInitialize()
    {
        var textField = new TextField("FunctionName")
        {
            value = FunctionName??"Default Text"
        };

        textField.RegisterValueChangedCallback(evt =>
        {
            FunctionName = evt.newValue;
        });
        mainContainer.Add(textField);
    }
    
    public override object GetSerializableData()
    {
        return new FunctionNodeData
        {
            functionName = FunctionName
        };
    }

    public override void InitFromLoadData(object data)
    {
        if (data is FunctionNodeData panelData)
        {
            FunctionName = panelData.functionName;
        }
        else
        {
            Debug.LogError("LoadFromData: data is not of type FunctionNodeData");
        }
    }
}

[Serializable]
public class FunctionNodeData
{
    [OdinSerialize] public string functionName;
}