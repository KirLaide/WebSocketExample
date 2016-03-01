using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace APSIM.MSPlugins.EventServer
{
    public class CommandQueue<T> where T : class
    {
        private Queue<T> queT = new Queue<T>();
        private object locker = new object();

        private static CommandQueue<T> helper;

        public delegate void ItemEnqueuedDelegate();
        public ItemEnqueuedDelegate dlgtItemEnqueued;

        public static CommandQueue<T> Instance
        {
            get
            {
                if (helper == null)
                    helper = new CommandQueue<T>();

                return helper;
            }
        }

        private CommandQueue()
        {
        }

        public void Enqueue(T t)
        {
            lock (locker)
            {
                queT.Enqueue(t);

                if (dlgtItemEnqueued != null)
                    dlgtItemEnqueued();
            }
        }

        public T Dequeue()
        {
            T t = null;
            lock (locker)
            {
                if (queT.Count > 0)
                    t = queT.Dequeue();
            }

            return t;
        }
    }

    public class Cmd
    {
         public string Name { get; set; }

         public string[] Args { get; set; }

         public NetworkStream Stream { get; set; }
    }
}

