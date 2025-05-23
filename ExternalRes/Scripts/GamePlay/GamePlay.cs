using System;
using UnityEngine;

public class GamePlay : MonoBehaviour
{
    private Vector3 _angle;
    private Vector3 _angleDiff;
    private void Start()
    {
        _angle = new Vector3(0, 0, 0);
        _angleDiff = new Vector3(0.0f, 1f, 0);
    }

    private void FixedUpdate()
    {
        _angle += _angleDiff;
        transform.eulerAngles = _angle;
    }
}
