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
    public ENetworkDataType type;
    public string data;

    public NetworkData(ENetworkDataType type, string data)
    {
        this.type = type;
        this.data = data;
    }
}
