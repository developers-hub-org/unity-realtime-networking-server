namespace DevelopersHub.RealtimeNetworking.Server
{
    using System;
    using System.Collections.Generic;

    public class Data
    {

        public class Player
        {
            public long id = 0;
            public string username = string.Empty;
            public bool online = false;
            public int client = 0;
            public bool ready = false;
            public int team = 0;
            public int scene = -1;
        }

        public class PlayerProfile
        {
            public long id = 0;
            public string username = string.Empty;
            public int coins = 0;
            public bool online = false;
            public DateTime login;
        }

        public class Party
        {
            public string id = string.Empty;
            public bool auto = false;
            public int gameID = 0;
            public int mapID = 0;
            public long leaderID = 0;
            public int maxPlayers = 100;
            public Extension extension = Extension.NONE;
            public bool matchmaking = false;
            public int teamsPerMatch = 2;
            public int playersPerTeam = 6;
            public List<Player> players = new List<Player>();
            public HashSet<long> invites = new HashSet<long>();
        }

        public class FriendRequest
        {
            public long id = 0;
            public long playerID = 0;
            public string username = string.Empty;
            public bool online = false;
            public DateTime time;
        }

        public class Room
        {
            public string id = string.Empty;
            public int gameID = 0;
            public int mapID = 0;
            public long hostID = 0;
            public string hostUsername = string.Empty;
            public string password = string.Empty;
            public int maxPlayers = 0;
            public List<Player> players = new List<Player>();
        }

        public class Friend
        {
            public long id = 0;
            public string username = string.Empty;
            public bool online = false;
        }

        public enum GameType
        {
            HOSTED = 1, MATCHED = 2
        }

        public class Game
        {
            public Data.Room room = null;
            public DateTime start;
            public GameType type = GameType.HOSTED;
            public Extension extension = Extension.NONE;
            public List<int> sceneHostsKeys = new List<int>();
            public List<long> sceneHostsValues = new List<long>();
        }

        public enum Extension
        {
            NONE = 0, NETCODE_SERVER = 1
        }

        public class RuntimeEquipment
        {
            public string name = string.Empty;
        }

        public class RuntimeCharacter
        {
            public string name = string.Empty;
            public int health = 100;
            public int totalDamageDealt = 0;
            public int kills = 0;
            public int assists = 0;
            public int deaths = 0;
            public List<RuntimeEquipment> equipments = new List<RuntimeEquipment>();
        }

        public class RuntimePlayer
        {
            public long id = 0;
            public string username = string.Empty;
            public int team = 0;
            public List<RuntimeCharacter> characters = new List<RuntimeCharacter>();
        }

        public class RuntimeGame
        {
            public string id = string.Empty;
            public int gameID = 0;
            public int mapID = 0;
            public List<RuntimePlayer> players = new List<RuntimePlayer>();
        }

        public class RuntimeResult
        {
            public double duration = 0;
            public RuntimeGame game = null;
        }

    }
}