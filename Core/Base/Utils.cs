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

    public static Vector3[] GetSpriteCorners(SpriteRenderer spriteRenderer)
    {
        if (spriteRenderer == null)
        {
            Debug.LogError("SpriteRenderer is null！");
            return null;
        }

        var spriteBounds = spriteRenderer.sprite.bounds;
        var transform = spriteRenderer.transform;
        // 计算四个角的本地坐标（相对于精灵中心）
        var localCorners = new Vector3[4];
        localCorners[0] = new Vector3(spriteBounds.min.x, spriteBounds.min.y, 0); // 左下
        localCorners[1] = new Vector3(spriteBounds.min.x, spriteBounds.max.y, 0); // 左上
        localCorners[2] = new Vector3(spriteBounds.max.x, spriteBounds.max.y, 0); // 右上
        localCorners[3] = new Vector3(spriteBounds.max.x, spriteBounds.min.y, 0); // 右下

        // 转换为世界坐标（考虑旋转、缩放和位置）
        var worldCorners = new Vector3[4];
        for (var i = 0; i < 4; i++)
        {
            worldCorners[i] = transform.TransformPoint(localCorners[i]);
        }

        return worldCorners;
    }

    public static Vector2 GetRandomPosInRect(Vector2 pos1, Vector2 pos2)
    {
        var offset = pos2 - pos1;
        return pos1 + new Vector2(Random.Range(0, offset.x), Random.Range(0, offset.y));
    }
}
