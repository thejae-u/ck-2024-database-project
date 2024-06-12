using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

public class NetworkManager : Singleton<NetworkManager> 
{
    private const int PORT = 56000;
    private const string IP = "127.0.0.1";

    private TcpClient _client;
    private NetworkStream _stream;
    
    private async void Start()
    {
        await ConnectToServer();
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
            Debug.LogError($"Socket Exception : {e.Message}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Exception : {e.Message}");
        }
    }

    private void OnApplicationQuit()
    {
        _stream?.Close();
        _client?.Close();
    }
}
