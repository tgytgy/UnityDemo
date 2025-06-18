using System.Collections.Generic;
using UnityEngine;

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
        List<BasePanel> list;
        if (_panelNameDic.TryGetValue(panelName, out list))
        {
            
        }
    }
}
