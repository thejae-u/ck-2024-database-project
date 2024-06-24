using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;

public enum ETableList
{
    item_list,
    userinfo,
    log
}

public enum EQueryType
{
    None,
    Login,
    Log,
    Update,
    Get
}

public class Query
{
    public NetworkData data;
    public EQueryType queryType;
    public string queryMessage;

    public Query(EQueryType queryType, string queryMessage)
    {
        this.queryType = queryType;
        this.queryMessage = queryMessage;
    }

    public Query(NetworkData data, EQueryType queryType, string queryMessage)
    {
        this.data = data;
        this.queryType = queryType;
        this.queryMessage = queryMessage;
    }
}

public static class DatabaseHandler
{
    private static readonly ConcurrentQueue<Query> queries = new ConcurrentQueue<Query>();

    private static MySqlConnection _conn = new MySqlConnection(MysqlConnectString.STR_CONN);

    private static readonly List<NetworkData> _sendDataList = new List<NetworkData>();

    public static void Start()
    {
        _ = Task.Run(ExecuteQuery);
    }


    public static void EnqueueQuery(Query query)
    {
        queries.Enqueue(query);
    }

    private static async Task ExecuteQuery()
    {
        _conn.Open();

        Log.PrintToServer("Database Connected And Wait Query");

        Query query;
        while (GameServer.IsRunning)
        {
            while (!queries.TryDequeue(out query))
            {
                await Task.Delay(100);
            }

            try
            {
                switch (query.queryType)
                {
                    case EQueryType.Log:
                        await DatabaseTransactions.AddLogTransaction(_conn, query);
                        break;

                    case EQueryType.Login:
                        await DatabaseTransactions.LoginTransaction(_conn, query);
                        break;

                    case EQueryType.Update:
                        // 아이템 구매 트랜잭션
                        await DatabaseTransactions.ExecuteExchangeTransaction(_conn, query);
                        break; 
                    case EQueryType.Get:
                        // 테이블 조회 트랜잭션
                        await DatabaseTransactions.GetTableTransaction(_conn, query);
                        break;
                    case EQueryType.None:
                    default:
                        Log.PrintToServer("Invalid Query " + query.queryMessage);
                        break;
                }
            }
            catch(Exception e) 
            {
                Log.PrintToServer(e.Message);
            }
        }
    }
}