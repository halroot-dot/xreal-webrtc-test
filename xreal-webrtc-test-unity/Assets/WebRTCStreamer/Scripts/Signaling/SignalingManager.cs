using UnityEngine;
using WebSocketSharp;
using System;
using System.Threading;

[Serializable]
public class SignalingMessage
{
    public string type;
    public DescData offer;
    public DescData answer;
    public CandidateData candidate;
}

[Serializable]
public class DescData
{
    public string type;
    public string sdp;
}

[Serializable]
public class CandidateData
{
    public string candidate;
    public string sdpMid;
    public int sdpMLineIndex;
}


public class SignalingManager
{
    private WebSocket webSocket;
    private readonly Action<SignalingMessage> onMessageReceived;
    private readonly SynchronizationContext context;

    public SignalingManager(Action<SignalingMessage> onMessageReceived)
    {
        this.onMessageReceived = onMessageReceived;
        this.context = SynchronizationContext.Current;
    }

    public void Connect(string serverUrl)
    {
        webSocket = new WebSocket(serverUrl);

        webSocket.OnOpen += (sender, e) =>
        {
            XrealLogger.Log("WebSocket Connected");
        };

        webSocket.OnMessage += (sender, e) =>
        {
            XrealLogger.Log($"Received message: {e.Data}");
            try
            {
                var message = JsonUtility.FromJson<SignalingMessage>(e.Data);
                context.Post(_ => onMessageReceived(message), null);
            }
            catch (Exception ex)
            {
                XrealLogger.LogError($"Failed to parse message: {ex.Message}");
            }
        };

        webSocket.OnError += (sender, e) =>
        {
            XrealLogger.LogError($"WebSocket Error: {e.Message}");
        };

        webSocket.Connect();
    }

    public void SendMessage(SignalingMessage message)
    {
        if (webSocket?.ReadyState != WebSocketState.Open)
        {
            XrealLogger.LogError($"WebSocket not ready: {webSocket?.ReadyState}");
            return;
        }

        try
        {
            string json = JsonUtility.ToJson(message);
            XrealLogger.Log($"Sending: {json}");
            webSocket.Send(json);
        }
        catch (Exception e)
        {
            XrealLogger.LogError($"Failed to send message: {e.Message}");
        }
    }

    public void Disconnect()
    {
        if (webSocket != null)
        {
            webSocket.Close();
            webSocket = null;
        }
    }
}