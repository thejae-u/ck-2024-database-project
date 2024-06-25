using System;
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

        // 서버 Task 시작
        _ = Task.Run(HandleDequeueNetworkData);
        _ = Task.Run(() => HandleInputAsync(_cts));
        _ = Task.Run(SendDataToClientAsync);

        // DB Task 시작
        DatabaseHandler.Start();

        Log.PrintToServer("Client Accept Started");

        // 서버 종료 시그널이 들어올 때 까지 반복
        while (!_cts.Token.IsCancellationRequested)
        {
            // 클라이언트 연결 대기
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

    // 서버 종료 시 연결 되어있는 클라이언트 모두 해제
    private static void OnExit(object sender, EventArgs e)
    {
        lock (_connectedClients)
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
    }

    /// <summary>
    /// 네트워크로 들어온 데이터를 처리하는 핸들러
    /// </summary>
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

            await ApplyNetworkRequest(networkData);
        }
    }

    /// <summary>
    /// 네트워크로 들어온 데이터를 적용하는 핸들러
    /// </summary>
    private static async Task ApplyNetworkRequest(NetworkData data)
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
                if (_connectedUsers.ContainsKey(data.data))
                {
                    while (!_connectedUsers.TryRemove(data.data, out int cash))
                    {
                        await Task.Delay(100);
                    }
                }
                else
                {
                    Log.PrintToDB($"Invalid Disconnect Request from {GetClientIp(data.client)}");
                }
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

    /// <summary>
    /// 유저 접속 시 유저 정보를 추가하는 함수
    /// </summary>
    public static async Task AddConnectUser(string userId, int cash)
    {
        await Task.Run(() => _connectedUsers.TryAdd(userId, cash));
    }

    /// <summary>
    /// 클라이언트의 요청을 처리하는 함수
    /// </summary>
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

    /// <summary>
    /// 클라이언트로 데이터를 전송하는 함수
    /// </summary>
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

    /// <summary>
    /// Exit 입력 시 종료
    /// </summary>
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

    /// <summary>
    /// 클라이언트의 IP 주소를 반환하는 함수
    /// </summary>
    public static IPAddress GetClientIp(TcpClient client)
    {
        return ((IPEndPoint)client.Client.RemoteEndPoint).Address;
    }
}
