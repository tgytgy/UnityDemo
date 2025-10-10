using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public class TCPManager : MonoBehaviour
{
    [Header("服务器地址与端口")]
    public string host = "127.0.0.1"; // 如果在同一台电脑上运行，保持 127.0.0.1
    public int port = 9000;

    [Header("启动时自动连接并发送一条测试消息")]
    public bool autoTestOnStart = true;

    private TcpClient _client;
    private NetworkStream _stream;
    private CancellationTokenSource _cts;

    private async void Start()
    {
        if (autoTestOnStart)
        {
            await ConnectAndTest();
        }
    }

    public async Task ConnectAndTest()
    {
        _cts = new CancellationTokenSource();
        try
        {
            Debug.Log($"[TCP] 尝试连接 {host}:{port} …");
            _client = new TcpClient();
            // 建议设置 NoDelay 降低 Nagle 延迟（对游戏常见）
            _client.NoDelay = true;

            // 尝试连接（3 秒超时）
            using (var ctsConnect = new CancellationTokenSource(TimeSpan.FromSeconds(3)))
            using (ctsConnect.Token.Register(() => _client.Close()))
            {
                await _client.ConnectAsync(host, port);
            }

            _stream = _client.GetStream();
            Debug.Log("[TCP] 已连接。");

            // 发送一条测试文本（UTF-8）
            string msg = $"Hello from Unity! 时间戳: {DateTime.Now:HH:mm:ss.fff}";
            byte[] data = Encoding.UTF8.GetBytes(msg);
            await _stream.WriteAsync(data, 0, data.Length, _cts.Token);
            await _stream.FlushAsync(_cts.Token);
            Debug.Log($"[TCP] 已发送: {msg}");

            // 接收服务器回显（读取到暂时无更多数据为止）
            string reply = await ReadAvailableAsync(_stream, _cts.Token, maxWaitMs: 800);
            if (string.IsNullOrEmpty(reply))
            {
                Debug.LogWarning("[TCP] 未收到服务器回显（可能网络延迟或被系统防火墙拦截）。");
            }
            else
            {
                Debug.Log($"[TCP] 收到: {reply}");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[TCP] 连接或通信异常: {ex.Message}");
        }
    }

    /// <summary>
    /// 持续读取，直到一定时间内没有更多数据可读为止。
    /// </summary>
    private async Task<string> ReadAvailableAsync(NetworkStream stream, CancellationToken token, int maxWaitMs = 800)
    {
        var ms = new MemoryStream();
        var buffer = new byte[1024];
        int idleWait = 0;

        while (!token.IsCancellationRequested)
        {
            if (stream.DataAvailable)
            {
                int n = await stream.ReadAsync(buffer, 0, buffer.Length, token);
                if (n <= 0) break;
                ms.Write(buffer, 0, n);
                idleWait = 0; // 读到数据，重置等待
            }
            else
            {
                // 没有数据，稍等一下再看（直到累计等待超过 maxWaitMs）
                await Task.Delay(50, token);
                idleWait += 50;
                if (idleWait >= maxWaitMs) break;
            }
        }

        return Encoding.UTF8.GetString(ms.ToArray());
    }

    private void OnApplicationQuit()
    {
        try
        {
            _cts?.Cancel();
            _stream?.Close();
            _client?.Close();
        }
        catch { }
    }
}
