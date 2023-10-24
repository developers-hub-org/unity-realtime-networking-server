using System;

namespace DevelopersHub.RealtimeNetworking.Server
{
    class Terminal
    {

        public const int port = 5555;
        public const int updates_per_second = 30;
        public const int max_players = 100000;
        public const string sqlite_database_path = @"C:\Database\realtime_networking.db";
        public const string log_directory_path = @"C:\Log\realtime_networking\";

        public static void Start()
        {
            Console.WriteLine("Server Started.");
        }

        public static void Update()
        {
            
        }

        public static void ClientConnected(int id, string ip)
        {
   
        }

        public static void ClientDisconnected(int id, string ip)
        {

        }

        public static void PacketReceived(int clientID, Packet packet)
        {

        }

    }
}