using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using System.Configuration;

namespace APSIM.MSPlugins.EventServer
{
    internal static class CommandProcessor
    {
        public static void Process(Cmd cmd)
        {
            //from milestone to client
            if (cmd.Name == "command")
            {
                SocketServer.Broadcast(String.Join(" ", cmd.Args));
                return;
            }
            
            // from client to milestone
            if (cmd.Name == "event")
            {
               // Byte[] bytes = Encoding.UTF8.GetBytes(String.Join(" ", cmd.Args));
              //  cmd.Stream.Write(bytes, 0, bytes.Length);
                return;
            }


        }
    }
}


