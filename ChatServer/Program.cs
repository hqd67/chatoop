using System;

namespace ChatServer
{
    class Program
    {
        static void Main(string[] args)
        {
            int port = 9000;
            if (args.Length >= 1) int.TryParse(args[0], out port);

            var server = new Server(port);
            server.Start();

            Console.WriteLine("Press ENTER to stop server...");
            Console.ReadLine();
        }
    }
}
