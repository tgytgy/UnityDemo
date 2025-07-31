using UnityEditor;
using UnityEditor.Callbacks;

public static class GraphAssetOpener
{
    [OnOpenAsset(1)]
    public static bool OnOpenAsset(int instanceID, int line)
    {
        var panelGraphAsset = EditorUtility.InstanceIDToObject(instanceID) as PanelGraphAsset;
        if (panelGraphAsset != null)
        {
            BaseGraphWindow.OpenWithAsset<PanelGraphWindow>(panelGraphAsset);
            return true;
        }
        return false;
    }
}