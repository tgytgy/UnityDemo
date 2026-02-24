using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PersonController2D : MonoBehaviour
{
    private CharacterInput2D _characterInput2D;
    private Vector2 _moveDirection;
    private bool _enableTwiceJump;
    private Rigidbody2D _rigidBody2D;
    private float _overlapRadius;
    private int _groundLayer;
    private float _jumpHeight;
    private float _jumpStartVelocity;
    private float _gravityScale;
    private void Awake()
    {
        _overlapRadius = 0.52f;
        _jumpHeight = 3;
        _gravityScale = 5;
        _jumpStartVelocity = Mathf.Sqrt(Mathf.Abs(2 * Physics2D.gravity.y * _jumpHeight * _gravityScale));
        _groundLayer = LayerMask.GetMask("Ground");
        _rigidBody2D = transform.GetComponent<Rigidbody2D>();
        _rigidBody2D.gravityScale = _gravityScale;
        _characterInput2D = new CharacterInput2D();
        _characterInput2D.Player.Move.performed += OnMovePerformed;
        _characterInput2D.Player.Move.canceled += OnMoveCanceled;
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
    
    private void FixedUpdate()
    {
        _rigidBody2D.velocity = new Vector2(_moveDirection.x * 5, _rigidBody2D.velocity.y);
    }

    public bool IsGrounded()
    {
        return Physics2D.OverlapCircle(transform.position, _overlapRadius, _groundLayer);
    }
    
    private void OnMovePerformed(InputAction.CallbackContext ctx)
    {
        _moveDirection = ctx.ReadValue<Vector2>();
    }
    
    private void OnMoveCanceled(InputAction.CallbackContext ctx)
    {
        _moveDirection = Vector2.zero;
    }

    private void OnJumpPerformed(InputAction.CallbackContext ctx)
    {
        if (IsGrounded())
        {
            _rigidBody2D.velocity = new Vector2(_rigidBody2D.velocity.x, _jumpStartVelocity);
            _enableTwiceJump = true;
            return;
        }

        if (!_enableTwiceJump) return;
        _rigidBody2D.velocity = new Vector2(_rigidBody2D.velocity.x, _jumpStartVelocity);
        _enableTwiceJump = false;
    }
}
