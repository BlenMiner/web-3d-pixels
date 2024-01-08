using System;
using System.Collections.Generic;
using ByteStream.Mananged;

public interface ISendData
{
    void Send(byte[] data, int offset, int size);
}

public interface IReceiveData
{
    event Action<byte[], int, int> OnReceivedData;
}

public class SessionBroadcaster
{
    readonly Dictionary<Type, int> m_messageIds = new ();
    readonly Dictionary<int, Type> m_idToType = new ();
    readonly Dictionary<int, List<Action<object>>> m_callbacks = new ();
    
    readonly ISendData m_sender;
    readonly IReceiveData m_receiver;
    
    readonly ManagedStream m_stream = new ();

    public SessionBroadcaster(ISendData sender, IReceiveData receiver)
    {
        m_sender = sender;
        m_receiver = receiver;
        
        m_receiver.OnReceivedData += OnReceivedData;
    }
    
    private int GetHash(Type type)
    {
        if (!m_messageIds.TryGetValue(type, out var messageId))
        {
            messageId = Hasher.GetFNV1aHashCode(type.FullName!);
            
            m_messageIds.Add(type, messageId);
            m_idToType.Add(messageId, type);
        }

        return messageId;
    }
    
    public void Send<T>(T message) where T : struct, INetworked
    {
        var messageId = GetHash(typeof(T));

        m_stream.ResetWrite();
        m_stream.Write(messageId);
        message.Serialize(m_stream);

        m_sender.Send(m_stream.Buffer, 0, m_stream.Offset);
    }
    
    public void Register<T>(Action<T> callback) where T : struct, INetworked
    {
        var messageId = GetHash(typeof(T));

        if (!m_callbacks.TryGetValue(messageId, out var callbacks))
        {
            callbacks = new List<Action<object>>();
            m_callbacks.Add(messageId, callbacks);
        }
        
        callbacks.Add(x => callback((T)x));
    }
    
    void OnReceivedData(byte[] buffer, int offset, int size)
    {
        m_stream.ResetRead(buffer, offset, size);
        
        int messageId = m_stream.Read<int>();
        
        if (!m_idToType.TryGetValue(messageId, out var type))
        {
            Logger.LogWarning($"Received messsage id {messageId}, but no type is registered for it.");
            return;
        }
        
        if (!m_callbacks.TryGetValue(messageId, out var callbacks))
        {
            Logger.LogWarning($"Received messsage id {messageId}, but no callback is registered for it.");
            return;
        }

        var result = Activator.CreateInstance(type) as INetworked;
        
        result!.Serialize(m_stream);

        for (var i = 0; i < callbacks.Count; i++)
            callbacks[i](result);
    }
}
