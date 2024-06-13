using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public static class Log
{
    /// <summary>
    /// 일반로그
    /// </summary>
    public static void Print(string message)
    {
        Console.WriteLine($"{DateTime.Now} {message}");
        // TODO : DB 저장
    }
}
