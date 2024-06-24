﻿using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.Concurrent;

public class GameServer
{
    private const int PORT = 56000;

    private static readonly ConcurrentDictionary<string, int> _connectedUsers = new ConcurrentDictionary<string, int>();

    private static readonly ConcurrentQueue<NetworkData> _data = new ConcurrentQueue<NetworkData>();
    private static readonly ConcurrentQueue<NetworkData> _sendData = new ConcurrentQueue<NetworkData>();
    private static CancellationTokenSource _cts = new CancellationTokenSource();
    private static readonly List<TcpClient> _connectedClients = new List<TcpClient>();

    public static ConcurrentQueue<NetworkData> SendData => _sendData;
    public static bool IsRunning => !_cts.Token.IsCancellationRequested;

    private static async Task Main()
    {
        AppDomain.CurrentDomain.ProcessExit += OnExit;
        TcpListener server = new TcpListener(IPAddress.Any, PORT);
        server.Start();
        Log.PrintToServer($"Server Started on {PORT}");

        _ = Task.Run(HandleDequeueNetworkData);

        _ = Task.Run(() => HandleInputAsync(_cts));

        _ = Task.Run(SendDataToClientAsync);

        DatabaseHandler.Start();

        Log.PrintToServer("Client Accept Started");

        while (!_cts.Token.IsCancellationRequested)
        {
            if (server.Pending())
            {
                TcpClient client = await server.AcceptTcpClientAsync();
                Log.PrintToDB($"Client {GetClientIp(client)} Connected");
                _ = HandleClientAsync(client);
            }
            else
            {
                await Task.Delay(10);
            }
        }

        server.Stop();
        Log.PrintToServer("Server Stopped");
    }

    private static void OnExit(object sender, EventArgs e)
    {
        if (_connectedClients.Count == 0)
        {
            return;
        }

        Log.PrintToServer("Connection Close");

        foreach (var client in _connectedClients)
        {
            // DB has Disconnected -> server Log
            Log.PrintToServer($"{GetClientIp(client)} Disconnected");
            client.Close();
        }
    }

    private static async Task HandleDequeueNetworkData()
    {
        Log.PrintToServer("DequeueHandler Started");

        while (!_cts.Token.IsCancellationRequested)
        {
            NetworkData networkData;
            while (!_data.TryDequeue(out networkData))
            {
                await Task.Delay(100);
            }

            ApplyNetworkRequest(networkData);
        }
    }

    private static void ApplyNetworkRequest(NetworkData data)
    {
        Log.PrintToDB($"{GetClientIp(data.client)} : Request Type \'{data.type}\' Request Data \'{data.data}\'");
        Query query;
        NetworkData sendData = null;

        switch (data.type)
        {
            case ENetworkDataType.Login:
                if (_connectedUsers.ContainsKey(data.data))
                {
                    // Already Exist
                    // Send To Invalid
                    sendData = new NetworkData(data.client, ENetworkDataType.Error, "");
                    Log.PrintToDB($"Login Request Denied {data.data} alread Exist");
                    SendData.Enqueue(sendData);
                }
                else
                {
                    // New User Login
                    // DB Find and Cash Check And Get Cash
                    // Send To DB Login Request
                    query = new Query(EQueryType.Login, data.data);
                    DatabaseHandler.EnqueueQuery(query);
                    Log.PrintToDB($"Login Request Applied {data.data}");
                }
                break;

            case ENetworkDataType.Register:
                break;

            case ENetworkDataType.Get:
                query = new Query(data, EQueryType.Get, data.data);
                DatabaseHandler.EnqueueQuery(query);
                break;

            case ENetworkDataType.Buy:
                string strData = data.data;
                query = new Query(data, EQueryType.Update, strData);
                DatabaseHandler.EnqueueQuery(query);
                break;

            case ENetworkDataType.Sell:
                break;

            case ENetworkDataType.Search:
                break;

            case ENetworkDataType.Log:
                break;

            case ENetworkDataType.Error:
                break;

            case ENetworkDataType.Disconnect:
                break;

            case ENetworkDataType.None:
            default:
                Log.PrintToDB($"Invalid Type Request from {GetClientIp(data.client)}");
                try
                {
                    lock (_connectedClients)
                    {
                        _connectedClients.Remove(data.client);
                    }

                    data.client.Close();
                }
                catch (Exception e)
                {
                    Log.PrintToServer($"Exception: {e.Message}");
                }
                break;
        }
    }

    public static async Task AddConnectUser(string userId, int cash)
    {
        await Task.Run(() => _connectedUsers.TryAdd(userId, cash));
    }

    private static async Task HandleClientAsync(TcpClient client)
    {
        lock (_connectedClients)
        {
            _connectedClients.Add(client);
        }

        NetworkStream stream = client.GetStream();

        try
        {
            while (!_cts.Token.IsCancellationRequested)
            {
                byte[] lengthBuffer = new byte[4];
                int lengthRead = await stream.ReadAsync(lengthBuffer, 0, lengthBuffer.Length);

                if (lengthRead == 0)
                {
                    Log.PrintToDB("Disconnected " + GetClientIp(client));
                    break;
                }

                int dataLength = BitConverter.ToInt32(lengthBuffer, 0);
                byte[] buffer = new byte[dataLength];

                int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);

                string recvData = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                string type = "";

                int i = 0;
                while (recvData[i] != ',')
                {
                    type += recvData[i++];
                }

                recvData = recvData.Remove(0, i + 1);

                ENetworkDataType dataType = (ENetworkDataType)Enum.Parse(typeof(ENetworkDataType), type);
                NetworkData networkData = new NetworkData(client, dataType, recvData);

                _data.Enqueue(networkData);
            }
        }
        catch (Exception e)
        {
            Log.PrintToServer("Exception: " + e.Message);
        }
        finally
        {
            lock (_connectedClients)
            {
                _connectedClients.Remove(client);
            }

            stream.Close();
            client.Close();
        }
    }

    private static async Task SendDataToClientAsync()
    {
        Log.PrintToServer($"SendDataToClientAsync Started");

        while (true)
        {
            NetworkData data;
            while (!_sendData.TryDequeue(out data))
                await Task.Delay(100);
            {
            }

            string sendDataStr = $"{data.type},{data.data}";
            NetworkStream stream = data.client.GetStream();

            try
            {
                byte[] buffer = Encoding.UTF8.GetBytes(sendDataStr);
                byte[] lengthBuffer = BitConverter.GetBytes(buffer.Length);

                await stream.WriteAsync(lengthBuffer, 0, lengthBuffer.Length);
                await stream.WriteAsync(buffer, 0, buffer.Length);

                if (data.type == ENetworkDataType.Get)
                {
                    Log.PrintToServer($"Send To Client {data.type} {data.data} {GetClientIp(data.client)}");
                    continue;
                }
                
                Log.PrintToDB($"Send To Client {data.type} {data.data} {GetClientIp(data.client)}");
            }
            catch (Exception e)
            {
                Log.PrintToServer(e.Message);
            }
        }
    }

    private static async Task HandleInputAsync(CancellationTokenSource cts)
    {

        while (!cts.Token.IsCancellationRequested)
        {
            string input = await Task.Run(() => Console.ReadLine());
            if (input.Equals("exit", StringComparison.OrdinalIgnoreCase))
            {
                cts.Cancel();
                break;
            }
        }
    }

    public static IPAddress GetClientIp(TcpClient client)
    {
        return ((IPEndPoint)client.Client.RemoteEndPoint).Address;
    }
}
