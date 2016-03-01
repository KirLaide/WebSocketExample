using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace APSIM.MSPlugins.EventServer
{
    class SocketServer
    {
        private static SocketServer _instance;

        private TcpListener server;
        private List<SocketClient> _clients;
        private static CancellationTokenSource cancelaction = new CancellationTokenSource();
  
        public TcpListener Server { get { return _instance.server; } }

        public static void Run()
        {
            _instance = new SocketServer();
            _instance._clients = new List<SocketClient>();
            _instance.server = new TcpListener(IPAddress.Parse(Properties.Settings.Default.SocketIP), Properties.Settings.Default.SocketPort);
            Console.WriteLine("Server has started {0} Waiting for a connection...", Environment.NewLine);
            _instance.server.Start();
            Listen();
        }


        private async static void Listen()
        {
            while (true)
            {
                SocketClient _client;
                try
                {
                    _client = new SocketClient(await Task.Run(() => _instance.server.AcceptTcpClient(), cancelaction.Token));
                }
                catch
                {
                    return;
                }
                _instance._clients.Add(_client);
                Console.WriteLine("A client connected.");
            }
        }

        public static void DisconnectClient(SocketClient _client)
        {
            _instance._clients.Remove(_client);
            Console.WriteLine("A client disconnected.");
        }

        public static void Broadcast(string message)
        {
            byte[] response = Encoding.UTF8.GetBytes(message);
            response = SocketClient.EncodeData(response);
            foreach (SocketClient cl in _instance._clients)
            {
                cl.Send(response);
            }
        }
        public static void Stop()
        {
            foreach (SocketClient client in _instance._clients)
            {
                client.Disconnect();
            }
            _instance.server.Stop();
            cancelaction.Cancel();
        }
    }
}