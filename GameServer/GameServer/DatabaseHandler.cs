using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using NetworkDataDLL;

public enum EQueryType
{
    None,
    Login,
    Log,
    Buy,
    Sell,
    Get
}

public class Query
{
    public readonly NetworkData? data;
    public readonly EQueryType queryType;
    public readonly string queryMessage;

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

    public static void Start()
    {
        // 쿼리 실행 Task 시작
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

        while (GameServer.IsRunning)
        {
            Query query;
            // 쿼리가 들어있는 Queue에서 쿼리 Dequeue
            while (!queries.TryDequeue(out query))
            {
                // 쿼리가 없으면 대기
                await Task.Delay(100);
            }

            try
            {
                // 쿼리 타입에 따라 트랜잭션 실행
                switch (query.queryType)
                {
                    case EQueryType.Log:
                        await DatabaseTransactions.AddLogTransaction(_conn, query);
                        break;

                    case EQueryType.Login:
                        await DatabaseTransactions.LoginTransaction(_conn, query);
                        break;
                    case EQueryType.Buy:
                        await DatabaseTransactions.ExecuteExchangeTransaction(_conn, query);
                        break; 
                    case EQueryType.Sell:
                        await DatabaseTransactions.ExecuteSellTransaction(_conn, query);
                        break;
                    case EQueryType.Get:
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