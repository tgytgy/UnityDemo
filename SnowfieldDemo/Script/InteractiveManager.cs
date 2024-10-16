using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

public class InteractiveManager : SingtonMono <InteractiveManager>
{
    private RenderTexture _crtRt;
    private RenderTexture _displayRt;
    private RenderTexture _historyRt;
    private ComputeShader _cs;  //用来绘制的compute shader
    private ComputeShader _csCopy;  //用来复制脚印的compute shader
    private Material _testPlaneMat;
    private int _worldSize;     //大世界大小
    private int _visualSize;    //可视脚印区域大小
    private int _texSize;
    private float _uvRange;     //脚印UV范围
    private Transform _crtAreaTr;   //当前地形transform
    private Transform _crtUVOriginTr;   //当前地形UV原点
    private int _threadGroupsX;    //compute shader
    private int _threadGroupsY;    //compute shader
    private InteractiveFollow _interactiveFollow;
    private List<InteractiveTrigger> _triggers;
    private int _uvArrCsBufferStride;
    private ComputeBuffer _uvArrCsBuffer;
    private List<Vector3> _posArr;
    private List<Vector3> _prePosArr;
    private int[] _offset;
    private float _unit;
    private Vector3 _followPosCrt;
    private Vector3 _followPosPre;
    private bool _existChange;
    
    private readonly Color _color = Color.white;
    private static readonly int BufferLengthId = Shader.PropertyToID("BufferLength");
    private static readonly int ColorId = Shader.PropertyToID("_Color");
    private static readonly int ResultId = Shader.PropertyToID("Result");
    private static readonly int InteractiveTex = Shader.PropertyToID("_InteractiveTex");
    private static readonly int InteractiveBound = Shader.PropertyToID("_InteractiveBound");
    private static readonly int CrtTextureId = Shader.PropertyToID("CrtTexture");
    private static readonly int HistoryTextureId = Shader.PropertyToID("HistoryTexture");
    private static readonly int DisplayTextureId = Shader.PropertyToID("DisplayTexture");
    private static readonly int StepSizeId = Shader.PropertyToID("StepSize");
    private static readonly int UvArrBuffer = Shader.PropertyToID("UvArrBuffer");

    private void Awake()
    {
        Application.targetFrameRate = -1;
        _texSize = 1024;
        _crtRt = new RenderTexture(_texSize, _texSize, 0, RenderTextureFormat.ARGB32)
        {
            enableRandomWrite = true
        };
        _displayRt = new RenderTexture(_texSize, _texSize, 0, RenderTextureFormat.ARGB32)
        {
            enableRandomWrite = true
        };
        _historyRt = new RenderTexture(_texSize, _texSize, 0, RenderTextureFormat.ARGB32)
        {
            enableRandomWrite = true
        };
        
        _crtRt.filterMode = FilterMode.Bilinear;
        _displayRt.filterMode = FilterMode.Bilinear;
        _historyRt.filterMode = FilterMode.Bilinear;
        
        _crtRt.Create();
        _displayRt.Create();
        _historyRt.Create();
        _triggers = new List<InteractiveTrigger>();
        _visualSize = 100;
        _worldSize = 200;
        _uvRange = _visualSize / (float)_worldSize * 0.5f;
        _threadGroupsX = Mathf.CeilToInt(_crtRt.width / 8.0f);
        _threadGroupsY = Mathf.CeilToInt(_crtRt.height / 8.0f);
        _cs = AssetManager.LoadRes<ComputeShader>("WriteTexture.compute");
        _csCopy = AssetManager.LoadRes<ComputeShader>("CopyCurrentTexture.compute");
        _testPlaneMat = AssetManager.LoadRes<Material>("PlaneTest.mat");
        _testPlaneMat.SetTexture(InteractiveTex, _displayRt);
        _uvArrCsBufferStride = 12;
        _uvArrCsBuffer = new ComputeBuffer(1, _uvArrCsBufferStride);
        _posArr = new List<Vector3>();
        _prePosArr = new List<Vector3>();
        _offset = new []{0, 0};
        _followPosCrt = Vector3.zero;
        _followPosPre = Vector3.zero;
        _unit = (float)_visualSize / _texSize;
        _existChange = false;
        //test
        AssetManager.LoadRes<Material>("CrtMat.mat").mainTexture = _crtRt;
        AssetManager.LoadRes<Material>("DisplayMat.mat").mainTexture = _displayRt;
        AssetManager.LoadRes<Material>("HistoryMat.mat").mainTexture = _historyRt;
    }

    private void Update()
    {
        if (!_interactiveFollow)
        {
            return;
        }
        if (_triggers.Count == 0)
        {
            return;
        }

        _followPosCrt = _interactiveFollow.GetCrtPos();
        //绘制当前帧
        PaintTriggers();
        //混合当前帧和历史脚印，如果有位移则将历史脚印位移,并把混合后的存到历史rt
        MixCrtAndHistory();
        //移动纹理位置
        UpdateInteractiveArea();
        _followPosPre = _followPosCrt;
        ClearCrtTex();
    }
    
    /// <summary>
    /// 设置InteractiveFollow
    /// </summary>
    /// <param name="follow"></param>
    public void SetInteractiveFollow(InteractiveFollow follow)
    {
        _interactiveFollow = follow;
    }
    
    /// <summary>
    /// 脚印区域跟随角色
    /// </summary>
    private void UpdateInteractiveArea()
    {
        if (_offset[0] == 0 && _offset[1] == 0)
        {
            return;
        }
        var crtPixelLocation = GetPixelLocation(_interactiveFollow.GetCrtPos());
        var x = crtPixelLocation.x * _unit / _worldSize;
        var y = crtPixelLocation.y * _unit / _worldSize;

        _testPlaneMat.SetVector(InteractiveBound, new Vector4(x - _uvRange, y - _uvRange, x + _uvRange, y + _uvRange));
    }

    /// <summary>
    /// 设置当前地形transform
    /// </summary>
    /// <param name="tr">地形transform</param>
    public void SetCrtAreaTr(Transform tr)
    {
        _crtAreaTr = tr;
        _crtUVOriginTr = _crtAreaTr.Find("UVOrigin");
    }

    /// <summary>
    /// 加入trigger动画
    /// </summary>
    /// <param name="input"></param>
    public void AddTrigger(InteractiveTrigger input)
    {
        _triggers.Add(input);
    }
    
    /// <summary>
    /// 混合当前帧和历史rt并输出到displayRt,把混合后的输出到历史rt
    /// </summary>
    private void MixCrtAndHistory()
    {
        var offsetY = Mathf.Abs(_followPosCrt.y - _followPosPre.y);
        var crtPixelLocation = GetPixelLocation(_followPosCrt);
        var prePixelLocation = GetPixelLocation(_followPosPre);
        var ost = crtPixelLocation - prePixelLocation;
        if (ost == Vector2Int.zero && offsetY < _unit && !_existChange)
        {
            _offset[0] = 0;
            _offset[1] = 0;
            return;
        }
        _offset[0] = ost.x;
        _offset[1] = ost.y;
        _csCopy.SetTexture(0, CrtTextureId, _crtRt);
        _csCopy.SetTexture(0, HistoryTextureId, _historyRt);
        _csCopy.SetTexture(0, DisplayTextureId, _displayRt);
        _csCopy.SetInts(StepSizeId, _offset);
        _csCopy.Dispatch(0, _threadGroupsX, _threadGroupsY, 1);
        
        _csCopy.SetTexture(1, HistoryTextureId, _historyRt);
        _csCopy.SetTexture(1, DisplayTextureId, _displayRt);
        _csCopy.Dispatch(1, _threadGroupsX, _threadGroupsY, 1);
    }

    /// <summary>
    /// 绘制当前
    /// </summary>
    private void PaintTriggers()
    {
        var pos = new Vector2(_followPosCrt.x, _followPosCrt.z);
        var halfRange = _visualSize / 2f;
        _posArr.Clear();
        foreach (var trigger in _triggers)
        {
            var triggerPos = trigger.GetPosXZ();
            var offset = triggerPos - pos;
            if(Mathf.Abs(offset.x) > halfRange || Mathf.Abs(offset.y) > halfRange)
            {
                continue;
            }
            var uv = (new Vector2(halfRange, halfRange) - offset) / _visualSize;
            _posArr.Add(new Vector3(uv.x, uv.y, trigger.GetRecordRadius() / _visualSize));
        }

        _existChange = false;
        if (_posArr.Count == _prePosArr.Count)
        {
            for (var i = 0; i < _posArr.Count; i++)
            {
                if (!(Mathf.Abs((_posArr[i] - _prePosArr[i]).magnitude) < _unit)) continue;
                _existChange = true;
                break;
            }
        }
        else
        {
            _existChange = true;
        }

        if (!_existChange)
        {
            return;
        }
        var count = _posArr.Count;
        if (count != _uvArrCsBuffer.count)
        {
            _uvArrCsBuffer.Release();
            _uvArrCsBuffer = new ComputeBuffer(Mathf.Max(1, count), _uvArrCsBufferStride);
        }
        _uvArrCsBuffer.SetData(_posArr);
        _cs.SetBuffer(0, UvArrBuffer, _uvArrCsBuffer);
        _cs.SetInt(BufferLengthId, Mathf.Max(1, count));
        _cs.SetVector(ColorId, _color);
        _cs.SetTexture(0, ResultId, _crtRt);
        _cs.Dispatch(0, _threadGroupsX, _threadGroupsY, 1);
        _prePosArr.Clear();
        foreach (var posItem in _posArr)
        {
            _prePosArr.Add(posItem);
        }
    }
    
    private void ClearCrtTex()
    { 
        _cs.SetTexture(1, ResultId, _crtRt);
        _cs.Dispatch(1, _threadGroupsX, _threadGroupsY, 1);
    }
    
    private Vector2Int GetPixelLocation(Vector3 worldPos)
    {
        var localPos = _crtUVOriginTr.position - worldPos;
        var x = localPos.x;
        var y = localPos.z;
        if (x < 0 || y < 0 || x > _worldSize || y > _worldSize)
        {
            return new Vector2Int(0, 0);
        }
        
        return new Vector2Int(Mathf.CeilToInt(x/_unit), Mathf.CeilToInt(y/_unit));
    }
}
