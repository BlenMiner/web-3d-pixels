using System;
using UnityEngine;

public sealed class Broadcaster : MonoBehaviour, ISendData, IReceiveData
{
    static Broadcaster s_instance;
    
    [SerializeField] NetworkClient m_client;
    
    private SessionBroadcaster m_broadcaster;
    
    void Awake()
    {
        s_instance = this;
        
        m_broadcaster = new SessionBroadcaster(this, this);
    }

    public event Action<byte[], int, int> OnReceivedData;

    private void OnEnable()
    {
        m_client.OnDataReceived += OnDataReceived;
    }

    private void OnDisable()
    {
        m_client.OnDataReceived -= OnDataReceived;
    }

    private void OnDataReceived(ArraySegment<byte> obj)
    {
        OnReceivedData?.Invoke(obj.Array, obj.Offset, obj.Count);
    }
    
    public void Send(byte[] data, int offset, int size)
    {
        m_client.Send(new ArraySegment<byte>(data, offset, size));
    }

    public static void Send<T>(T message) where T : struct, INetworked
    {
        s_instance.m_broadcaster.Send(message);
    }
    
    public static void Register<T>(Action<T> callback) where T : struct, INetworked
    {
        s_instance.m_broadcaster.Register(callback);
    }
}
