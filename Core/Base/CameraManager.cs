using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering.Universal;

public class CameraManager : SingletonMono<CameraManager>
{
    private Camera _mainCamera;
    private Camera _uiCamera;

    public void InitGlobalOnce()
    {
        InitGlobalUICamera();
        InitEventSystem();
    }

    private void InitGlobalUICamera()
    {
        var go = new GameObject("UICamera");
        _mainCamera = go.AddComponent<Camera>();
        _uiCamera = _mainCamera;
        _uiCamera.GetUniversalAdditionalCameraData().renderType = CameraRenderType.Base;
        _uiCamera.orthographic = true;
        _uiCamera.orthographicSize = 5f;
        DontDestroyOnLoad(go);
    }

    private void InitEventSystem()
    {
        var go = new GameObject("EventSystem");
        go.AddComponent<EventSystem>();
        go.AddComponent<InputSystemUIInputModule>();
        DontDestroyOnLoad(go);
    }
    
    public Camera GetUICamera()
    {
        return _uiCamera;
    }
}