using UnityEngine;
using UnityEngine.Rendering.Universal;

public class CameraManager : SingletonMono<CameraManager>
{
    private Camera _mainCamera;
    private Camera _uiCamera;

    public void InitGlobalUICamera()
    {
        var go = new GameObject("UICamera");
        _mainCamera = go.AddComponent<Camera>();
        _uiCamera = _mainCamera;
        _uiCamera.GetUniversalAdditionalCameraData().renderType = CameraRenderType.Base;
        _uiCamera.orthographic =  true;
        _uiCamera.orthographicSize = 5f;
        DontDestroyOnLoad(go);
    }

    public Camera GetUICamera()
    {
        return _uiCamera;
    }
}