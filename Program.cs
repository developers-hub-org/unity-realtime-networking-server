using System;
using System.Threading;

namespace DevelopersHub.RealtimeNetworking.Server
{
    class Program
    {

        private static bool isRunning = false;
        private const float updatePeriod = 1000f / Terminal.updatesPerSecond;

        static void Main(string[] args)
        {
            Console.Title = "Server Console";
            isRunning = true;
            Thread mainThread = new Thread(new ThreadStart(MainThread));
            mainThread.Start();
            Server.Start(Terminal.maxPlayers, Terminal.port);
        }

        private static void MainThread()
        {
            DateTime nextLoop = DateTime.Now;
            while (isRunning)
            {
                while (nextLoop < DateTime.Now)
                {
                    Terminal.Update();
                    Threading.UpdateMain();
                    nextLoop = nextLoop.AddMilliseconds(updatePeriod);
                    if (nextLoop > DateTime.Now)
                    {
                        Thread.Sleep(nextLoop - DateTime.Now);
                    }
                }
            }
        }

    }
}