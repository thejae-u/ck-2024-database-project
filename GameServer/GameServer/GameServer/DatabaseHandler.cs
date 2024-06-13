using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public enum EQueryType
{
    None,
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

    public static void Start()
    {
        _ = Task.Run(ExecuteQuery);
    }

    public static void EnqueueQuery(Query query)
    {
        queries.Enqueue(query);
    }

    private static async Task<bool> ExecuteQuery()
    {
        while (true)
        {
            if(queries.Count == 0)
            {
                await Task.Delay(100);
                continue;
            }

            // TODO : SQL 연결 후 Query 처리, Query Type 에 따라서 정해진 Query 전송
            Query query = queries.Dequeue();

            switch(query.queryType)
            {
                case EQueryType.None:
                    break;
            }
        }
    }
}