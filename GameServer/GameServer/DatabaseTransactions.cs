using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using NetworkDataDLL;

public static class DatabaseTransactions
{
    private static readonly List<NetworkData> _sendDataList = new List<NetworkData>();

    /// <summary>
    /// 아이템 구매 트랜잭션
    /// </summary>
    public static async Task ExecuteExchangeTransaction(MySqlConnection conn, Query query)
    {
        MySqlCommand cmd = conn.CreateCommand();
        MySqlTransaction tr = await conn.BeginTransactionAsync();

        cmd.Connection = conn;
        cmd.Transaction = tr;

        // Split Query message
        int i = 0;
        string buyUserId = string.Empty;
        string sellUserId = string.Empty;

        // 트랜잭션 시작 전 쿼리 메시지 파싱
        while (query.queryMessage[i] != ',')
        {
            buyUserId += query.queryMessage[i++];
        }

        string itemNameWithSellUserId = query.queryMessage.Remove(0, i + 1);

        i = 0;
        while (itemNameWithSellUserId[i] != '@')
        {
            sellUserId += itemNameWithSellUserId[i++];
        }

        string itemName = itemNameWithSellUserId.Remove(0, i + 1);

        // 트랜잭션 시작
        try
        {
            // 테이블 락
            cmd.CommandText = $"LOCK TABLES userinfo WRITE, item_list WRITE";
            await cmd.ExecuteNonQueryAsync();

            // 요청자의 정보가 유효한지 확인
            cmd.CommandText = $"SELECT uid FROM userinfo WHERE uid = @user_id_buy";
            cmd.Parameters.AddWithValue("@user_id_buy", buyUserId);
            if (await cmd.ExecuteScalarAsync() == null)
            {
                Log.PrintToDB($"{GameServer.GetClientIp(query.data.client)} Invalid User Id Request");
                await tr.RollbackAsync();
                return;
            }

            // 아이템의 정보가 유효한지 확인
            cmd.CommandText = $"SELECT item FROM item_list WHERE item = @item_name AND uid = @user_id_sell";
            cmd.Parameters.AddWithValue("@item_name", itemName);
            cmd.Parameters.AddWithValue("@user_id_sell", sellUserId);
            if (await cmd.ExecuteScalarAsync() == null)
            {
                Log.PrintToDB($"Exchange by {buyUserId} Failed - Invalid Item");
                await tr.RollbackAsync();
                return;
            }

            // 요청자와 판매자가 다른지 확인 (자기 자신에게 거래 요청 불가)
            if (buyUserId == sellUserId)
            {
                Log.PrintToDB($"Exchange by {buyUserId} Failed - Same User Request");
                await tr.RollbackAsync();
                return;
            }

            // 요청자 잔액 저장
            cmd.CommandText = $"SELECT cash FROM userinfo WHERE uid = @user_id_buy";
            int buyUserCash = (int)await cmd.ExecuteScalarAsync();

            // 아이템 가격 저장
            cmd.CommandText = $"SELECT price FROM item_list WHERE item = @item_name AND uid = @user_id_sell";
            int sellPrice = (int)await cmd.ExecuteScalarAsync();

            // 아이템 가격과 요청자 잔액을 비교 후 거래 진행
            if (buyUserCash > sellPrice)
            {
                // 요청자 잔액 갱신 -> 현재 잔액 - 아이템 가격
                int buyUserCashAfterExchange = buyUserCash - sellPrice;
                cmd.CommandText = $"UPDATE userinfo SET cash = @after_cash WHERE uid = @user_id_buy";
                cmd.Parameters.AddWithValue("@after_cash", buyUserCashAfterExchange);
                await cmd.ExecuteNonQueryAsync();

                // 판매자 잔액 갱신 -> 현재 잔액 + 아이템 가격
                cmd.CommandText = $"UPDATE userinfo SET cash = cash + @sellPrice WHERE uid = @user_id_sell";
                cmd.Parameters.AddWithValue("@sellPrice", sellPrice);
                await cmd.ExecuteNonQueryAsync();

                // 아이템 리스트에서 삭제
                cmd.CommandText = $"DELETE FROM item_list WHERE item = @item_name AND uid = @user_id_sell";
                await cmd.ExecuteNonQueryAsync();

                await tr.CommitAsync();
                Log.PrintToDB(
                    $"Exchange Success {itemName} - Buy : {buyUserId}, Sell : {sellUserId}, Price : {sellPrice}");
            }
            else
            {
                await tr.RollbackAsync();
                Log.PrintToDB($"Exchange by {buyUserId} Failed - Not Enough Cash");
            }

        }
        catch (Exception e)
        {
            await tr.RollbackAsync();
            Log.PrintToServer(e.Message);
        }
        finally
        {
            // 테이블 언락
            cmd.CommandText = "UNLOCK TABLES";
            await cmd.ExecuteNonQueryAsync();
        }
    }

    /// <summary>
    /// 테이블 조회 트랜잭션
    /// </summary>
    public static async Task GetTableTransaction(MySqlConnection conn, Query query)
    {
        MySqlCommand cmd = conn.CreateCommand();

        try
        {
            // 테이블 락
            cmd.CommandText = $"LOCK TABLES {query.queryMessage} READ";
            await cmd.ExecuteNonQueryAsync();
            
            // 조회할 테이블 파싱
            ETableList tableToGet = (ETableList)Enum.Parse(typeof(ETableList), query.queryMessage);
            
            // 테이블 조회 쿼리 작성 및 조회
            cmd.CommandText = $"SELECT * FROM {tableToGet}";
            MySqlDataReader reader = (MySqlDataReader)await cmd.ExecuteReaderAsync();

            // 조회 된 테이블 파싱
            ETableList table = (ETableList)Enum.Parse(typeof(ETableList), query.queryMessage);
            switch (table)
            {
                case ETableList.item_list: // 아이템 리스트 조회
                    while (reader.Read())
                    {
                        string data = $"item_list@{reader["uid"]},{reader["item"]},{reader["price"]}";
                        NetworkData readData = new NetworkData(query.data.client, ENetworkDataType.Get, data);
                        _sendDataList.Add(readData);
                    }

                    foreach (var data in _sendDataList)
                    {
                        GameServer.SendData.Enqueue(data);
                    }

                    break;

                case ETableList.userinfo: // 유저 정보 조회
                    while (reader.Read())
                    {
                        string data = $"user_info@{reader["uid"]},{reader["cash"]}";
                        NetworkData readData = new NetworkData(query.data.client, ENetworkDataType.Get, data);
                        _sendDataList.Add(readData);
                    }

                    foreach (var data in _sendDataList)
                    {
                        GameServer.SendData.Enqueue(data);
                    }
                    break;
                
                case ETableList.log: // 로그 조회
                    while (reader.Read())
                    {
                        string data = $"log@{reader["message"]}";
                        NetworkData readData = new NetworkData(query.data.client, ENetworkDataType.Get, data);
                        _sendDataList.Add(readData);
                    }

                    foreach (var data in _sendDataList)
                    {
                        Log.PrintToServer(data.data);
                    }

                    break;
                default:
                    Log.PrintToServer("Invalid Query " + query.queryMessage);
                    break;
            }

            reader.Close();
        }
        catch (Exception e)
        {
            Log.PrintToServer(e.Message);
        }
        finally
        {
            _sendDataList.Clear();
            
            // 테이블 언락
            cmd.CommandText = "UNLOCK TABLES";
            await cmd.ExecuteNonQueryAsync();
        }
    }

    /// <summary>
    /// 로그 저장 트랜잭션
    /// </summary>
    public static async Task AddLogTransaction(MySqlConnection conn, Query query)
    {
        MySqlCommand cmd = conn.CreateCommand();
        try
        {
            // 테이블 락
            cmd.CommandText = "LOCK TABLES log WRITE";
            await cmd.ExecuteNonQueryAsync();

            // 로그 저장
            cmd.CommandText = "INSERT INTO log (message) values(@message)";
            cmd.Parameters.AddWithValue("@message", query.queryMessage);
            cmd.ExecuteNonQuery();
        }
        catch (Exception e)
        {
            Log.PrintToServer(e.Message);
        }
        finally
        {
            // 테이블 언락
            cmd.CommandText = "UNLOCK TABLES";
            await cmd.ExecuteNonQueryAsync();
        }
    }

    /// <summary>
    /// 로그인 트랜잭션
    /// </summary>
    public static async Task LoginTransaction(MySqlConnection conn, Query query)
    {
        MySqlCommand cmd = conn.CreateCommand();
        NetworkData sendData = null;

        try
        {
            // 테이블 락
            cmd.CommandText = "LOCK TABLES userinfo READ";
            await cmd.ExecuteNonQueryAsync();

            // 로그인 요청자 정보 조회
            cmd.CommandText = "SELECT * FROM userinfo WHERE uid = @userId";
            cmd.Parameters.AddWithValue("@userId", query.queryMessage);
            MySqlDataReader reader = (MySqlDataReader)await cmd.ExecuteReaderAsync();

            // 로그인 요청자 정보 파싱
            string uid = string.Empty;
            int cash = 0;

            while (reader.Read())
            {
                uid = (string)reader["uid"];
                cash = (int)reader["cash"];
                string data = $"user_info@{uid},{cash}";
                sendData = new NetworkData(query.data.client, ENetworkDataType.Login, data);
            }

            // 로그인 수락 후 저장
            await GameServer.AddConnectUser(uid, cash);
        }
        catch (Exception e)
        {
            Log.PrintToServer(e.Message);
        }
        finally
        {
            // 테이블 언락
            cmd.CommandText = "UNLOCK TABLES";
            await cmd.ExecuteNonQueryAsync();
        }
        
        GameServer.SendData.Enqueue(sendData);
    }
}