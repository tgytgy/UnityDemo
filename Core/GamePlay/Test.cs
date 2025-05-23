using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using Debug = UnityEngine.Debug;

public class Test : MonoBehaviour
{
    private void Awake()
    {
        
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.A))
        {
            Stopwatch stopwatch = new Stopwatch();

            // 开始计时
            stopwatch.Start();

            for (var i = 0; i < 10000; i++)
            {
                //Utils.GetNode(transform, "TargetNode");
                Utils.GetNode(transform, "TargetNode");  //45  65ms
                //var tr = GameObject.Find("TargetNode"); //1.8ms
            }
            
            stopwatch.Stop();
            TimeSpan elapsed = stopwatch.Elapsed;
            Debug.Log($"执行时间：{elapsed.TotalMilliseconds} 毫秒");
        }
    }
}
