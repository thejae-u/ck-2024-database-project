using System;
using System.Net.Sockets;

public enum ENetworkDataType
{
    None,
    Login,
    Register,
    Get,
    Buy,
    Sell,
    Search,
    Log,
    Error,
    Disconnect
}

public class NetworkData
{
    public TcpClient client;
    public DateTime time;
    public ENetworkDataType type;
    public string data;

    public NetworkData(TcpClient client, ENetworkDataType type, string data)
    {
        this.client = client;
        this.type = type;
        this.data = data;
        time = DateTime.Now;
    }
}