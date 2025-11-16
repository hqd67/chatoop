using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace ChatClient
{
    public class Client
    {
        private TcpClient _tcp;
        private StreamReader _reader;
        private StreamWriter _writer;

        public string UserName { get; private set; }

        public event Action<ChatMessage> OnMessageReceived;
        public event Action<string[]> OnUserListUpdated;

        public async Task<bool> ConnectAsync(string ip, int port, string userName)
        {
            try
            {
                _tcp = new TcpClient();
                await _tcp.ConnectAsync(ip, port);

                var stream = _tcp.GetStream();
                _reader = new StreamReader(stream, Encoding.UTF8);
                _writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true };

                UserName = userName;

                var hello = new ChatMessage { Type = "connect", Sender = userName, Text = "" };
                string json = JsonConvert.SerializeObject(hello);
                await _writer.WriteLineAsync(json);

                Task.Run(ReceiveLoopAsync);

                return true;
            }
            catch
            {
                return false;
            }
        }

        private async Task ReceiveLoopAsync()
        {
            try
            {
                while (true)
                {
                    string line = await _reader.ReadLineAsync();
                    if (line == null) break;

                    ChatMessage msg = null;
                    try { msg = JsonConvert.DeserializeObject<ChatMessage>(line); } catch { }

                    if (msg == null) continue;

                    if (msg.Type == "userlist")
                    {
                        var arr = string.IsNullOrEmpty(msg.Text) ? new string[0] : msg.Text.Split('|');
                        OnUserListUpdated?.Invoke(arr);
                    }
                    else
                    {
                        OnMessageReceived?.Invoke(msg);
                    }
                }
            }
            catch
            {
            }
        }

        public async Task SendTextMessageAsync(string text, string recipient)
        {
            if (_writer == null) return;

            var msg = new ChatMessage
            {
                Type = "message",
                Sender = UserName,
                Recipient = recipient,
                Text = text,
                Timestamp = DateTime.UtcNow
            };

            string json = JsonConvert.SerializeObject(msg);
            await _writer.WriteLineAsync(json);
        }

        public async Task SendFileAsync(string filePath, string recipient)
        {
            if (_writer == null) return;

            byte[] bytes = File.ReadAllBytes(filePath);
            string base64 = Convert.ToBase64String(bytes);

            var msg = new ChatMessage
            {
                Type = "file",
                Sender = UserName,
                Recipient = recipient,
                FileName = Path.GetFileName(filePath),
                FileSize = bytes.Length,
                FileData = base64,
                Timestamp = DateTime.UtcNow
            };

            string json = JsonConvert.SerializeObject(msg);
            await _writer.WriteLineAsync(json);
        }

        public void Disconnect()
        {
            try { _tcp?.Close(); } catch { }
        }
    }
}
