using System;
using System.Net;
using MySql.Data.MySqlClient;

public static class DatabaseTransactions
{
    
    public static void ExecuteExchangeTransaction(MySqlConnection conn, Query query)
    {
        MySqlCommand cmd = conn.CreateCommand();
        MySqlTransaction tr = conn.BeginTransaction();        

        cmd.Connection = conn;
        cmd.Transaction = tr;

        // Split Query message
        int i = 0;
        string sellUserId = string.Empty;

        while (query.queryMessage[i] != '@')
        {
            sellUserId += query.queryMessage[i++];
        }

        string itemName = query.queryMessage.Remove(0, i + 1);

        try
        {
            // 요청자 잔액 확인
            cmd.CommandText = $"SELECT cash FROM userinfo WHERE uid = @user_id_buy";
            cmd.Parameters.AddWithValue("@user_id_buy", query.requestUserId);

            int buyUserCash = (int)cmd.ExecuteScalar();

            cmd.CommandText = $"SELECT price FROM item_list WHERE item = @item_name AND uid = @user_id_sell";
            cmd.Parameters.AddWithValue("@item_name", itemName);
            cmd.Parameters.AddWithValue("@user_id_sell", sellUserId);

            int sellPrice = (int)cmd.ExecuteScalar();

            if(buyUserCash > sellPrice)
            {
                int buyUserCashAfterExchange = buyUserCash - sellPrice;
                cmd.CommandText = $"UPDATE userinfo SET cash = @after_cash WHERE uid = @user_id_buy";
                cmd.Parameters.AddWithValue("@after_cash", buyUserCashAfterExchange);

                cmd.ExecuteNonQuery();

                cmd.CommandText = $"UPDATE userinfo SET cash = cash + @sellPrice WHERE uid = @user_id_sell";
                cmd.Parameters.AddWithValue("@sellPrice", sellPrice);

                cmd.ExecuteNonQuery();

                tr.Commit();
                Log.PrintToDB($"Exchange Success {itemName} - Buy : {query.requestUserId}, Sell : {sellUserId}, Price : {sellPrice}");
            }
            else
            {
                tr.Rollback();
            }

        }
        catch(Exception e) 
        {
            tr.Rollback();
            Log.PrintToServer(e.Message);
        }
    }
}