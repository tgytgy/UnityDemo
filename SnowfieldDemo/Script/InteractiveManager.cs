using System;
using System.Collections.Generic;
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
    private Vector3 _interactivePos;
    private List<InteractiveTrigger> _triggers;
    private int _uvArrCsBufferStride;
    private ComputeBuffer _uvArrCsBuffer;
    private List<Vector3> _posArr;
    private int[] _offset;    
    
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
        _offset = new []{0, 0};
        //test
        AssetManager.LoadRes<Material>("CrtMat.mat").mainTexture = _crtRt;
        AssetManager.LoadRes<Material>("DisplayMat.mat").mainTexture = _displayRt;
        AssetManager.LoadRes<Material>("HistoryMat.mat").mainTexture = _historyRt;
    }

    private void Update()
    {
        //捕捉角色移动
        if (!_interactiveFollow)
        {
            return;
        }
        if (_triggers.Count == 0)
        {
            return;
        }
        //绘制当前帧
        PaintTriggers();
        //混合当前帧和历史脚印，如果有位移则将历史脚印位移,并把混合后的存到历史rt
        MixCrtAndHistory();
        //移动纹理位置
        UpdateInteractiveArea();
        _interactivePos = _interactiveFollow.GetCrtPos();
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
        var localPos = _crtUVOriginTr.position - _interactiveFollow.GetCrtPos();;
        var x = localPos.x / _worldSize;
        var y = localPos.z / _worldSize;
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
        var dPos = _interactiveFollow.GetCrtPos() - _interactivePos;
        var offset =new Vector2(-1 * dPos.x / _visualSize, -1 * dPos.z / _visualSize);
        offset *= _texSize;
        _offset[0] = Mathf.RoundToInt(offset.x);
        _offset[1] = Mathf.RoundToInt(offset.y);
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
        var crtPos = _interactiveFollow.GetCrtPos();
        var pos = new Vector2(crtPos.x, crtPos.z);
        _posArr.Clear();
        foreach (var trigger in _triggers)
        {
            var triggerPos = trigger.GetPosXZ();
            var offset = triggerPos - pos;
            if(Mathf.Abs(offset.x) > 100 || Mathf.Abs(offset.y) > 100)
            {
                continue;
            }

            var uv = (offset - new Vector2(-50, -50))/100;
            _posArr.Add(new Vector3(uv.x, uv.y, trigger.GetRecordRadius() / _visualSize));
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
    }
}
