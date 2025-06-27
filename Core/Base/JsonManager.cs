using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[System.Serializable]
public class ItemList<T>
{
    public T[] items;
}

[System.Serializable]
public class TestSys
{
    public int ID;
    public int SysTp;
    public int OID;
    public int AutoCreate;
    public int UnlockCd;
    public string Class;
    public string ErrSuspend;
    public List<string> LoginUrl;
    public List<string> SyncUrl;
}

public class JsonManager : Singleton<JsonManager>
{
    public static List<T> ReadJsonArr<T>(string name)
    {
        var jsonFile = AssetManager.LoadAssetSync<TextAsset>(name);
        if (jsonFile == null) return null;
        var jsonString = "{\"items\":" + jsonFile.text + "}";
        var itemList = JsonUtility.FromJson<ItemList<T>>(jsonString);
        return itemList.items.ToList();
    }
}
