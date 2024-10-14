using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class Test : MonoBehaviour
{
    [Range(0, 511)]
    public int x;
    [Range(0, 511)]
    public int y;
    public Texture historyRt;
    
    
    private RenderTexture _crtRt;
    private RenderTexture _displayRt;
    private ComputeShader _cs;  //用来绘制的compute shader
    private ComputeShader _csCopy;  //用来复制脚印的compute shader
    private Material _testPlaneMat;
    private int _worldSize;     //大世界大小
    private int _visualSize;    //可视脚印区域大小
    private int _texSize;
    private Transform _crtAreaTr;   //当前地形transform
    private Transform _crtUVOriginTr;   //当前地形UV原点
    private int _threadGroupsX;    //compute shader
    private int _threadGroupsY;    //compute shader
    private InteractiveFollow _interactiveFollow;
    private Vector3 _interactivePos;
    private int _uvArrCsBufferStride;
    private bool enableUpdate = true;
    private int[] _offset;
    
    
    private static readonly int InteractiveTex = Shader.PropertyToID("_InteractiveTex");
    private static readonly int CrtTextureId = Shader.PropertyToID("CrtTexture");
    private static readonly int HistoryTextureId = Shader.PropertyToID("HistoryTexture");
    private static readonly int DisplayTextureId = Shader.PropertyToID("DisplayTexture");
    private static readonly int StepSizeId = Shader.PropertyToID("StepSize");

    private void Awake()
    {
        Application.targetFrameRate = -1;
        _texSize = 512;
        _crtRt = new RenderTexture(_texSize, _texSize, 0, RenderTextureFormat.ARGB32)
        {
            enableRandomWrite = true
        };
        _displayRt = new RenderTexture(_texSize, _texSize, 0, RenderTextureFormat.ARGB32)
        {
            enableRandomWrite = true
        };

        
        _crtRt.filterMode = FilterMode.Bilinear;
        _displayRt.filterMode = FilterMode.Bilinear;
        
        _crtRt.Create();
        _displayRt.Create();
        _visualSize = 100;
        _worldSize = 200;
        _threadGroupsX = Mathf.CeilToInt(_crtRt.width / 8.0f);
        _threadGroupsY = Mathf.CeilToInt(_crtRt.height / 8.0f);
        _cs = AssetManager.LoadRes<ComputeShader>("WriteTexture.compute");
        _csCopy = AssetManager.LoadRes<ComputeShader>("CopyCurrentTexture.compute");
        _testPlaneMat = AssetManager.LoadRes<Material>("PlaneTest.mat");
        _testPlaneMat.SetTexture(InteractiveTex, _displayRt);
        _uvArrCsBufferStride = 12;
        _offset = new []{0, 0};
        //test
        AssetManager.LoadRes<Material>("CrtMat.mat").mainTexture = _crtRt;
        AssetManager.LoadRes<Material>("DisplayMat.mat").mainTexture = _displayRt;
        AssetManager.LoadRes<Material>("HistoryMat.mat").mainTexture = historyRt;
    }

    private void Update()
    {
        _offset[0] = x;
        _offset[1] = y;
        _csCopy.SetTexture(0, CrtTextureId, _crtRt);
        _csCopy.SetTexture(0, HistoryTextureId, historyRt);
        _csCopy.SetTexture(0, DisplayTextureId, _displayRt);
        _csCopy.SetInts(StepSizeId, _offset);
        _csCopy.Dispatch(0, _threadGroupsX, _threadGroupsY, 1);
    }
}
