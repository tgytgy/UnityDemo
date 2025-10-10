using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Google.Protobuf;      // 需要 Google.Protobuf
using UnityEngine;
using Echo;                 // 生成的 Chat.cs 的命名空间（来自 option csharp_namespace = "Echo"）

public class TcpProtoClient : MonoBehaviour
{
    [Header("服务器地址与端口")]
    public string host = "127.0.0.1";
    public int port = 9000;

    [Header("启动自动连接并测试一次")]
    public bool autoTestOnStart = true;

    private TcpClient _client;
    private NetworkStream _stream;
    private CancellationTokenSource _cts;

    private async void Start()
    {
        if (autoTestOnStart)
        {
            await ConnectAndTestOnce();
        }
    }

    public async Task ConnectAndTestOnce()
    {
        _cts = new CancellationTokenSource();
        try
        {
            _client = new TcpClient { NoDelay = true };
            Debug.Log($"[ProtoTCP] Connecting {host}:{port} …");
            await _client.ConnectAsync(host, port);
            _stream = _client.GetStream();
            Debug.Log("[ProtoTCP] Connected.");

            // 1) 组装 Chat
            var chat = new Chat
            {
                Seq = 1,
                Text = $"Hello from Unity (proto)! {DateTime.Now:HH:mm:ss.fff}"
            };
            byte[] payload = chat.ToByteArray();

            // 2) 写入长度前缀(大端) + payload
            var lenBuf = new byte[4];
            WriteBE32(lenBuf, (uint)payload.Length);
            await _stream.WriteAsync(lenBuf, 0, 4, _cts.Token);
            await _stream.WriteAsync(payload, 0, payload.Length, _cts.Token);
            await _stream.FlushAsync(_cts.Token);
            Debug.Log($"[ProtoTCP] Sent Chat: seq={chat.Seq}, text={chat.Text}");

            // 3) 读取回显（长度前缀 + payload）
            byte[] reply = await ReadFrameAsync(_stream, _cts.Token);
            var back = Chat.Parser.ParseFrom(reply);
            Debug.Log($"[ProtoTCP] Received Chat: seq={back.Seq}, text={back.Text}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[ProtoTCP] Error: {ex.Message}");
        }
        finally
        {
            _stream?.Close();
            _client?.Close();
        }
    }

    static async Task<byte[]> ReadExactAsync(Stream s, int n, CancellationToken token)
    {
        byte[] buf = new byte[n];
        int off = 0;
        while (off < n)
        {
            int r = await s.ReadAsync(buf, off, n - off, token);
            if (r <= 0) throw new IOException("Remote closed");
            off += r;
        }
        return buf;
    }

    static async Task<byte[]> ReadFrameAsync(Stream s, CancellationToken token)
    {
        byte[] lenBuf = await ReadExactAsync(s, 4, token);
        uint len = ReadBE32(lenBuf);
        if (len == 0 || len > 10 * 1024 * 1024) throw new IOException($"Invalid frame size: {len}");
        return await ReadExactAsync(s, (int)len, token);
    }

    static void WriteBE32(byte[] buf, uint v)
    {
        buf[0] = (byte)(v >> 24);
        buf[1] = (byte)(v >> 16);
        buf[2] = (byte)(v >> 8);
        buf[3] = (byte)(v);
    }

    static uint ReadBE32(byte[] buf)
    {
        return ((uint)buf[0] << 24) | ((uint)buf[1] << 16) | ((uint)buf[2] << 8) | buf[3];
    }
}
