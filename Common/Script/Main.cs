using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UI;

public class Main : MonoBehaviour
{
    void Start()
    {
        var img = transform.Find("RawImage").GetComponent<RawImage>();
        /*Addressables.LoadAssetAsync<GameObject>("Assets/Prefab/Cube.prefab").Completed += (handle) =>
        {
            // 预设物体
            GameObject prefabObj = handle.Result;
            // 实例化
            GameObject cubeObj = Instantiate(prefabObj);
        };*/

        /*Addressables.InstantiateAsync("Assets/Prefab/Cube.prefab").Completed += (handle) =>
        {
            // 已实例化的物体
            GameObject cubeObj = handle.Result;
        };*/

        Addressables.LoadAssetAsync<Texture2D>("Assets/Texture/img_circle.png").Completed += (obj) =>
        {
            // 图片
            Texture2D tex2D = obj.Result;
            img.texture = tex2D;
            img.GetComponent<RectTransform>().sizeDelta = new Vector2(tex2D.width, tex2D.height);
        };
    }
    
    private async void InstantiateCube()
    {
        // 虽然这里使用了Task，但并没有使用多线程
        GameObject prefabObj = await Addressables.LoadAssetAsync<GameObject>("Assets/Prefabs/Cube.prefab").Task;
        // 实例化
        GameObject cubeObj = Instantiate(prefabObj);
		
        // 也可直接使用InstantiateAsync方法
        // GameObject cubeObj = await Addressables.InstantiateAsync("Assets/Prefabs/Cube.prefab").Task;
    }
}
