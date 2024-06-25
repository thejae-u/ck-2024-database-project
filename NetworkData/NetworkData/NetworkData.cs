using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace NetworkDataDLL;

[Serializable]
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

[Serializable]
public enum ETableList
{
    item_list,
    userinfo,
    log
}

[Serializable]
public class NetworkData
{
    public TcpClient? client;
    public ENetworkDataType type;
    public string data;

    public NetworkData(ENetworkDataType type, string data)
    {
        this.type = type;
        this.data = data;
    }

    public NetworkData(TcpClient client, ENetworkDataType type, string data)
    {
        this.client = client;
        this.type = type;
        this.data = data;
    }
    
    public static string Serialize(NetworkData networkData)
    {
        return $"{networkData.type},{networkData.data}";
    }

    public static NetworkData Deserialize(byte[] buffer, int bytesRead)
    {
        string recvData = Encoding.UTF8.GetString(buffer, 0, bytesRead);
        string type = "";

        int i = 0;
        while (recvData[i] != ',')
        {
            type += recvData[i++];
        }

        recvData = recvData.Remove(0, i + 1);


        ENetworkDataType dataType = (ENetworkDataType)Enum.Parse(typeof(ENetworkDataType), type);
        NetworkData networkData = new NetworkData(dataType, recvData);
        return networkData;
    }
}