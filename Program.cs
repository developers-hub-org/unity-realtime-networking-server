using System;
using System.Threading;

namespace DevelopersHub.RealtimeNetworking.Server
{
    class Program
    {

        private static bool isRunning = false;
        private const float updatePeriod = 1000f / Terminal.updates_per_second;

        static void Main(string[] args)
        {
            AppDomain.CurrentDomain.ProcessExit += new EventHandler(OnExit);
            if (Manager.enabled)
            {
                Manager.Initialize();
            }
            AppDomain.CurrentDomain.UnhandledException += GlobalUnhandledExceptionHandler;
            try
            {
                Console.Title = "Server Console";
                isRunning = true;
                Thread mainThread = new Thread(new ThreadStart(MainThread));
                mainThread.Start();
                Server.Start(Terminal.max_players, Terminal.port);
            }
            catch (Exception ex)
            {
                Tools.LogError(ex.Message, ex.StackTrace);
            }
        }

        private static void MainThread()
        {
            DateTime nextLoop = DateTime.Now;
            while (isRunning)
            {
                while (nextLoop < DateTime.Now)
                {
                    Terminal.Update();
                    if (Manager.enabled)
                    {
                        Manager.Update();
                    }
                    Threading.UpdateMain();
                    nextLoop = nextLoop.AddMilliseconds(updatePeriod);
                    if (nextLoop > DateTime.Now)
                    {
                        Thread.Sleep((int)Math.Clamp((nextLoop - DateTime.Now).TotalMilliseconds, 0, Int32.MaxValue));
                    }
                }
            }
        }

        private static void GlobalUnhandledExceptionHandler(object sender, UnhandledExceptionEventArgs e)
        {
            Exception ex = default(Exception);
            ex = (Exception)e.ExceptionObject;
            Tools.LogError(ex.Message, ex.StackTrace, "Unhandled");
        }

        private static void OnExit(object sender, EventArgs e)
        {
            if (Manager.enabled)
            {
                Manager.OnExit();
            }
        }
    }
}