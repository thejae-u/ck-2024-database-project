using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

public class GameSerever
{
    public const int PORT = 56000;
    public const int BUFFER_SIZE = 1024;

    private static readonly Queue<NetworkData> _data = new Queue<NetworkData>();
    private static Task _dequeueTask;

    private static async Task Main(string[] args)
    {
        TcpListener server = new TcpListener(IPAddress.Any, PORT);
        server.Start();
        string log = $"{DateTime.Now} Server Started on {PORT}";
        Console.WriteLine(log);

        _ = Task.Run(HandleDequeueNetworkData);

        log = $"{DateTime.Now} Client Accept Started";
        Console.WriteLine(log);
        while (true)
        {
            TcpClient client = await server.AcceptTcpClientAsync();
            log = $"{DateTime.Now} Client {client.Client.RemoteEndPoint} Connected.";
            Console.WriteLine(log);
            HandleClientAsync(client);
        }
    }

    private static Task HandleDequeueNetworkData()
    {
        string log = $"{DateTime.Now} DequeueHandler Started";
        Console.WriteLine(log);

        while (true)
        {
            if( _data.Count == 0 )
            {
                continue;
            }

            NetworkData networkData = _data.Dequeue();
            ApplyNetworkRequest(networkData);
        }
    }

    private static void ApplyNetworkRequest(NetworkData data)
    {
        string log = $"{DateTime.Now} {data.client.Client.RemoteEndPoint} : Request Type \"{data.type}\" Request Data \"{data.data}\"";
        Console.WriteLine(log);
        switch (data.type)
        {
            case ENetworkDataType.Login:
                break;
            case ENetworkDataType.Register:
                break;
            case ENetworkDataType.Request:
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
                Console.WriteLine($"Invalid Type Request from {data.client.Client.RemoteEndPoint}");
                try
                {
                    data.client.Close();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
                break;
        }
    }

    private static async void HandleClientAsync(TcpClient client)
    {
        NetworkStream stream = client.GetStream();
        byte[] buffer = new byte[BUFFER_SIZE];

        try
        {
            while (true)
            {
                int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);

                if (bytesRead == 0)
                {
                    Console.WriteLine($"Disconnected {client.Client.RemoteEndPoint}");
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

                _data.Enqueue(networkData);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Exception: " + ex.Message);
        }
        finally
        {
            stream.Close();
            client.Close();
        }
    }

    private static ENetworkDataType ConvertStringToNetworkDataType(string typeString)
    {
        switch (typeString)
        {
            case "Login":
                return ENetworkDataType.Login;
            case "Register":
                return ENetworkDataType.Register;
            case "Request":
                return ENetworkDataType.Request;
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

