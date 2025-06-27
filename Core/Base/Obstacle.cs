using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Obstacle : MonoBehaviour
{
    private float _speed;
    private Vector2 _initDir;
    private Rigidbody2D _rigidBody2D;
    private void Awake()
    {
        _speed = 5f;
        _rigidBody2D = transform.GetComponent<Rigidbody2D>();
    }
}
