using UnityEngine;

public class GameBootstrapper : MonoBehaviour
{
    [SerializeField] private NetworkClient m_client;
    [SerializeField] private ChatClient m_chatClient;

    private void OnEnable()
    {
        if (m_chatClient)
            m_chatClient.OnMessageSubmited += OnMessageSubmited;
        else
        {
            Debug.LogWarning("ChatClient is not set");
        }
    }
    
    private void OnDisable()
    {
        if (m_chatClient)
            m_chatClient.OnMessageSubmited -= OnMessageSubmited;
    }

    private void OnMessageSubmited(string message)
    {
        m_client.Send(message);
    }

    private void Start()
    {
        m_client.Connect();
    }

    private void OnDestroy()
    {
        m_client.Disconnect();
    }
}
