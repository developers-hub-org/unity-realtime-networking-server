using System;
using System.Numerics;

namespace DevelopersHub.RealtimeNetworking.Server
{
    class Terminal
    {

        public const int updatesPerSecond = 30;
        public const bool autoManage = true;
        public const int maxPlayers = 100000;
        public const int port = 5555;

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