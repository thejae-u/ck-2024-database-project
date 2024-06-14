using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;

public enum EQueryType
{
    None,
    Log,
    Update,
    Get
}

public class Query
{
    public EQueryType queryType;
    public string queryMessage;

    public Query(EQueryType queryType, string queryMessage)
    {
        this.queryType = queryType;
        this.queryMessage = queryMessage;
    }
}


public static class DatabaseHandler
{
    private static readonly Queue<Query> queries = new Queue<Query>();

    private static MySqlConnection _conn = new MySqlConnection(MysqlConnectString.STR_CONN);

    public static void Start()
    {
        _ = Task.Run(ExecuteQuery);
    }


    public static void EnqueueQuery(Query query)
    {
        lock(queries)
        {
            queries.Enqueue(query);
        }
    }

    private static async Task ExecuteQuery()
    {
        _conn.Open();

        Log.PrintToServer("Database Connected And Wait Query");

        while (true)
        {
            if(queries.Count == 0)
            {
                await Task.Delay(100);
                continue;
            }

            // TODO : SQL 연결 후 Query 처리, Query Type 에 따라서 정해진 Query 전송
            Query query = queries.Dequeue();
            string strQuery = "";
            MySqlCommand cmd = new MySqlCommand();

            try
            {
                switch (query.queryType)
                {
                    case EQueryType.Log:
                        strQuery = "INSERT INTO log (message) values(@message)";
                        cmd = new MySqlCommand(strQuery, _conn);
                        cmd.Parameters.AddWithValue("@message", query.queryMessage);
                        await cmd.ExecuteNonQueryAsync();
                        break;
                    case EQueryType.Update:
                        strQuery = "";
                        break;
                    case EQueryType.Get:
                        break;
                    case EQueryType.None:
                    default:
                        Log.PrintToServer("Invalid Query " + query.queryMessage);
                        break;
                }
            }
            catch(Exception e) 
            {
                Console.WriteLine(e.Message);
            }
            finally
            {
                cmd.Dispose();
            }
        }
    }
}