using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Obstacle : MonoBehaviour
{
    private float _speed;
    private Vector2 _initDir;
    private Rigidbody2D _rigidBody2D;
    private bool _startMove;
    private void Awake()
    {
        _startMove = false;
        _speed = 4f;
        _rigidBody2D = transform.GetComponent<Rigidbody2D>();
    }

    public void Init(Vector2 startPos, Vector2 targetPos)
    {
        transform.position = startPos;
        _rigidBody2D.velocity = (targetPos - startPos).normalized * _speed;
        _startMove = true;
    }

    private void OnCollisionEnter2D(Collision2D other)
    {
        var obj = other.transform.name;
        Debug.Log($"collision name: {obj}");
        var normal = other.contacts[0].normal;
        _rigidBody2D.velocity = Vector2.Reflect(_rigidBody2D.velocity, normal);
    }
    
}
