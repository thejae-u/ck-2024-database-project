using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum ETableList
{
    item_list,
    userinfo,
    log
}

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
    public readonly ENetworkDataType type;
    public readonly string data;
    
    public NetworkData(ENetworkDataType type, string data)
    {
        this.type = type;
        this.data = data;
    }
}
