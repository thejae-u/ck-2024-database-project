using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

public class GameServer
{
    public const int PORT = 56000;
    public const int BUFFER_SIZE = 1024;

    private static readonly Queue<NetworkData> _data = new Queue<NetworkData>();
    private static CancellationTokenSource _cts = new CancellationTokenSource();
    private static readonly List<TcpClient> _connectedClients = new List<TcpClient>();
    
    private static async Task Main()
    {
        AppDomain.CurrentDomain.ProcessExit += new EventHandler(OnExit);
        TcpListener server = new TcpListener(IPAddress.Any, PORT);
        server.Start();
        Log.PrintToServer("Server Started on " + PORT);

        _ = Task.Run(HandleDequeueNetworkData);

        _ = Task.Run(() => HandleInputAsync(_cts));

        DatabaseHandler.Start();

        Log.PrintToServer("Client Accept Started");

        while (!_cts.Token.IsCancellationRequested)
        {
            if (server.Pending())
            {
                TcpClient client = await server.AcceptTcpClientAsync();
                Log.PrintToDB("Client " + GetClientIP(client) + " Connected");
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
        if(_connectedClients.Count == 0)
        {
            return;
        }

        Log.PrintToServer("Connection Close");

        foreach(var client in _connectedClients)
        {
            // DB has Disconnected -> server Log
            Log.PrintToServer(GetClientIP(client) + " Disconnected");
            client.Close();
        }
    }

    private static async Task HandleDequeueNetworkData()
    {
        Log.PrintToServer("DequeueHandler Started");

        while (!_cts.Token.IsCancellationRequested)
        {
            if (_data.Count == 0)
            {
                await Task.Delay(10);
                continue;
            }

            NetworkData networkData;
            lock (_data)
            {
                networkData = _data.Dequeue();
            }
            ApplyNetworkRequest(networkData);
        }
    }

    private static void ApplyNetworkRequest(NetworkData data)
    {
        Log.PrintToDB(GetClientIP(data.client) + " : Request Type \'"+ data.type + "\' Request Data \'" + data.data + "\'");

        switch (data.type)
        {
            case ENetworkDataType.Login:
                break;
            case ENetworkDataType.Register:
                break;
            case ENetworkDataType.Buy:
                break;
            case ENetworkDataType.Sell:
                break;
            case ENetworkDataType.Search:
                break;
            case ENetworkDataType.Response:
                break;
            case ENetworkDataType.Log:
                break;
            case ENetworkDataType.Error:
                break;
            case ENetworkDataType.Disconnect:
                break;
            case ENetworkDataType.None:
            default:
                Log.PrintToDB($"Invalid Type Request from " + GetClientIP(data.client));
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
                    Log.PrintToServer("Exception: " + e.Message);
                }
                break;
        }
    }

    private static async Task HandleClientAsync(TcpClient client)
    {
        lock (_connectedClients)
        {
            _connectedClients.Add(client);
        }

        NetworkStream stream = client.GetStream();
        byte[] buffer = new byte[BUFFER_SIZE];

        try
        {
            while (!_cts.Token.IsCancellationRequested)
            {
                int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);

                if (bytesRead == 0)
                {
                    Log.PrintToDB("Disconnected " + GetClientIP(client));
                    break;
                }

                string recvData = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                string type = "";

                int i = 0;
                while (recvData[i] != ',')
                {
                    type += recvData[i++];
                }

                recvData = recvData.Remove(0, i + 1);

                NetworkData networkData = new NetworkData(client, ConvertStringToNetworkDataType(type), recvData);

                lock (_data)
                {
                    _data.Enqueue(networkData);
                }
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

    private static IPAddress GetClientIP(TcpClient client)
    {
        return ((IPEndPoint)client.Client.RemoteEndPoint).Address;
    }

    private static ENetworkDataType ConvertStringToNetworkDataType(string typeString)
    {
        switch (typeString)
        {
            case "Login":
                return ENetworkDataType.Login;
            case "Register":
                return ENetworkDataType.Register;
            case "Buy":
                return ENetworkDataType.Buy;
            case "Sell":
                return ENetworkDataType.Sell;
            case "Search":
                return ENetworkDataType.Search;
            case "Response":
                return ENetworkDataType.Response;
            case "Log":
                return ENetworkDataType.Log;
            case "Error":
                return ENetworkDataType.Error;
            case "Disconnect":
                return ENetworkDataType.Error;
            // Invalid Area
            case "None":
            default:
                return ENetworkDataType.None;
        }
    }
}