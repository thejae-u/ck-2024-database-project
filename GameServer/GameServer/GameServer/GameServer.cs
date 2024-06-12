using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

class GameSerever
{
    const int PORT = 56000;
    const int BUFFER_SIZE = 1024;

    static async Task Main(string[] args)
    {
        TcpListener server = new TcpListener(IPAddress.Any, PORT);
        server.Start();
        Console.WriteLine("Server listening on port " + PORT);

        while (true)
        {
            TcpClient client = await server.AcceptTcpClientAsync();
            Console.WriteLine("Client connected.");
            HandleClientAsync(client);
        }
    }

    static async void HandleClientAsync(TcpClient client)
    {
        NetworkStream stream = client.GetStream();
        byte[] buffer = new byte[BUFFER_SIZE];

        try
        {
            while (true)
            {
                int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                if (bytesRead == 0)
                {
                    Console.WriteLine("Client disconnected.");
                    break;
                }

                string receivedMessage = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                Console.WriteLine("Received: " + receivedMessage);

                byte[] responseMessage = Encoding.UTF8.GetBytes(receivedMessage);
                await stream.WriteAsync(responseMessage, 0, responseMessage.Length);
                Console.WriteLine("Sent: " + receivedMessage);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Exception: " + ex.Message);
        }
        finally
        {
            stream.Close();
            client.Close();
        }
    }
}

