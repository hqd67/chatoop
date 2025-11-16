using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading.Tasks;
using Newtonsoft.Json;

public class ChatServer
{
    private TcpListener listener;
    private List<ClientConnection> clients = new List<ClientConnection>();

    public void Start(int port)
    {
        listener = new TcpListener(System.Net.IPAddress.Any, port);
        listener.Start();
        Task.Run(AcceptLoopAsync);
    }

    private async Task AcceptLoopAsync()
    {
        while (true)
        {
            TcpClient tcp = await listener.AcceptTcpClientAsync();

            var conn = new ClientConnection(tcp, this);
            clients.Add(conn);

            _ = Task.Run(() => conn.ListenAsync());

            Console.WriteLine("Клиент подключён");
        }
    }

    public async Task BroadcastAsync(Message msg)
    {
        foreach (var c in clients)
        {
            await c.SendAsync(msg);
        }
    }

    public void RemoveClient(ClientConnection client)
    {
        clients.Remove(client);

        BroadcastAsync(new Message
        {
            Sender = "SERVER",
            Text = $"{client.Username} вышел",
            Timestamp = DateTime.Now
        });
    }
}
