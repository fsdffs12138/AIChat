using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class DeepSeek : LLM
{
    public DeepSeek()
    {
        // DeepSeek API 端点
        url = "api.deepinfra.com/v1/openai/chat/completions";
    }

    [Header("API Settings")]
    [SerializeField] private string apiKey;
    [SerializeField] private string modelName ;
    [SerializeField][TextArea] private string systemPrompt = "You are a helpful assistant";

    private readonly List<MessageData> messageHistory = new List<MessageData>();

    private void Start()
    {
        InitializeConversation();
    }

    private void InitializeConversation()
    {
        messageHistory.Clear();
        AddSystemMessage(systemPrompt);
    }

    public override void PostMsg(string userMessage, Action<string> callback)
    {
        base.PostMsg(userMessage, callback);
        StartCoroutine(SendRequest(userMessage, callback));
    }

    private IEnumerator SendRequest(string userMessage, Action<string> callback)
    {
        // 添加用户消息到历史
        AddUserMessage(userMessage);

        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            var requestData = new APIRequest
            {
                model = modelName,
                messages = messageHistory,
                temperature = 0.7f
            };

            string jsonPayload = JsonUtility.ToJson(requestData);
            byte[] payloadBytes = System.Text.Encoding.UTF8.GetBytes(jsonPayload);

            request.uploadHandler = new UploadHandlerRaw(payloadBytes);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", $"Bearer {apiKey}");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                var response = JsonUtility.FromJson<APIResponse>(request.downloadHandler.text);
                if (response?.choices != null && response.choices.Count > 0)
                {
                    string assistantReply = response.choices[0].message.content;
                    AddAssistantMessage(assistantReply);
                    callback?.Invoke(assistantReply);
                }
            }
            else
            {
                Debug.LogError($"API Error: {request.error}\nResponse: {request.downloadHandler.text}");
                callback?.Invoke("Error processing your request");
            }
        }
    }

    private void AddSystemMessage(string content)
    {
        messageHistory.Add(new MessageData("system", content));
    }

    private void AddUserMessage(string content)
    {
        messageHistory.Add(new MessageData("user", content));
    }

    private void AddAssistantMessage(string content)
    {
        messageHistory.Add(new MessageData("assistant", content));
    }

    [Serializable]
    private class APIRequest
    {
        public string model;
        public List<MessageData> messages;
        public float temperature;
    }

    [Serializable]
    private class APIResponse
    {
        public string id;
        public string created;
        public string model;
        public List<Choice> choices;
    }

    [Serializable]
    private class Choice
    {
        public MessageData message;
        public string finish_reason;
        public int index;
    }

    [Serializable]
    private class MessageData
    {
        public string role;
        public string content;

        public MessageData(string role, string content)
        {
            this.role = role;
            this.content = content;
        }
    }
}