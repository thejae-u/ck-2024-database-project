using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public static class Log
{
    /// <summary>
    /// DB 저장 로그
    /// </summary>
    public static void PrintToDB(string message)
    {
        Query query = new Query(EQueryType.Log, message);
        DatabaseHandler.EnqueueQuery(query);

        Console.WriteLine($"[{DateTime.Now}] {message} saved in DB");
    }

    /// <summary>
    /// 일반 서버 출력 로그
    /// </summary>
    public static void PrintToServer(string message)
    {
        Console.WriteLine($"[{DateTime.Now}] {message}");
    }
}
