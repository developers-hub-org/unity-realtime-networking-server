using System;

namespace DevelopersHub.RealtimeNetworking.Server
{
    class Terminal
    {

        public const int updatesPerSecond = 30;
        public const int maxPlayers = 100000;
        public const int port = 5555;
        public const bool useInternalManager = true;
        public const string sqliteDatabasePath = @"C:\Database\realtime_networking.db";
        public const string logFolderPath = @"C:\Log\realtime_networking\";

        public static void Start()
        {
            Console.WriteLine("Server Started.");
        }

        public static void Update()
        {
            
        }

        public static void OnClientConnected(int id, string ip)
        {
   
        }

        public static void OnClientDisconnected(int id, string ip)
        {

        }

        public static void ReceivedPacket(int clientID, Packet packet)
        {

        }

    }
}