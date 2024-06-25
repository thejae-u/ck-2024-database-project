using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using NetworkDataDLL;

public static class DatabaseClient
{
    private static readonly string SERVER_DOMAIN = "jaeu.iptime.org";
    private static readonly int PORT = 56000;
    
    private static TcpClient _client;
    private static NetworkStream _stream;
    
    private static bool _isRunning;
    private static bool _isConnected;
    
    private static async Task Main()
    {
        AppDomain.CurrentDomain.ProcessExit += OnProcessExit!;
        _isRunning = await Start();
        
        if(!_isRunning)
        {
            return;
        }

        _ = Task.Run(SendAsyncUntilExit);
        _ = Task.Run(ReceiveAsyncUntilExit);

        while (_isRunning)
        {
            await Task.Delay(1000);
        }
    }

    private static void OnProcessExit(object sender, EventArgs e)
    {
        CloseConnection();
        Console.WriteLine($"Press Any Key to Exit Program");
        Console.ReadLine();
    }

    private static async Task<bool> Start()
    {
        _client = new TcpClient();
        Console.WriteLine($"Get IP Address from {SERVER_DOMAIN}");
        
        IPAddress[] serverIpAddress = await Dns.GetHostAddressesAsync(SERVER_DOMAIN);

        Console.WriteLine($"Success to get IP Address");
        foreach(var address in serverIpAddress)
        {
            Console.WriteLine($"IP Address: {address}");
        }
        
        IPAddress ipAddress = serverIpAddress[0];

        try
        {
            Console.WriteLine($"Connecting to {ipAddress}:{PORT}");
            await _client.ConnectAsync(ipAddress, PORT);
        }
        catch (Exception e)
        {
            Console.WriteLine($"Failed to connect to server");
            return false;
        }

        Console.WriteLine($"Connected to {ipAddress}:{PORT}");
        _stream = _client.GetStream();
        return true;
    }

    private static Task ShowMenu()
    {
        Console.WriteLine($"1. Get List of Items");
        Console.WriteLine($"2. Buy Item");
        Console.WriteLine($"3. User List");
        Console.WriteLine($"4. Log");
        Console.WriteLine($"5. Sell Item");
        Console.WriteLine($"0. Exit");
        return Task.CompletedTask;
    }
    
    private static async Task ReceiveAsyncUntilExit()
    {
        Console.WriteLine($"Receive Async Enabled");
        
        while (_isRunning)
        {
            // 길이를 먼저 받음
            byte[] lengthBuffer = new byte[4];
            int lengthRead = await _stream.ReadAsync(lengthBuffer, 0, lengthBuffer.Length);
            if (lengthRead == 0)
            {
                Console.WriteLine($"Server Disconnected");
                _isRunning = false;
                continue;
            }
            
            int length = BitConverter.ToInt32(lengthBuffer, 0);
            
            // 데이터를 받음
            byte[] buffer = new byte[length];
            int bytesRead = await _stream.ReadAsync(buffer, 0, buffer.Length);

            NetworkData networkData = NetworkData.Deserialize(buffer, bytesRead);
            Console.WriteLine($"{networkData.type}, {networkData.data}");
        }
        
        Console.WriteLine($"Receive Async Disabled");
    }

    private static async Task SendAsyncUntilExit()
    {
        while (_isRunning)
        {
            await ShowMenu();
            int input;
            try
            {
                input = Convert.ToInt32(Console.ReadLine());
            }
            catch (Exception e)
            {
                Console.WriteLine($"Invalid Input Type : {e.Message}");
                continue;
            }

            try
            {
                await ProcessUserInput(input);
            }
            catch (Exception e)
            {
                _isRunning = false;
                Console.WriteLine($"Error: {e.Message}");
            }
        }
        
        Console.WriteLine($"Exit Program");
    }

    private static void CloseConnection()
    {
        _stream.Close();
        _client.Close();
        Console.WriteLine($"Network Connection Closed");
    }

    private static Task ProcessUserInput(int input)
    {
        NetworkData? networkData = null;
        switch (input)
        {
            case 1: // Get List Of Items
                networkData = NetworkFunctions.GetItemListNetworkData();
                break;
            case 2: // Buy Item
                networkData = NetworkFunctions.DisplayBuyItemAndGetNetworkDataOrNull();
                break;
            case 3: // Display User List
                networkData = NetworkFunctions.GetUserInfoNetworkData();
                break;
            case 4: // Display Log To Server
                networkData = NetworkFunctions.GetLogNetworkData();
                break;
            case 5: // Sell Item
                networkData = NetworkFunctions.GetSellNetworkDataOrNull();
                break;
            case 0:
                _isRunning = false;
                break;
            default:
                Console.WriteLine($"Out of Range : Invalid Input");
                break;
        }
        
        return SendDataAsync(networkData);
    }

    private static async Task SendDataAsync(NetworkData? networkData)
    {
        if (networkData == null)
        {
            return;
        }

        try
        {
            string dataStr = NetworkData.Serialize(networkData);

            byte[] lengthBuffer = BitConverter.GetBytes(dataStr.Length);
            byte[] buffer = Encoding.UTF8.GetBytes(dataStr);

            // Send Length First
            await _stream.WriteAsync(lengthBuffer, 0, lengthBuffer.Length);
            // Send Data Second
            await _stream.WriteAsync(buffer, 0, buffer.Length);
        }
        catch (Exception e)
        {
            Console.WriteLine($"{e.Message}");
        }
    }
}