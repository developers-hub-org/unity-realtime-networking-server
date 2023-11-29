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

        public static (int, int) OverrideMatchmaking(int gameID, int mapID)
        {
            int teamsPerMatch = 2;
            int playersPerTeam = 6;
            // --->
            // Add your custom game conditions here, for example:
            if (gameID == 1)
            {
                teamsPerMatch = 2;
                playersPerTeam = 1;
            }
            else if (gameID == 2)
            {
                teamsPerMatch = 2;
                playersPerTeam = 100;
            }
            // <---
            return (teamsPerMatch, playersPerTeam);
        }

        public static (Data.PurchaseResult, int) OverridePurchase(long accountID, int itemCategory, int itemID, int itemLevel, int currencyID, Microsoft.Data.Sqlite.SqliteConnection connection)
        {
            int price = 99999;
            Data.PurchaseResult result = Data.PurchaseResult.Unknown;

            /*
            if(itemCategory == whatever && itemID == whatever && itemLevel == whatever)
            {
                price = 12345;
            }

            int haveCurrency = 0;
            if(currencyID == whatever)
            {
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = string.Format(@"SELECT coins FROM accounts WHERE id = {0};", accountID);
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.HasRows)
                        {
                            while (reader.Read())
                            {
                                haveCoins = reader.GetInt32("coins");
                                break;
                            }
                        }
                    }
                }
            }
            if(haveCurrency >= price)
            {
                if (currencyID == whatever)
                {
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = string.Format(@"UPDATE accounts SET coins = coins - {0} WHERE id = {1};", price, accountID);
                        command.ExecuteNonQuery();
                    }
                }

                // Add item here

            }
            else
            {
                result = Data.PurchaseResult.InsufficientFunds;
            }
            */

            return (result, price);
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