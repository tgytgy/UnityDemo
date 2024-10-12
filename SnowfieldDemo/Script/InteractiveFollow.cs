using System;
using UnityEngine;

public class InteractiveFollow : MonoBehaviour
{
    private void Awake()
    {
        InteractiveManager.Instance.SetInteractiveFollow(this);
        InteractiveManager.Instance.SetCrtAreaTr(GameObject.Find("Plane").transform);
    }

    public Vector3 GetCrtPos()
    {
        return transform.position;
    } 
}
