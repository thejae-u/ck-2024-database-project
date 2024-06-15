using System;
using System.Collections.Generic;
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
            Query query;
            lock (queries)
            {
                query = queries.Dequeue();
            }

            string strQuery;
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
                        ETableList tableToGet = (ETableList)Enum.Parse(typeof(ETableList), query.queryMessage);
                        strQuery = $"SELECT * FROM {tableToGet}";
                        cmd = new MySqlCommand(strQuery, _conn);

                        MySqlDataReader reader = (MySqlDataReader)await cmd.ExecuteReaderAsync();

                        ETableList table = (ETableList)Enum.Parse(typeof(ETableList), query.queryMessage);
                        switch (table)
                        {
                            case ETableList.item_list:
                                NetworkData sendData = new NetworkData(query.data.client, ENetworkDataType.Get, "");
                                
                                while (reader.Read())
                                {
                                    sendData.data = $"item_list@{reader["uid"]},{reader["item_name"]},{reader["price"]}";
                                    lock (GameServer.SendData)
                                    {
                                        GameServer.SendData.Enqueue(sendData);
                                    }
                                }

                                break;
                            case ETableList.userinfo:
                                break;
                            case ETableList.log:
                            default:
                                Log.PrintToServer("Invalid Query " + query.queryMessage);
                                break;
                        }

                        reader.Close();
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