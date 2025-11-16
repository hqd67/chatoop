using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace ChatServer
{
    public class Server
    {
        private readonly TcpListener _listener;
        private readonly List<ClientConnection> _clients = new List<ClientConnection>();
        private readonly object _clientsLock = new object();

        public Server(int port)
        {
            _listener = new TcpListener(IPAddress.Any, port);
        }

        public void Start()
        {
            _listener.Start();
            Console.WriteLine("Server started on port " + ((_listener.LocalEndpoint is IPEndPoint ie) ? ie.Port.ToString() : "?"));
            Task.Run(AcceptLoop);
        }

        private async Task AcceptLoop()
        {
            while (true)
            {
                try
                {
                    var tcp = await _listener.AcceptTcpClientAsync();
                    var conn = new ClientConnection(tcp);
                    lock (_clientsLock) { _clients.Add(conn); }
                    Console.WriteLine("New connection: " + tcp.Client.RemoteEndPoint);
                    _ = Task.Run(() => HandleClient(conn));
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Accept error: " + ex.Message);
                }
            }
        }

        private async Task HandleClient(ClientConnection conn)
        {
            try
            {
                while (true)
                {
                    string line = await conn.Reader.ReadLineAsync();
                    if (line == null) break;

                    ChatMessage msg = null;
                    try
                    {
                        msg = JsonConvert.DeserializeObject<ChatMessage>(line);
                    }
                    catch { /* ignore bad json */ }

                    if (msg == null) continue;

                    if (msg.Type == "connect")
                    {
                        conn.Username = msg.Sender ?? "Unknown";
                        Console.WriteLine(conn.Username + " connected");
                        await BroadcastAsync(new ChatMessage
                        {
                            Type = "message",
                            Sender = "SERVER",
                            Text = conn.Username + " joined",
                            Timestamp = DateTime.UtcNow
                        });
                        await SendUserListToAll();
                    }
                    else if (msg.Type == "message")
                    {
                        Console.WriteLine($"[{DateTime.UtcNow:HH:mm:ss}] {msg.Sender}: {msg.Text}");
                        await BroadcastAsync(msg, msg.Recipient);
                    }
                    else if (msg.Type == "file")
                    {
                        Console.WriteLine($"File from {msg.Sender}: {msg.FileName} ({msg.FileSize} bytes)");
                        await BroadcastAsync(msg, msg.Recipient);
                    }
                    else if (msg.Type == "disconnect")
                    {
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Client handler error: " + ex.Message);
            }
            finally
            {
                lock (_clientsLock) { _clients.Remove(conn); }
                try { conn.Tcp.Close(); } catch { }
                Console.WriteLine(conn.Username + " disconnected");
                // Inform others
                await BroadcastAsync(new ChatMessage
                {
                    Type = "message",
                    Sender = "SERVER",
                    Text = conn.Username + " left",
                    Timestamp = DateTime.UtcNow
                });
                await SendUserListToAll();
            }
        }

        private async Task BroadcastAsync(ChatMessage msg, string recipient = null)
        {
            List<ClientConnection> snapshot;
            lock (_clientsLock) { snapshot = new List<ClientConnection>(_clients); }

            string json = JsonConvert.SerializeObject(msg);

            foreach (var c in snapshot)
            {
                if (!string.IsNullOrEmpty(recipient) && c.Username != recipient) continue;
                try
                {
                    await c.Writer.WriteLineAsync(json);
                }
                catch
                {
                    // ignore per-client errors
                }
            }
        }

        private async Task SendUserListToAll()
        {
            string list;
            lock (_clientsLock) { list = string.Join("|", _clients.ConvertAll(c => c.Username)); }
            var msg = new ChatMessage
            {
                Type = "userlist",
                Text = list,
                Timestamp = DateTime.UtcNow
            };
            await BroadcastAsync(msg);
        }
    }
}
