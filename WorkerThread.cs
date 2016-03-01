using System;
using System.Threading;

namespace APSIM.MSPlugins.EventServer
{
    public class WorkerThread
    {
        public delegate void PeriodicProcessingDelegate(object ob, bool setEvent);
        public delegate void PeriodicProcessingExceptionDelegate(Exception e);

        private static Thread thread;
        private static AutoResetEvent ev = new AutoResetEvent(false);
        private static long finish = 0;

        public static void StartThread(PeriodicProcessingDelegate dlgtPeriodicProcessing, object arg,
                                       PeriodicProcessingExceptionDelegate dlgtPeriodicProcessingException)
        {
            if (thread != null || dlgtPeriodicProcessing == null || dlgtPeriodicProcessingException == null)
                    return;

            thread = new Thread(new ThreadStart(() =>
                {
                    bool bContinue = true;
                    while (bContinue)
                    {
                        bool setEvent;
                        if (setEvent = ev.WaitOne())
                            bContinue = Interlocked.Read(ref finish) != 1;

                        if (bContinue)
                        {
                            try
                            {
                                // Useful processing
                                Console.Write(".");
                                dlgtPeriodicProcessing(arg, setEvent);
                            }
                            catch (Exception e)
                            {
                                dlgtPeriodicProcessingException(e);
                            }
                        }
                    }
                }));

            thread.Start();
        }

        public static void ProcessNow()
        {
            ev.Set();
        }

        public static void StopThread()
        {
            if (thread != null)
            {
                Interlocked.Increment(ref finish);
                ev.Set();
                if (!thread.Join(3000))
                {
                    try
                    {
                        thread.Abort();
                    }
                    catch { }
                }

                thread = null;
            }
        }
    }
}
