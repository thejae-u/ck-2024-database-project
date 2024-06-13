using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Log
{
    public static void LogSend(string message)
    {
        NetworkData logData = new (ENetworkDataType.Log, message);
        NetworkManager.Instance.EnqueueData(logData);
    }
}
