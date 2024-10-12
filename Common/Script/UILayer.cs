using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UILayer : MonoBehaviour
{
    private float fps;
    private float timeCounter = 0.0f;
    private float refreshTime = 0.5f;
    private int frameCounter = 0;
    public ExampleClass sample;
    public TMP_Text fpsText;
    public TMP_Text sizeText;
    public TMP_Text countText;
    public Button btnAreaAdd;
    public Button btnAreaSub;
    public Button btnDenAdd;
    public Button btnDenSub;
    public Button btnRefresh;
    
    
    private void Awake()
    {
        //Application.targetFrameRate = 60;
        btnAreaAdd.onClick.AddListener(() =>
        {
            sample.gameObject.SetActive(false);
            sample.tileSize += 5;
            sample.gameObject.SetActive(true);
            sizeText.text = "Size: " + sample.tileSize;
        });
        btnAreaSub.onClick.AddListener(() =>
        {
            sample.gameObject.SetActive(false);
            sample.tileSize -= 5;
            sample.gameObject.SetActive(true);
            sizeText.text = "Size: " + sample.tileSize;
        });
        btnDenAdd.onClick.AddListener(() =>
        {
            sample.gameObject.SetActive(false);
            sample.density += 50;
            sample.gameObject.SetActive(true);
            countText.text = "Count: " + sample.density * sample.density;
        });
        btnDenSub.onClick.AddListener(() =>
        {
            sample.gameObject.SetActive(false);
            sample.density -= 50;
            sample.gameObject.SetActive(true);
            countText.text = "Count: " + sample.density * sample.density;
        });
        btnRefresh.onClick.AddListener(() =>
        {
            sizeText.text = "Size: " + sample.tileSize;
            countText.text = "Count: " + sample.density * sample.density;
        });
    }

    private void Update()
    {
        // 累积时间
        timeCounter += Time.deltaTime;
        frameCounter++;

        // 如果时间超过1秒，更新帧率
        if (timeCounter >= refreshTime)
        {
            fps = frameCounter / timeCounter;

            fpsText.text = "FPS: "+Mathf.RoundToInt(fps);
            // 重置计数器
            timeCounter = 0.0f;
            frameCounter = 0;
        }
    }
    
}
