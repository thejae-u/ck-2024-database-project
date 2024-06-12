using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

public enum ENetworkDataType
{
    None,
    Login,
    Register,
    Request,
    Response,
    Log,
    Error,
    Disconnect
}

public class NetworkData
{
    public ENetworkDataType type;
    public string data;
    
    public NetworkData(ENetworkDataType type, string data)
    {
        this.type = type;
        this.data = data;
    }
}

public class NetworkManager : Singleton<NetworkManager> 
{
    private bool IsRunning { get; set; }
    private const int PORT = 56000;
    private const string IP = "127.0.0.1";

    private readonly Queue<NetworkData> _sendQueue = new();

    private TcpClient _client;
    private NetworkStream _stream;
    
    private async void Start()
    {
        await ConnectToServer();
        await Task.Run(SendDataToServer);
    }

    private async Task ConnectToServer()
    {
        try
        {
            _client = new TcpClient();

            await _client.ConnectAsync(IP, PORT);

            _stream = _client.GetStream();

            Debug.Log("Connected to server");
        }
        catch (SocketException e)
        {
            Application.Quit(-999);
            Debug.LogError($"Socket Exception : {e.Message}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Exception : {e.Message}");
        }
    }

    public void EnqueueData(NetworkData data)
    {
        _sendQueue.Enqueue(data);
    }

    private async void SendDataToServer()
    {
        IsRunning = true;
        int runningCount = 0;
        while (true)
        {
            Debug.Log($"runningCount : {++runningCount} / IsRunning : {IsRunning}");
            if (_sendQueue.Count == 0)
            {
                if (!IsRunning)
                {
                    break;
                }
                
                continue;
            }    
            
            NetworkData data = _sendQueue.Dequeue();
            try
            {
                byte[] buffer = System.Text.Encoding.ASCII.GetBytes(JsonUtility.ToJson(data));
                await _stream.WriteAsync(buffer, 0, buffer.Length);
            }
            catch (Exception e)
            {
                Debug.LogError($"Exception : {e.Message}");
                break;
            }
        }
    }

    private void OnApplicationQuit()
    {
        _stream?.Close();
        _client?.Close();
        IsRunning = false;
    }
}
