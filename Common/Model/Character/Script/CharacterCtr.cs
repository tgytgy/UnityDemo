using System;
using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

public class CharacterCtr : MonoBehaviour
{
    private enum MoveState
    {
        Idle,
        Walk,
        Run
    }

    private bool _runningPressed;
    private MoveState _crtState;
    private Animator _animator;
    private CharacterInputAction _characterInputAction;
    private CinemachineFreeLook _characterCamera;
    private static readonly int JumpKey = Animator.StringToHash("Jump");
    private static readonly int MoveStateId = Animator.StringToHash("MoveState");

    private void Awake()
    {
        _animator = GetComponent<Animator>();
        _characterInputAction = new CharacterInputAction();
        _characterCamera = FindObjectOfType<CinemachineFreeLook>();
        _characterInputAction.Player.Enable();
        _characterInputAction.Player.Jump.performed += Jump;
        _characterInputAction.Player.Sprint.performed += SprintPerformed;
        _characterInputAction.Player.Sprint.canceled += SprintCanceled;
    }
    
    private void SprintPerformed(InputAction.CallbackContext obj)
    {
        _runningPressed = true;
    }
    
    private void SprintCanceled(InputAction.CallbackContext obj)
    {
        _runningPressed = false;
    }

    private void FixedUpdate()
    {
        var cameraFacing = _characterCamera.State.FinalOrientation;
        var moveDir = _characterInputAction.Player.TestMove.ReadValue<Vector2>();
        var frameState = MoveState.Idle;
        if (moveDir.y > 0)
        {
            transform.rotation = Quaternion.Euler(0, cameraFacing.eulerAngles.y, 0);
            frameState = MoveState.Walk;
            if (_runningPressed)
            {
                frameState = MoveState.Run; 
            }
        }

        if (_crtState == frameState)
        {
            return;
        }
        _crtState = frameState;
        _animator.SetInteger(MoveStateId, (int)_crtState);
    }

    private void Jump(InputAction.CallbackContext obj)
    {
        _animator.SetTrigger(JumpKey);
    }
    
}
