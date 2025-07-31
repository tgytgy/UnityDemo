using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using UnityEngine;

enum GridType
{
    Path,
    Wall
}

internal class GridData
{
    public GridType GridType;
    public GridData ParentGrid;
    public Vector2Int GridPos { get; private set; }
    public float Cost { get; set; }
    public float FVal { get; private set; }
    public float HVal { get; set; }
    public float GVal { get; set; }

    public GridData(int instanceId, GridType gridType, Vector2Int gridPos, float gVal = 0, float cost = 1, GridData parentGrid = null)
    {
        GridPos = gridPos;
        GridType = gridType;
        GVal = gVal;
        Cost = cost;
        ParentGrid = parentGrid;
    }

    public void CalFVal()
    {
        FVal = GVal + HVal;
    }
}

public class AStarPanel : BasePanel
{
    //render
    private int _gridDensity;
    private Vector2 _gridPivot;
    private float _mapWidth;
    private float _gridWidth;
    private GameObject _gridGo;
    private SpriteRenderer[][] _srArr;

    //algorithms
    private List<GridData> _closeSet;
    private List<GridData> _openSet;
    private GridData[][] _gridDataArr;
    private Vector2Int _startGrid;
    private Vector2Int _targetGrid;
    private bool _alreadyFind;

    private void Awake()
    {
        _gridDensity = 10;
        _mapWidth = 18f;
        _gridPivot = new Vector2(-9, -9);
        _gridWidth = _mapWidth / _gridDensity;
        _gridGo = Utils.GetNode(transform, "RawGrid").gameObject;
        _gridGo.transform.localScale = new Vector3(_gridWidth, _gridWidth, _gridWidth);
        _srArr = new SpriteRenderer[_gridDensity][];
        _gridDataArr = new GridData[_gridDensity][];
        _startGrid = new Vector2Int(0, 0);
        _targetGrid = new Vector2Int(8, 8);
        _closeSet = new List<GridData>();
        _openSet = new List<GridData>();
        CreateGrid();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.A))
        {
            AStarStart();
            RenderStartTarget();
        }
        else if (Input.GetKeyDown(KeyCode.B))
        {
            AStarNext();
            if (_alreadyFind)
            {
                return;
            }
            RenderPath();
        }
    }

    private void CreateGrid()
    {
        var instanceId = 0;
        var mapNode = Utils.GetNode(transform, "MapNode");
        for (var x = 0; x < _gridDensity; x++)
        {
            _srArr[x] = new SpriteRenderer[_gridDensity];
            _gridDataArr[x] = new GridData[_gridDensity];
            for (var y = 0; y < _gridDensity; y++)
            {
                var go = Instantiate(_gridGo, mapNode);
                var tr = go.transform;
                tr.localPosition = _gridPivot + new Vector2((x + 0.5f) * _gridWidth, (y + 0.5f) * _gridWidth);
                _srArr[x][y] = tr.GetComponent<SpriteRenderer>();
                _gridDataArr[x][y] = new GridData(instanceId++, GridType.Path, new Vector2Int(x, y));
                Utils.ChangeTextByName(tr, "Text", $"(x:{x},y:{y})");
            }
        }

        SetWall();
    }

    private void SetWall()
    {
        _srArr[3][8].color = Color.black;
        _srArr[3][7].color = Color.black;
        _srArr[3][6].color = Color.black;
        _srArr[3][5].color = Color.black;
        _srArr[3][4].color = Color.black;
        _srArr[3][3].color = Color.black;
        _srArr[3][2].color = Color.black;
        _srArr[3][1].color = Color.black;
        
        _gridDataArr[3][8].GridType = GridType.Wall;
        _gridDataArr[3][7].GridType = GridType.Wall;
        _gridDataArr[3][6].GridType = GridType.Wall;
        _gridDataArr[3][5].GridType = GridType.Wall;
        _gridDataArr[3][4].GridType = GridType.Wall;
        _gridDataArr[3][3].GridType = GridType.Wall;
        _gridDataArr[3][2].GridType = GridType.Wall;
        _gridDataArr[3][1].GridType = GridType.Wall;
    }
    
    private void RenderStartTarget()
    {
        _srArr[_startGrid.x][_startGrid.y].color = Color.green;
        _srArr[_targetGrid.x][_targetGrid.y].color = Color.red;
    }
    
    private void RenderPath()
    {
        foreach (var pos in _closeSet.Select(item => item.GridPos).Where(pos => pos != _startGrid && pos != _targetGrid && pos != new Vector2Int(3, 8)))
        {
            _srArr[pos.x][pos.y].color = Color.gray;
        }

        foreach (var pos in _openSet.Select(item => item.GridPos).Where(pos => pos != _startGrid && pos != _targetGrid && pos != new Vector2Int(3, 8)))
        {
            _srArr[pos.x][pos.y].color = Color.yellow;
        }
    }

    private void RenderFinialPath()
    {
        for (var x = 0; x < _gridDensity; x++)
        {
            for (var y = 0; y < _gridDensity; y++)
            {
                _srArr[x][y].color = Color.white;
            }
        }
        
        var pathGrid = GetGridByPos(_targetGrid);
        while (pathGrid.ParentGrid != null)
        {
            var pos = pathGrid.ParentGrid.GridPos;
            _srArr[pos.x][pos.y].color = Color.grey;
            pathGrid = pathGrid.ParentGrid;
        }
        
        RenderStartTarget();
        SetWall();
    }
    
    private void AStarStart()
    {
        var data = GetGridByPos(_startGrid);
        var h = GetHeuristicVal(_startGrid);
        _openSet.Add(data);
        data.HVal = h;
        data.CalFVal();
    }

    private void AStarNext()
    {
        var crtGrid = FindAndRemoveMinInOpenSet();
        if (crtGrid.GridPos == _targetGrid)
        {
            Debug.Log("Path Find Finish");
            RenderFinialPath();
            _alreadyFind = true;
            return;
        }
        _closeSet.Add(crtGrid);
        TraversalNeighbor(crtGrid);
    }

    private void TraversalNeighbor(GridData gridData)
    {
        var pos = gridData.GridPos;
        var leftUp = GetGridByPos(pos + new Vector2Int(-1, 1));
        var up = GetGridByPos(pos + new Vector2Int(0, 1));
        var rightUp = GetGridByPos(pos + new Vector2Int(1, 1));
        var left = GetGridByPos(pos + new Vector2Int(-1, 0));
        var right = GetGridByPos(pos + new Vector2Int(1, 0));
        var leftDown = GetGridByPos(pos + new Vector2Int(-1, -1));
        var down = GetGridByPos(pos + new Vector2Int(0, -1));
        var rightDown = GetGridByPos(pos + new Vector2Int(1, -1));
        
        CheckNeighbor(gridData, leftUp);
        CheckNeighbor(gridData, up);
        CheckNeighbor(gridData, rightUp);
        CheckNeighbor(gridData, left);
        CheckNeighbor(gridData, right);
        CheckNeighbor(gridData, leftDown);
        CheckNeighbor(gridData, down);
        CheckNeighbor(gridData, rightDown);
    }
    
    private void CheckNeighbor(GridData crtGrid, GridData gridData)
    {
        if (gridData == null || gridData.GridType == GridType.Wall)
        {
            return;
        }
        
        if (_closeSet.Any(v => v == gridData))
        {
            return;
        }

        var tempGVal = crtGrid.GVal + gridData.Cost;
        if (CheckInOpen(gridData))
        {
            if (!(tempGVal < gridData.GVal)) return;
            gridData.ParentGrid = crtGrid;
            gridData.GVal = tempGVal;
            gridData.CalFVal();
        }
        else
        {
            _openSet.Add(gridData);
            gridData.ParentGrid = crtGrid;
            gridData.GVal = tempGVal;
            gridData.HVal = GetHeuristicVal(gridData.GridPos);
            gridData.CalFVal();
        }
    }
    
    private GridData FindAndRemoveMinInOpenSet()
    {
        var minFVal = float.MaxValue;
        var index = 0;
        GridData ret = null;
        for (var i = 0; i < _openSet.Count; i++)
        {
            var gd = _openSet[i];
            if (gd.FVal > minFVal) continue;
            minFVal = gd.FVal;
            ret = gd;
            index = i;
        }
        _openSet.RemoveAt(index);
        return ret;
    }

    private bool CheckInOpen(GridData gridData)
    {
        return _openSet.Any(item => item == gridData);
    }
    
    private GridData GetGridByPos(Vector2Int pos)
    {
        if (pos.x > _gridDensity || pos.y > _gridDensity || pos.x < 0 || pos.y < 0)
        {
            return null;
        }
        return _gridDataArr[pos.x][pos.y];
    }

    private float GetHeuristicVal(Vector2Int pos)
    {
        return (float)Math.Sqrt(Math.Pow(pos.x - _targetGrid.x, 2) + Math.Pow(pos.y - _targetGrid.y, 2));
    }
}