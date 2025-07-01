using System;

public class MainPanel : BasePanel
{
    private void Awake()
    {
        Utils.BindBtnByName(transform, "Button", StartBtnClick);
    }

    private void StartBtnClick()
    {
        PanelManager.Instance.PanelOn("prefab_main.prefab", "BattleMain", WorldNode.Node1);
        CloseSelf();
    }
}
