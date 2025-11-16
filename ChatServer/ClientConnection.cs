using System.Net.Sockets;
using System.IO;
using System.Text;

namespace ChatServer
{
    public class ClientConnection
    {
        public TcpClient Tcp { get; private set; }
        public string Username { get; set; }
        public StreamReader Reader { get; private set; }
        public StreamWriter Writer { get; private set; }

        public ClientConnection(TcpClient tcp)
        {
            Tcp = tcp;
            var stream = tcp.GetStream();
            Reader = new StreamReader(stream, Encoding.UTF8);
            Writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true };
            Username = "Unknown";
        }
    }
}
