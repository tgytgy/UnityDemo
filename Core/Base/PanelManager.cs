using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum UILayer
{
    LayerTop0,
    LayerTop1,
    LayerTop2,
    LayerMiddle0,
    LayerMiddle1,
    LayerMiddle2,
    LayerBottom1,
    LayerBottom2,
    LayerBottom3,
}

public enum WorldNode
{
    Node1,
    Node2,
    Node3,
    Node4
}

public class PanelManager : Singleton<PanelManager>
{
    private Dictionary<int, BasePanel> _panelIdDic;
    private Dictionary<string, List<BasePanel>> _panelNameDic;
    private Dictionary<UILayer, Transform> _layerDic;
    private Dictionary<WorldNode, Transform> _worldDic;

    public PanelManager()
    {
        _panelIdDic = new Dictionary<int, BasePanel>();
        _panelNameDic = new Dictionary<string, List<BasePanel>>();
        _layerDic = new Dictionary<UILayer, Transform>();
        _worldDic = new Dictionary<WorldNode, Transform>();
    }

    public void InitLayers()
    {
        var worldRootNode = new GameObject("World");
        var canvasGo = new GameObject("Canvas");
        var canvas = canvasGo.AddComponent<Canvas>();
        var canvasScaler = canvasGo.AddComponent<CanvasScaler>();
        canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasScaler.referenceResolution = new Vector2(1920, 1080);
        canvasScaler.matchWidthOrHeight = 1;
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = CameraManager.Instance.GetUICamera();
        foreach (UILayer value in Enum.GetValues(typeof(UILayer)))
        {
            var go = new GameObject(value.ToString());
            var rectTransform = go.AddComponent<RectTransform>();
            go.transform.SetParent(canvasGo.transform);
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.localScale = Vector3.one;
            go.transform.localPosition = Vector3.zero;
            _layerDic[value] = rectTransform;
        }

        foreach (WorldNode value in Enum.GetValues(typeof(WorldNode)))
        {
            var go = new GameObject(value.ToString());
            var tr = go.transform;
            tr.SetParent(worldRootNode.transform);
            tr.localScale = Vector3.one;
            tr.localPosition = Vector3.zero;
            _worldDic[value] = tr;
        }
    }
    
    public BasePanel PanelOn(string trName, string panelName, Transform rootNode, Dictionary<string, object> args = null, bool isUI = true)
    {
        var go = AssetManager.LoadPrefab(trName, rootNode);
        if (!go)
        {
            Debug.LogError($"Prefab: {trName} Not Found!");
            return null;
        }

        var s = go.transform.GetComponent<BasePanel>();
        if (!s)
        {
            Debug.LogError($"Prefab: {trName} Don't Have BasePanel Component!");
            return null;
        }

        var tr = go.transform;
        if (isUI)
        {
            var rectTr = go.GetComponent<RectTransform>();
            rectTr.anchorMin = Vector2.zero;
            rectTr.anchorMax = Vector2.one;
            rectTr.offsetMin = Vector2.zero;
            rectTr.offsetMax = Vector2.zero;
            rectTr.pivot = new Vector2(0.5f, 0.5f);
        }
        tr.localPosition = Vector2.zero;
        tr.localScale = Vector2.one;
        s.InitByArgs(args);
        _panelIdDic.Add(go.GetInstanceID(), s);
        if (_panelNameDic.ContainsKey(panelName))
        {
            _panelNameDic[panelName].Add(s);
            
        }
        else
        {
            _panelNameDic.Add(panelName, new List<BasePanel>() { s });   
        }

        return s;
    }

    public BasePanel PanelOn(string trName, string panelName, UILayer uiLayer, Dictionary<string, object> args = null)
    {
        return PanelOn(trName, panelName, _layerDic[uiLayer], args);
    }
    
    public BasePanel PanelOn(string trName, string panelName, WorldNode worldNode, Dictionary<string, object> args = null)
    {
        return PanelOn(trName, panelName, _worldDic[worldNode], args, false);
    }
}
