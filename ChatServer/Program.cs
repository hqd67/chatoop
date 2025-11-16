using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

class Program
{
    static ChatServer server = new ChatServer();

    static void Main()
    {
        server.Start(9000);
        Console.WriteLine("Сервер запущен на порту 9000");
        Console.ReadLine();
    }
}

public class ChatServer
{
    private TcpListener listener;

    public void Start(int port)
    {
        listener = new TcpListener(IPAddress.Any, port);
        listener.Start();

        Task.Run(AcceptLoopAsync);
    }

    private async Task AcceptLoopAsync()
    {
        while (true)
        {
            TcpClient client = await listener.AcceptTcpClientAsync();
            Console.WriteLine("Клиент подключён!");
        }
    }
}
