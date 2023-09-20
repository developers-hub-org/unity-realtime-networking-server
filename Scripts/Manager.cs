using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DevelopersHub.RealtimeNetworking.Server
{
    class Manager
    {

        public static void Initialize()
        { 
            Sqlite.Initialize();
        }

        public static void OnClientConnected(int id, string ip)
        {

        }

        public static void OnClientDisconnected(int id, string ip)
        {

        }

    }
}