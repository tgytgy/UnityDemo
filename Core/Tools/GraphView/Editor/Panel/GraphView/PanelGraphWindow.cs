using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

public class PanelGraphWindow : BaseGraphWindow
{
    
    protected override void OnInit()
    {
    }
    
    protected override void CreateGraphView()
    {
        GraphView = new PanelGraphView()
        {
            name = CurrentAsset.name
        };
        GraphView.StretchToParentSize();
        rootVisualElement.Add(GraphView);
    }
    
    protected override void InitToolBar()
    {
        var toolbar = new Toolbar();
        var btn = new Button(GenerateCode)
        {
            text = "GenerateCode"
        };
        toolbar.Add(btn);
        rootVisualElement.Add(toolbar);
    }
    
    private void GenerateCode()
    {
        var assetPath = AssetDatabase.GetAssetPath(CurrentAsset);
        var directory = System.IO.Path.GetDirectoryName(assetPath);
        var fileName = CurrentAsset.name + ".cs";
        if (directory != null)
        {
            var txtPath = System.IO.Path.Combine(directory, fileName);
            var content = GraphView.GenerateCode();
            System.IO.File.WriteAllText(txtPath, content);
            Debug.Log("Generated txt at: " + txtPath);
        }

        if (CurrentAsset == null)
        {
            Debug.LogError("No GraphAsset loaded.");
            return;
        }
        AssetDatabase.Refresh();
    }
}