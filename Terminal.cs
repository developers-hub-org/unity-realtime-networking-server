using System;

namespace DevelopersHub.RealtimeNetworking.Server
{
    class Terminal
    {

        public const int port = 5555;
        public const int updates_per_second = 30;
        public const int max_players = 100000;
        public const string sqlite_database_file_path = @"C:\Database\realtime_networking.db";
        public const string log_directory_path = @"C:\Log\realtime_networking\";

        #region Extensions
        public const string netcode_server_executable_path = @"C:\Users\Test\Desktop\Server\Netcode.exe";
        public const int netcode_max_server_life_seconds = 21600;
        #endregion

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

        public static void OnAuthenticated(long accountID, bool wasSignedUp, Microsoft.Data.Sqlite.SqliteConnection connection)
        {

        }

        public static (Data.PurchaseResult, int) OverridePurchase(long accountID, int itemCategory, int itemID, int itemLevel, int currencyID, Microsoft.Data.Sqlite.SqliteConnection connection)
        {
            uint price = 99999;
            Data.PurchaseResult result = Data.PurchaseResult.Unknown;

            /*
            bool purchased = false;
            if (itemCategory == weapon && itemID == excalibur && itemLevel == 1)
            {
                if(currencyID == coins)
                {
                    price = 12345;
                    purchased = Manager.SpendCoins(accountID, price);
                }
            }
            if(purchased)
            {
                // Add item here. For example:
                Data.RuntimeEquipment sword = new Data.RuntimeEquipment();
                sword.name = "Excalibur";
                sword.tag = "excalibur";
                sword.prefabID = 0;
                sword.weight = 4.8;
                sword.damage = 25;
                sword.level = itemLevel;
                Manager.CreateEquipment(accountID, 0, sword);
            }
            else
            {
                result = Data.PurchaseResult.InsufficientFunds;
            }
            */

            return (result, (int)price);
        }

        public static (int, int) OverrideMatchmaking(int gameID, int mapID)
        {
            int teamsPerMatch = 2;
            int playersPerTeam = 6;
            // --->
            // Add your custom game conditions here, for example:
            if (gameID == 1)
            {
                teamsPerMatch = 10;
                playersPerTeam = 10;
            }
            else if (gameID == 2)
            {
                teamsPerMatch = 2;
                playersPerTeam = 100;
            }
            // <---
            return (teamsPerMatch, playersPerTeam);
        }

        public static void OverrideGameInitialData(ref Data.RuntimeGame data, Microsoft.Data.Sqlite.SqliteConnection connection)
        {

        }

        public static void OnGameFinished(Data.Game game)
        {

        }

        public static void OnNetcodeGameResultReceived(Data.RuntimeResult result)
        {

        }

    }
}