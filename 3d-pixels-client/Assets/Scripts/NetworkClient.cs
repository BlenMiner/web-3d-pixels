using System;
using System.Text;
using UnityEngine;
using JamesFrowen.SimpleWeb;

public class NetworkClient : MonoBehaviour
{
    public bool IsConnected { get; private set; }
    private SimpleWebClient m_client;
    
    void Awake()
    {
        var tcpConfig = new TcpConfig(noDelay: false, sendTimeout: 5000, receiveTimeout: 20000);
        m_client = SimpleWebClient.Create(ushort.MaxValue, 5000, tcpConfig);
        
        m_client.onConnect += OnConnected;
        m_client.onDisconnect += OnDisconnected;
        m_client.onData += OnReceivedData;
        m_client.onError += OnError;
    }

    private void OnConnected()
    {
        Debug.Log("Connected");
        IsConnected = true;
    }
    
    private void OnDisconnected()
    {
        Debug.Log("Disconnected");
        IsConnected = false;
    }

    static void OnError(Exception exception)
    {
        Debug.LogException(exception);
    }

    void OnReceivedData(ArraySegment<byte> data)
    {
        if (data.Array == null) return;
        
        string message = Encoding.UTF8.GetString(data.Array, data.Offset, data.Count);

        Debug.Log($"Data from Server: {message}");
    }

    public void Connect(bool wss = false, string host = "localhost", int port = 8080)
    {
        var builder = new UriBuilder
        {
            Scheme = wss ? "wss" : "ws",
            Host = host,
            Port = port
        };

        var uri = builder.Uri;
        
        Debug.Log($"Connecting to {uri}");
        
        m_client.Connect(uri);
    }

    public void Send(string message)
    {
        var bytes = Encoding.UTF8.GetBytes(message);
        var segment = new ArraySegment<byte>(bytes);
        m_client.Send(segment);
    }
    
    public void Send(byte[] bytes)
    {
        var segment = new ArraySegment<byte>(bytes);
        m_client.Send(segment);
    }
    
    public void Send(ArraySegment<byte> segment)
    {
        m_client.Send(segment);
    }

    private void Update()
    {
        m_client.ProcessMessageQueue();
    }

    private void OnDestroy()
    {
        m_client.onConnect -= OnConnected;
        m_client.onDisconnect -= OnDisconnected;
        m_client.onData -= OnReceivedData;
        m_client.onError -= OnError;
    }

    public void Disconnect()
    {
        m_client.Disconnect();
    }
}
