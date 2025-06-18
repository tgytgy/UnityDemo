using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class ThirdPersonController : MonoBehaviour
{
    private float _walkSpeed;
    private float _runSpeed;
    private float _speedChangeRate;
    private float _rotationChangeRate;
    private float _targetSpeed;
    private CharacterController _characterController;
    private CharacterInput _characterInput;
    private Vector2 _moveDirection;
    private bool _isSprinting;

    private void Awake()
    {
        _walkSpeed = 4.0f;
        _runSpeed = 6.0f;
        _speedChangeRate = 10.0f;
        _characterController = GetComponent<CharacterController>();
        _characterInput = new CharacterInput();
        _characterInput.Player.Move.performed += OnMovePerformed;
        _characterInput.Player.Move.canceled += OnMoveCanceled;
        _characterInput.Player.Sprint.performed += OnSprintPerformed;
        _characterInput.Player.Sprint.canceled += OnSprintCanceled;
    }

    private void OnMovePerformed(InputAction.CallbackContext ctx)
    {
        _moveDirection = ctx.ReadValue<Vector2>();
    }
    
    private void OnMoveCanceled(InputAction.CallbackContext ctx)
    {
        _moveDirection = Vector2.zero;
    }

    private void OnSprintPerformed(InputAction.CallbackContext ctx)
    {
        _isSprinting = true;
    }
    
    private void OnSprintCanceled(InputAction.CallbackContext ctx)
    {
        _isSprinting = false;
    }
    
    private void Update()
    {
        
    }

    private void Move()
    {
        if (_moveDirection == Vector2.zero)
        {
            _targetSpeed = 0;
        }
        else
        {
            _targetSpeed = _isSprinting ? _runSpeed : _walkSpeed;
        }
        var currentVelocityNor = _characterController.velocity;
        var currentVelocity = new Vector3(currentVelocityNor.x, 0.0f, currentVelocityNor.z).magnitude;
        
    }
    
    private void OnEnable()
    {
        _characterInput.Enable();
    }

    private void OnDisable()
    {
        _characterInput.Disable();
    }
}
