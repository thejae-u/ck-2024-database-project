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
    public string requestUserId;
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

    public Query(NetworkData data, EQueryType queryType, string requestUserId, string queryMessage)
    {
        this.data = data;
        this.queryType = queryType;
        this.requestUserId = requestUserId;
        this.queryMessage = queryMessage;
    }
}

public static class DatabaseHandler
{
    private static readonly ConcurrentQueue<Query> queries = new ConcurrentQueue<Query>();

    private static MySqlConnection _conn = new MySqlConnection(MysqlConnectString.STR_CONN);

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
        while (true)
        {
            // TODO : SQL 연결 후 Query 처리, Query Type 에 따라서 정해진 Query 전송
            while (!queries.TryDequeue(out query))
            {
                await Task.Delay(100);
            }

            string strQuery;
            MySqlCommand cmd = new MySqlCommand();
            MySqlDataReader reader = null;
            NetworkData sendData = null;

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

                    case EQueryType.Login:
                        strQuery = "SELECT * FROM userinfo WHERE uid = @userId";
                        cmd = new MySqlCommand(strQuery, _conn);
                        cmd.Parameters.AddWithValue("@userId", query.queryMessage);
                        reader = (MySqlDataReader) await cmd.ExecuteReaderAsync();

                        string uid = string.Empty;
                        int cash = 0;

                        while (reader.Read())
                        {
                            uid = (string)reader["uid"];
                            cash = (int)reader["cash"];
                            string data = $"user_info@{uid},{cash}";
                            sendData = new NetworkData(query.data.client, ENetworkDataType.Login, data);
                        }

                        await GameServer.AddConnectUser(uid, cash);
                        GameServer.SendData.Enqueue(sendData);
                        break;

                    case EQueryType.Update:
                        // 트랜잭션 호출, 데이터 혹은 결과 클라이언트로 전송
                        DatabaseTransactions.ExecuteExchangeTransaction(_conn, query);
                        break;

                    case EQueryType.Get:
                        ETableList tableToGet = (ETableList)Enum.Parse(typeof(ETableList), query.queryMessage);
                        strQuery = $"SELECT * FROM {tableToGet}";
                        cmd = new MySqlCommand(strQuery, _conn);

                        reader = (MySqlDataReader)await cmd.ExecuteReaderAsync();

                        ETableList table = (ETableList)Enum.Parse(typeof(ETableList), query.queryMessage);
                        switch (table)
                        {
                            case ETableList.item_list:
                                List<NetworkData> sendDataList = new List<NetworkData>();
                                
                                while (reader.Read())
                                {
                                    string data = $"item_list@{reader["uid"]},{reader["item"]},{reader["price"]}";
                                    NetworkData readData = new NetworkData(query.data.client, ENetworkDataType.Get, data);
                                    sendDataList.Add(readData);
                                }

                                foreach(var data in sendDataList)
                                {
                                    GameServer.SendData.Enqueue(data);
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
                Log.PrintToServer(e.Message);
            }
            finally
            {
                cmd?.Dispose();
                reader?.Close();
            }
        }
    }
}