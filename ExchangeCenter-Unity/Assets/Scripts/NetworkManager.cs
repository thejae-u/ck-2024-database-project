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
    Buy,
    Sell,
    Search,
    Response,
    Log,
    Error,
    Disconnect
}

public class NetworkData
{
    public readonly ENetworkDataType type;
    public readonly string data;
    
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
    private const string DNS = "jaeu.iptime.org";

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
        IPHostEntry ip = await Dns.GetHostEntryAsync(DNS);
        string ipToString = ip.AddressList[0].ToString();
        Debug.Log($"Connecting to server {ipToString}:{PORT}");
        
        try
        {
            _client = new TcpClient();

            await _client.ConnectAsync(ipToString, PORT);

            _stream = _client.GetStream();

            IPAddress serverIP = ((IPEndPoint)_client.Client.RemoteEndPoint).Address;
            Debug.Log($"Connected to server {serverIP}:{PORT}");
            IsRunning = true;
        }
        catch (SocketException e)
        {
            Debug.LogError($"Socket Exception : {e.Message}");
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit(0);
#endif
        }
        catch (Exception e)
        {
            Debug.LogError($"Exception : {e.Message}");
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit(0);
#endif
        }
    }

    public void EnqueueData(NetworkData data)
    {
        _sendQueue.Enqueue(data);
    }

    private async void SendDataToServer()
    {
        while (true)
        {
            if (_sendQueue.Count == 0)
            {
                if (!IsRunning)
                {
                    break;
                }
                
                continue;
            }    
            
            NetworkData data = _sendQueue.Dequeue();
            string sendData = $"{data.type.ToString()},{data.data}";
            
            try
            {
                byte[] buffer = System.Text.Encoding.UTF8.GetBytes(sendData);
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
        if (!IsRunning)
        {
            return;
        }
        
        _stream?.Close();
        _client?.Close();
        IsRunning = false;
    }
}
