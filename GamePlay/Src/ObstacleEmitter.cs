using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Random = UnityEngine.Random;

public class ObstacleEmitter : MonoBehaviour
{
   
    private Vector3[] _emitterAreaCorners;
    private Vector3[] _receiveAreaLeft;
    private Vector3[] _receiveAreaRight;
    private void Awake()
    {
        var emitterTr = Utils.GetNode(transform, "EmitterArea");
        var receiveTr = Utils.GetNode(transform, "ReceiveArea");
        var receiveAreaCorners = Utils.GetSpriteCorners(receiveTr.GetComponent<SpriteRenderer>());
        _emitterAreaCorners = Utils.GetSpriteCorners(emitterTr.GetComponent<SpriteRenderer>());
        _receiveAreaLeft = new[] { receiveAreaCorners[0], receiveAreaCorners[1] - receiveAreaCorners[0] };
        _receiveAreaRight = new[] { receiveAreaCorners[3], receiveAreaCorners[2] - receiveAreaCorners[3] };
        Destroy(emitterTr.gameObject);
        Destroy(receiveTr.gameObject);
        StartCoroutine(AutoShoot());
    }
    
    private void Shoot()
    {
        var pos = Utils.GetRandomPosInRect(_emitterAreaCorners[0], _emitterAreaCorners[2]);
        var go = AssetManager.LoadPrefab("prefab_obstacle.prefab", transform);
        var targetPos = GetTargetPos();
        go.transform.GetComponent<Obstacle>().Init(pos, targetPos);
    }

    private Vector2 GetTargetPos()
    {
        var targetPosRange = Random.value > 0.5 ? _receiveAreaLeft : _receiveAreaRight;
        return targetPosRange[0] + Random.value * targetPosRange[1];
    }

    private IEnumerator AutoShoot()
    {
        while (true)
        {
            Shoot();
            yield return new WaitForSeconds(1f);;
        }
    }
    
}