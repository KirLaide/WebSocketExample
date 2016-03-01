using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Security.Cryptography;

namespace APSIM.MSPlugins.EventServer
{
    class Program
    {
        static void Main(string[] args)
        {
            SocketServer.Run();

            CommandQueue<Cmd>.Instance.dlgtItemEnqueued = new CommandQueue<Cmd>.ItemEnqueuedDelegate(WorkerThread.ProcessNow);

            // thread for processing commands from milestone
            WorkerThread.StartThread(
                // Periodic processing
                (ob, setEvent) =>
                {
                    // Command processing
                    Cmd cmd;
                    while ((cmd = CommandQueue<Cmd>.Instance.Dequeue()) != null) // obtain command
                    {
                        // Process command
                        CommandProcessor.Process(cmd);
                    }
                }, null,
                // Exception callback
                e =>
                {
                    Console.WriteLine(e.Message);
                });
            Console.ReadLine();
            CommandQueue<Cmd>.Instance.Enqueue(new Cmd() { Name = "command", Args = new string[] {"respomn", "tets"} });
            Console.ReadLine();
            SocketServer.Stop();
            WorkerThread.StopThread();
        }
    }
}
