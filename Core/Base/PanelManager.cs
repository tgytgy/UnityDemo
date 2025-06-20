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
public class PanelManager : Singleton<PanelManager>
{
    private Dictionary<int, BasePanel> _panelIdDic;
    private Dictionary<string, List<BasePanel>> _panelNameDic;
    private Dictionary<UILayer, Transform> _layerDic;

    public PanelManager()
    {
        _panelIdDic = new Dictionary<int, BasePanel>();
        _panelNameDic = new Dictionary<string, List<BasePanel>>();
        _layerDic = new Dictionary<UILayer, Transform>();
    }

    public void InitLayers()
    {
        var _ = new GameObject("World");
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
    }
    
    public BasePanel PanelOn(string trName, string panelName, Transform rootNode, Dictionary<string, object> args = null)
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

        var tr = go.GetComponent<RectTransform>();
        tr.anchorMin = Vector2.zero;
        tr.anchorMax = Vector2.one;
        tr.offsetMin = Vector2.zero;
        tr.offsetMax = Vector2.zero;
        tr.pivot = new Vector2(0.5f, 0.5f);
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
}
