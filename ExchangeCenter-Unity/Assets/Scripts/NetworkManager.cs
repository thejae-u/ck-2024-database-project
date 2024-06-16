using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;
using UnityEngine;

using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

public class NetworkManager : Singleton<NetworkManager>
{
    public string CurrentUser { get; private set; }
    public bool IsRunning { get; private set; }
    private bool IsServerStarted { get; set; }
    
    private const int PORT = 56000;
    private const string DNS = "jaeu.iptime.org";

    private ItemListHandler _itemListHandler;

    private readonly ConcurrentQueue<NetworkData> _sendQueue = new();
    private readonly ConcurrentQueue<string> _receiveQueue = new();
    
    private TcpClient _client;
    private NetworkStream _stream;

    private async void Awake()
    {
        await ConnectToServer();
    }

    private async void Start()
    {
        _itemListHandler = ItemListHandler.Instance.Result;
        await Task.Run(SendDataToServer);
        await Task.Run(ReceiveDataFromServer);
        await Task.Run(ProcessReceivedData);
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
            IsServerStarted = true;
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
        Debug.Log($"Send Data To Server Started");

        while (true)
        {
            if (!IsServerStarted)
            {
                await Task.Delay(100);
                continue;
            }

            if (!IsRunning)
            {
                break;
            }

            NetworkData data;
            while(!_sendQueue.TryDequeue(out data))
            {
                await Task.Delay(100);
            }

            string sendData = $"{data.type.ToString()},{data.data}";

            try
            {
                byte[] buffer = System.Text.Encoding.UTF8.GetBytes(sendData);
                byte[] lengthBuffer = BitConverter.GetBytes(buffer.Length);
                
                await _stream.WriteAsync(lengthBuffer, 0, lengthBuffer.Length);
                await _stream.WriteAsync(buffer, 0, buffer.Length);
            }
            catch (Exception e)
            {
                Debug.LogError($"Exception : {e.Message}");
                break;
            }
        }
    }

    private async void ReceiveDataFromServer()
    {
        Debug.Log($"Receive Data From Server Started");
        while (true)
        {
            if (!IsServerStarted)
            {
                await Task.Delay(100);
                continue;
            }

            if (!IsRunning)
            {
                break;
            }

            byte[] lengthBuffer = new byte[4];
            int byteCount = await _stream.ReadAsync(lengthBuffer, 0, lengthBuffer.Length);
            if (byteCount == 0)
            {
                Debug.Log("Disconnected from server");
                break;
            }

            int dataLength = BitConverter.ToInt32(lengthBuffer, 0);
            byte[] buffer = new byte[dataLength];
            byteCount = await _stream.ReadAsync(buffer, 0, buffer.Length);
            Debug.Log($"Received Data from Server");

            string receivedData = System.Text.Encoding.UTF8.GetString(buffer, 0, byteCount);

            _receiveQueue.Enqueue(receivedData);
        }
    }

    private async void ProcessReceivedData()
    {
        Debug.Log($"Process Received Data Started");
        string receivedData = "";

        while (true)
        {
            if (!IsServerStarted)
            {
                await Task.Delay(100);
                continue;
            }

            if (!IsRunning)
            {
                break;
            }

            while(!_receiveQueue.TryDequeue(out receivedData))
            {
                await Task.Delay(100);
            }

            int i = 0;

            string typeData = "";

            while (receivedData[i] != ',')
            {
                typeData += receivedData[i++];
            }
            
            receivedData = receivedData.Remove(0, i + 1);
            ENetworkDataType type = (ENetworkDataType)Enum.Parse(typeof(ENetworkDataType), typeData);
            NetworkData data = new NetworkData(type, receivedData);

            switch (data.type)
            {
                case ENetworkDataType.Get:
                    _itemListHandler.ItemDataQueue.Enqueue(data.data);
                    
                    break;
                case ENetworkDataType.Disconnect:
                    Debug.Log($"Disconnected from server");
                    break;
                case ENetworkDataType.Error:
                    Debug.LogError($"Error : {data.data}");
                    break;
                case ENetworkDataType.Log:
                default:
                    Debug.Assert(false);
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
        
        IsRunning = false;
        _stream?.Close();
        _client?.Close();
    }
}
