using System;
using UnityEngine;
using TMPro;

public class ChatClient : MonoBehaviour
{
    [SerializeField] private TMP_InputField m_inputField;

    public event Action<string> OnMessageSubmited;

    private void OnEnable()
    {
        m_inputField.onSubmit.AddListener(SubmitMessage);
    }

    private void OnDisable()
    {
        m_inputField.onSubmit.RemoveListener(SubmitMessage);
    }

    public void SubmitMessage(string text)
    {
        if (string.IsNullOrEmpty(text)) return;
        OnMessageSubmited?.Invoke(text);
        m_inputField.text = string.Empty;
    }
}
