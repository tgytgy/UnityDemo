using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//[RequireComponent(typeof(LineRenderer))]
public class Rope2D : MonoBehaviour
{
    [Header("Rope")]
    [SerializeField] private int _segmentCount = 50;
    [SerializeField] private float _segmentLength = 0.225f;

    [Header("Physics")]
    [SerializeField] private Vector2 _gravityForce = new Vector2(0, -10f);
    [SerializeField] private float _springRate = 1f;
    [SerializeField] private float _damplingForce = 0.99f;
    [SerializeField] private LayerMask _collisionMask;
    [SerializeField] private float _collisionRadius = 0.1f;
    [SerializeField] private float _bounceFactor = 0.1f;
    [SerializeField] private float _correctionClampAmount = 0.1f;
    
    
    [Header("Optimization")]
    [SerializeField] private int _collisionSegmentInterval = 2;
    [SerializeField] private GameObject _item;
    //private LineRenderer _lineRenderer;
    private readonly List<RopeSegment> _ropeSegments = new List<RopeSegment>();
    private List<GameObject> _itemArr = new List<GameObject>();
    private Vector2 _ropeStartPos;
    
    private class RopeSegment
    {
        public Vector2 CurrentPos;
        public Vector2 OldPos;

        public RopeSegment(Vector2 pos)
        {
            CurrentPos = pos;
            OldPos = pos;
        }        
    }


    private void Awake()
    {
        //_lineRenderer = gameObject.GetComponent<LineRenderer>();
        //_lineRenderer.positionCount = _segmentCount;
        if (Camera.main != null) _ropeStartPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        for (var i = 0; i < _segmentCount; i++)
        {
            _ropeSegments.Add(new RopeSegment(_ropeStartPos));
            _ropeStartPos.y -= _segmentLength;
            _itemArr.Add(Instantiate(_item));
        }
    }

    private void Update()
    {
        DrawRope();
    }

    private void DrawRope()
    {
        var points = new Vector3[_segmentCount];
        for (var i = 0; i < _segmentCount; i++)
        {
            points[i] = _ropeSegments[i].CurrentPos;
            points[i].z = 5;
            _itemArr[i].transform.position = points[i];
        }
        //_lineRenderer.SetPositions(points);
    }

    private void FixedUpdate()
    {
        Simulate(Time.fixedDeltaTime);
    }

    private void Simulate(float dt)
    {
        var firstSeg = _ropeSegments[0];
        if (Camera.main != null) firstSeg.CurrentPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        for (var i = 1; i < _segmentCount; i++)
        {
            var seg = _ropeSegments[i];
            Vector2 a;
            var changeDir = _ropeSegments[i - 1].CurrentPos - seg.CurrentPos;
            var changeDirNor = changeDir.normalized;
            var dis = changeDir.magnitude;
            if (Mathf.Abs(dis - _segmentLength) <= 0.001f)
            {
                a = Vector2.zero;
            }
            else if(dis < _segmentLength)
            {
                a = _gravityForce;
            }
            else
            {
                a = _gravityForce + changeDirNor * SpringForce(changeDir);
            }
            var crtPos = seg.CurrentPos;
            seg.CurrentPos = seg.CurrentPos + _damplingForce * (seg.CurrentPos - seg.OldPos) + a * (dt * dt);
            seg.OldPos = crtPos;
        }
    }

    private float SpringForce(Vector2 changeDir)
    {
        //F = -kx
        var force = _springRate * (changeDir.magnitude - _segmentLength);
        return force;
    }
    
    private void ApplyConstrains()
    {
        var firstSeg = _ropeSegments[0];
        if (Camera.main != null) firstSeg.CurrentPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        _ropeSegments[0] = firstSeg;
        for (var i = 0; i < _segmentCount - 1; i++)
        {
            var currentSeg = _ropeSegments[i];
            var nextSeg = _ropeSegments[i + 1];
            var dis = (currentSeg.CurrentPos - nextSeg.CurrentPos).magnitude;
            var difference = dis - _segmentLength;
            var changeDir = (currentSeg.CurrentPos - nextSeg.CurrentPos).normalized;
            var changeVec = changeDir * difference;
            if (i == 0)
            {
                currentSeg.CurrentPos -= changeVec * 0.5f;
                nextSeg.CurrentPos += changeVec * 0.5f;
            }
            else
            {
                nextSeg.CurrentPos += changeVec;
            }

            _ropeSegments[i] = currentSeg;
            _ropeSegments[i + 1] = nextSeg;
        }
    }

    private void HandleCollisions()
    {
        for (var i = 0; i < _segmentCount - 1; i++)
        {
            var colliders = new Collider2D[10];
            var seg = _ropeSegments[i];
            var velocity = seg.CurrentPos - seg.OldPos;
            var count = Physics2D.OverlapCircleNonAlloc(seg.CurrentPos, _collisionRadius, colliders, _collisionMask);
            if(count==0) continue;
            foreach (var c in colliders)
            {
                var closestPoint = c.ClosestPoint(seg.CurrentPos);
                var dis = Vector2.Distance(seg.CurrentPos, closestPoint);
                if (dis < _collisionRadius)
                {
                    var normal = (seg.CurrentPos - closestPoint).normalized;
                    if (normal == Vector2.zero)
                    {
                        normal = (seg.CurrentPos - (Vector2)c.transform.position).normalized;
                    }

                    var depth = _collisionRadius - dis;
                    seg.CurrentPos += normal * depth;

                    velocity = Vector2.Reflect(velocity, normal) * _bounceFactor;
                }
            }

            seg.OldPos = seg.CurrentPos - velocity;
            _ropeSegments[i] = seg;
        }
    }
}
