using UnityEngine;

public static class Utils
{
    public static Transform GetNode(Transform tr, string name)
    {
        var targetNode = tr.Find(name);
        if (targetNode)
        {
            return targetNode;
        }
        for (var i = 0; i < tr.childCount; i++)
        {
            targetNode = GetNode(tr.GetChild(i), name);
            if (targetNode)
            {
                return targetNode;
            }
        }

        return null;
    }
}
