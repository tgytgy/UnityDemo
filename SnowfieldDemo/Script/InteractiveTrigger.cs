using UnityEngine;

public class InteractiveTrigger : MonoBehaviour
{
    public float detectionRadius;
    public float snowSurfaceHeight;
    private float _recordRadius;
    private float _remapVal;

    private void Start()
    {
        InteractiveManager.Instance.AddTrigger(this);
    }

    private void CalRecordRadius()
    {
        _recordRadius = Mathf.Sqrt(Mathf.Pow(detectionRadius, 2) - Vector3.Distance(new Vector3(0, transform.position.y, 0), new Vector3(0, snowSurfaceHeight, 0)));
    }
    
    public float GetRecordRadius()
    {
        return _recordRadius;
    }
    
    public Vector2 GetPosXZ()
    {
        var pos = transform.position;
        return new Vector2(pos.x, pos.z);
    }
    
    private void Update()
    {
        CalRecordRadius();
    }
}
