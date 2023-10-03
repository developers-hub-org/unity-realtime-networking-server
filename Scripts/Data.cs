using System;
using System.Collections.Generic;

namespace DevelopersHub.RealtimeNetworking.Server
{
    public class Data
    {

        public class Player
        {
            public long id = 0;
            public string username = string.Empty;
            public int client = 0;
            public bool ready = false;
            public int team = 0;
            public int scene = -1;
        }

        public enum RoomType
        {
            HOST_MANAGED = 1
        }

        public class Room
        {
            public string id = string.Empty;
            public RoomType type = RoomType.HOST_MANAGED;
            public bool started = false;
            public int gameID = 0;
            public long hostID = 0;
            public string hostUsername = string.Empty;
            public string password = string.Empty;
            public int maxPlayers = 100;
            public List<Player> players = new List<Player>();
            public List<int> sceneHostsKeys = new List<int>();
            public List<long> sceneHostsValues = new List<long>();
        }

    }
}