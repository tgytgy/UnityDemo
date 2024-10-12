using System;
using UnityEngine;
using Random = UnityEngine.Random;

public class ExampleClass : MonoBehaviour
{
    public Material material;
    public Mesh mesh;
    private Vector3 centerPos;
    public float tileSize;
    public int density;
    public Camera mainCamera;
    
    GraphicsBuffer commandBuf;
    GraphicsBuffer instanceDataBuf;
    GraphicsBuffer.IndirectDrawIndexedArgs[] commandData;
    const int commandCount = 1;
    private int instanceCount = 1; // 实例数量
    private Vector2 _firstPos;
    private Vector2 _interval;
    private Vector2 _planeSize;
    
    void OnEnable()
    {
        centerPos = mainCamera.transform.position;
        _planeSize = new Vector2(tileSize, tileSize);
        _interval = new Vector2(_planeSize.x / density, _planeSize.y / density);
        _firstPos = new Vector2(-1 * _planeSize.x / 2 + _interval.x / 2, -1 * _planeSize.y / 2 + _interval.y / 2);
        
        instanceCount = density * density;
        commandBuf = new GraphicsBuffer(GraphicsBuffer.Target.IndirectArguments, commandCount, GraphicsBuffer.IndirectDrawIndexedArgs.size);
        commandData = new GraphicsBuffer.IndirectDrawIndexedArgs[commandCount];

        // 创建用于存储实例化数据的 GraphicsBuffer
        instanceDataBuf = new GraphicsBuffer(GraphicsBuffer.Target.Structured, instanceCount, sizeof(float) * 4 * 4); // 每个实例需要 4x4 矩阵

        Matrix4x4[] instanceMatrices = new Matrix4x4[instanceCount];
        for (int i = 0; i < instanceCount; i++)
        {
            // 随机位置
            Vector3 randomPosition = new Vector3(
                Random.Range(centerPos.x - tileSize, centerPos.x + tileSize),
                0,
                Random.Range(-24.2f, -24.2f + tileSize * 2)
            );

            // 随机旋转
            float randomYRotation = Random.Range(0f, 360f);
            //float randomYRotation = 180;
            Quaternion randomRotation = Quaternion.Euler(0, randomYRotation, 0);

            // 随机缩放
            Vector3 randomScale = new Vector3(
                0.6f,
                1,
                1
            );
            //Vector3 randomScale = Vector3.one;

            // 组合平移、旋转和缩放信息到一个 4x4 矩阵中
            Matrix4x4 transformMatrix = Matrix4x4.TRS(randomPosition, randomRotation, randomScale);
            instanceMatrices[i] = transformMatrix;
        }
        
        // 将实例化数据传输到 GPU
        instanceDataBuf.SetData(instanceMatrices);
        material.SetBuffer("instanceDataBuf", instanceDataBuf);

        commandData[0].indexCountPerInstance = mesh.GetIndexCount(0);
        commandData[0].instanceCount = (uint)instanceCount;
        commandBuf.SetData(commandData);
    }

    void OnDestroy()
    {
        commandBuf?.Release();
        instanceDataBuf?.Release();
        commandBuf = null;
        instanceDataBuf = null;
    }

    void Update()
    {
        RenderParams rp = new RenderParams(material);
        rp.worldBounds = new Bounds(Vector3.zero, 10000 * Vector3.one); // 使用较紧的范围进行视锥体剔除

        // 一次性渲染所有实例
        Graphics.RenderMeshIndirect(rp, mesh, commandBuf, commandCount);
    }
    
    private Vector2 GetRandomOffset()
    {
        var rx = Random.Range(-1 * _interval.x / 2, _interval.x / 2);
        var ry = Random.Range(-1 * _interval.y / 2, _interval.y / 2);
        return new Vector2(rx, ry);
    }
}
