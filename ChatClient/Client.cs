using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

public class Client
{
    private TcpClient tcp;
    private StreamReader reader;
    private StreamWriter writer;

    public string UserName { get; private set; }

    public event Action<Message> OnMessageReceived;

    public async Task<bool> ConnectAsync(string ip, int port, string user)
    {
        try
        {
            tcp = new TcpClient();
            await tcp.ConnectAsync(ip, port);

            var stream = tcp.GetStream();
            reader = new StreamReader(stream, Encoding.UTF8);
            writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true };

            UserName = user;

            await SendAsync(new Message
            {
                Type = "connect",
                Sender = user
            });

            _ = Task.Run(ReceiveLoopAsync);

            return true;
        }
        catch
        {
            return false;
        }
    }

    private async Task ReceiveLoopAsync()
    {
        while (true)
        {
            string line = await reader.ReadLineAsync();
            if (line == null) break;

            Message msg = JsonConvert.DeserializeObject<Message>(line);
            OnMessageReceived?.Invoke(msg);
        }
    }

    public Task SendAsync(Message msg)
    {
        string json = JsonConvert.SerializeObject(msg);
        return writer.WriteLineAsync(json);
    }
    public Task SendTextAsync(string text)
    {
        return SendAsync(new Message
        {
            Type = "message",
            Sender = UserName,
            Text = text,
            Timestamp = DateTime.Now
        });
    }
    public Task SendFileAsync(string path)
    {
        byte[] bytes = File.ReadAllBytes(path);

        return SendAsync(new Message
        {
            Type = "file",
            Sender = UserName,
            FileName = Path.GetFileName(path),
            FileSize = bytes.Length,
            FileData = Convert.ToBase64String(bytes),
            Timestamp = DateTime.Now
        });
    }

}
