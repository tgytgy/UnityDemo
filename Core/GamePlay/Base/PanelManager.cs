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

    public PanelManager()
    {
        _panelIdDic = new Dictionary<int, BasePanel>();
        _panelNameDic = new Dictionary<string, List<BasePanel>>();
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
            go.transform.SetParent(canvasGo.transform);
            go.transform.localPosition = Vector3.zero;
        }
    }
    
    public void PanelOn(string trName, string panelName, Transform rootNode)
    {
        var go = AssetManager.LoadPrefab(trName, rootNode);
        if (!go)
        {
            Debug.LogError($"Prefab: {trName} Not Found!");
            return;
        }

        var s = go.transform.GetComponent<BasePanel>();
        if (!s)
        {
            Debug.LogError($"Prefab: {trName} Don't Have BasePanel Component!");
            return;
        }
        _panelIdDic.Add(go.GetInstanceID(), s);
        if (_panelNameDic.TryGetValue(panelName, out var list))
        {
            list ??= new List<BasePanel>();
            list.Add(s);
        }
    }
}
