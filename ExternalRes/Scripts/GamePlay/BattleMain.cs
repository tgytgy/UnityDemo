using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BattleMain : MonoBehaviour
{
    private Vector3[] _areaCorners;

    private void Awake()
    {
        var battleArea = Utils.GetNode(transform, "BattleArea");
        var spriteRenderer = battleArea.GetComponent<SpriteRenderer>();
        _areaCorners = Utils.GetSpriteCorners(spriteRenderer);
        Destroy(spriteRenderer.gameObject);
        CreateFloor();
        CreateWall();
    }

    private void CreateFloor()
    {
        var floorGo = new GameObject("Floor");
        var rigidBody = floorGo.AddComponent<Rigidbody2D>();
        var tr = floorGo.transform;
        var pos1 = _areaCorners[0];
        var pos2 = _areaCorners[3];
        tr.SetParent(transform);
        floorGo.AddComponent<BoxCollider2D>();
        floorGo.layer = 6;
        tr.position = (pos1 + pos2) / 2 - new Vector3(0, 0.5f, 0);
        tr.localScale = new Vector3(pos1.x - pos2.x, 1, 1);
        rigidBody.bodyType = RigidbodyType2D.Static;
    }

    private void CreateWall()
    {
        CreateWallCommon("WallLeftGo", _areaCorners[0], _areaCorners[1], new Vector3(-0.5f, 0, 0));
        CreateWallCommon("WallRightGo", _areaCorners[3], _areaCorners[2], new Vector3(0.5f, 0, 0));
    }

    private void CreateWallCommon(string nm, Vector3 pos1, Vector3 pos2, Vector3 posOffset)
    {
        var wallGo = new GameObject(nm);
        var rigidBody = wallGo.AddComponent<Rigidbody2D>();
        var tr = wallGo.transform;
        tr.SetParent(transform);
        wallGo.AddComponent<BoxCollider2D>();
        tr.position = (pos1 + pos2) / 2 + posOffset;
        tr.localScale = new Vector3(1, pos2.y - pos1.y, 1);
        rigidBody.bodyType = RigidbodyType2D.Static;
    }
}