using System;
using System.Text;
using NetworkDataDLL;

public class NetworkFunctions
{
    public static NetworkData? DisplayBuyItemAndGetNetworkDataOrNull()
    {
        Console.Write("user id : ");
        string? uid = Console.ReadLine();
        Console.Write("item name : ");
        string? itemName = Console.ReadLine();
        Console.Write($"Sell User id : ");
        string? sellUserId = Console.ReadLine();

        if (string.IsNullOrEmpty(uid)
            || string.IsNullOrEmpty(itemName) 
            || string.IsNullOrEmpty(sellUserId))
        {
            return null;
        }
        
        string dataStr = $"{uid},{sellUserId}@{itemName}";
        NetworkData networkData = new NetworkData(ENetworkDataType.Buy, dataStr);
        return networkData;
    }

    public static NetworkData GetItemListNetworkData()
    {
        string dataStr = ETableList.item_list.ToString();
        NetworkData networkData = new NetworkData(ENetworkDataType.Get, dataStr);
        return networkData;
    }

    public static NetworkData GetUserInfoNetworkData()
    {
        string dataStr = ETableList.userinfo.ToString();
        NetworkData networkData = new NetworkData(ENetworkDataType.Get, dataStr);
        return networkData;
    }

    public static NetworkData GetLogNetworkData()
    {
        string dataStr = ETableList.log.ToString();
        NetworkData networkData = new NetworkData(ENetworkDataType.Get, dataStr);
        return networkData;
    }

    public static NetworkData? GetSellNetworkDataOrNull()
    {
        Console.Write("user id : ");
        string? uid = Console.ReadLine();
        Console.Write("item name : ");
        string? itemName = Console.ReadLine();
        Console.Write("price : ");
        string? price = Console.ReadLine();

        if (string.IsNullOrEmpty(uid)
            || string.IsNullOrEmpty(itemName)
            || string.IsNullOrEmpty(price))
        {
            return null;
        }

        string dataStr = $"{uid}@{itemName},{price}";
        NetworkData networkData = new NetworkData(ENetworkDataType.Sell, dataStr);
        return networkData;
    }
}