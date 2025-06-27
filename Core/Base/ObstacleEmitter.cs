using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class ObstacleEmitter : MonoBehaviour
{
   
    private Vector3[] _emitterAreaCorners;
    private Vector3[] _receiveAreaCorners;
    private CharacterInput2D _characterInput2D;

    private void Awake()
    {
        var emitterTr = Utils.GetNode(transform, "EmitterArea");
        var receiveTr = Utils.GetNode(transform, "ReceiveArea");
        _emitterAreaCorners = Utils.GetSpriteCorners(emitterTr.GetComponent<SpriteRenderer>());
        _receiveAreaCorners = Utils.GetSpriteCorners(receiveTr.GetComponent<SpriteRenderer>());
        _characterInput2D = new CharacterInput2D();
        //Destroy(emitterTr.gameObject);
        Destroy(receiveTr.gameObject);
        _characterInput2D.Player.Jump.performed += OnJumpPerformed;
    }
    
    private void OnEnable()
    {
        _characterInput2D.Enable();
    }

    private void OnDisable()
    {
        _characterInput2D.Disable();
    }
    
    private void Shoot()
    {
        var pos = Utils.GetRandomPosInRect(_emitterAreaCorners[0], _emitterAreaCorners[2]);
        var go = new GameObject("Pos");
        go.transform.SetParent(transform);
        go.transform.position = pos;
    }
    
    private void OnJumpPerformed(InputAction.CallbackContext ctx)
    {
        Shoot();
    }
}