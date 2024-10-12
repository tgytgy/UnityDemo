using System;
using UnityEngine;
using Random = UnityEngine.Random;

public class TileGenerate : MonoBehaviour
{
    public Material material;
    public Mesh mesh;

    GraphicsBuffer commandBuf;
    GraphicsBuffer.IndirectDrawIndexedArgs[] commandData;
    const int commandCount = 2;
    
    public int density;
    public GameObject obj;
    
    private Transform _plane;
    private Vector2 _firstPos;
    private Vector2 _interval;
    private Vector2 _planeSize;

    private void Start()
    {
        commandBuf = new GraphicsBuffer(GraphicsBuffer.Target.IndirectArguments, commandCount, GraphicsBuffer.IndirectDrawIndexedArgs.size);
        commandData = new GraphicsBuffer.IndirectDrawIndexedArgs[commandCount];
    }

    private void Awake()
    {
        _plane = transform.Find("Plane");
        _planeSize = new Vector2(10, 10);
        _interval = new Vector2(_planeSize.x / density, _planeSize.y / density);
        _firstPos = new Vector2(-1 * _planeSize.x / 2 + _interval.x / 2, -1 * _planeSize.y / 2 + _interval.y / 2);
        Generate();
    }

    private void Generate()
    {
        var tempPos = Vector3.zero;
        var tempRotation = Vector3.zero;
        for (var i = 0; i < density; i++)
        {
            for (var j = 0; j < density; j++)
            {
                var randomOffset = GetRandomOffset();
                var randomRotation = GetRandomRotation();
                var item = Instantiate(obj, _plane).transform;
                tempPos.x = _firstPos.x + j * _interval.x + randomOffset.x;
                tempPos.z = _firstPos.y + i * _interval.y + randomOffset.y;
                tempRotation.y = randomRotation;
                item.localPosition = tempPos;
                item.localEulerAngles = tempRotation;
                BendGrass(item);
            }
        }
    }

    private Vector2 GetRandomOffset()
    {
        var rx = Random.Range(-1 * _interval.x / 2, _interval.x / 2);
        var ry = Random.Range(-1 * _interval.y / 2, _interval.y / 2);
        return new Vector2(rx, ry);
    }
    
    private float GetRandomRotation()
    {
        return Random.Range(0, 360);
    }

    private void BendGrass(Transform tr)
    {
        
    }
}
