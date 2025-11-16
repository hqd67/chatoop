using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

public class ClientConnection
{
    public string Username { get; set; }
    public TcpClient Tcp { get; }
    private StreamReader reader;
    private StreamWriter writer;
    private ChatServer server;

    public ClientConnection(TcpClient tcp, ChatServer server)
    {
        Tcp = tcp;
        this.server = server;

        var stream = tcp.GetStream();
        reader = new StreamReader(stream, Encoding.UTF8);
        writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true };
    }

    public async Task ListenAsync()
    {
        try
        {
            while (true)
            {
                string json = await reader.ReadLineAsync();
                if (json == null) break;

                Message msg = JsonConvert.DeserializeObject<Message>(json);

                if (msg.Type == "connect")
                {
                    Username = msg.Sender;
                    server.BroadcastAsync(new Message
                    {
                        Sender = "SERVER",
                        Text = $"{Username} подключился",
                        Timestamp = DateTime.Now
                    });
                }
                else
                {
                    await server.BroadcastAsync(msg);
                }
            }
        }
        finally
        {
            server.RemoveClient(this);
        }
    }

    public Task SendAsync(Message msg)
    {
        string json = JsonConvert.SerializeObject(msg);
        return writer.WriteLineAsync(json);
    }

    public void Disconnect()
    {
        try { Tcp.Close(); } catch { }
    }
}
