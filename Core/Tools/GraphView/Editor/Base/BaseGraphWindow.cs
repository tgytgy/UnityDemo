using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

public class BaseGraphWindow : EditorWindow
{
    protected BaseGraphAsset CurrentAsset;
    protected BaseGraphView GraphView;

    public static void OpenWithAsset<T>(BaseGraphAsset asset) where T : BaseGraphWindow
    {
        var window = GetWindow<T>();
        window.titleContent = new GUIContent(asset.name);
        window.Init(asset);
    }

    protected void Init(BaseGraphAsset asset)
    {
        CurrentAsset = asset;
        OnInit();
        CreateGraphView();
        InitToolBar();
        LoadGraph();
    }

    protected virtual void OnInit() { }
    
    protected virtual void InitToolBar() { }
    
    protected virtual void CreateGraphView() { }

    private void OnDisable()
    {
        if (GraphView != null && CurrentAsset != null)
        {
            SaveGraph();
            rootVisualElement.Remove(GraphView);
        }
    }

    private void LoadGraph()
    {
        GraphView.LoadGraph(CurrentAsset);
    }

    public void SaveGraph()
    {
        GraphView.SaveGraph(CurrentAsset);
        EditorUtility.SetDirty(CurrentAsset);
        AssetDatabase.SaveAssets();
    }
}