using Microsoft.Data.Sqlite;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace DevelopersHub.RealtimeNetworking.Server
{
    class Sqlite
    {

        private const string _sqliteDatabasePath = @"C:\Database\realtime_networking.db";

        public static async void Initialize()
        {
            bool created = await CreateDatabase();
            CreateTables();
        }

        private async static Task<bool> CreateDatabase()
        {
            Task<bool> task = Task.Run(() =>
            {
                try
                {
                    if (!File.Exists(_sqliteDatabasePath))
                    {
                        string directory = Path.GetDirectoryName(_sqliteDatabasePath);
                        if (!Directory.Exists(directory))
                        {
                            Directory.CreateDirectory(directory);
                        }
                        FileStream fileStream = File.Create(_sqliteDatabasePath);
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message + "\n" + ex.StackTrace);
                }
                return false;
            });
            return await task;
        }

        private static void CreateTables()
        {
            using (var connection = new SqliteConnection("Data Source = " + _sqliteDatabasePath + ""))
            {
                connection.Open();
                var command = connection.CreateCommand();
                command.CommandText = @"
                        Create Table accounts (
                        id INTEGER PRIMARY KEY AUTOINCREMENT,
                        username VARCHAR(30)
                        )";
                command.ExecuteNonQuery();
                connection.Close();
            }
        }

    }
}
