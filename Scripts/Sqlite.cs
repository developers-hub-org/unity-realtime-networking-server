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

        public static SqliteConnection connection
        {
            get
            {
                return new SqliteConnection("Data Source = " + Terminal.sqliteDatabasePath + "");
            }
        }

        public static async void Initialize()
        {
            bool created = await CreateDatabase();
            if(created)
            {
                CreateTables();
            }
            Start();
        }

        public static void Start()
        {
            try
            {
                using (var _connection = connection)
                {
                    _connection.Open();
                    var command = _connection.CreateCommand();
                    command.CommandText = @"UPDATE accounts SET client_index = -1";
                    command.ExecuteNonQuery();
                    _connection.Close();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message + "\n" + ex.StackTrace);
            }
        }

        private async static Task<bool> CreateDatabase()
        {
            Task<bool> task = Task.Run(() =>
            {
                try
                {
                    if (!File.Exists(Terminal.sqliteDatabasePath))
                    {
                        string directory = Path.GetDirectoryName(Terminal.sqliteDatabasePath);
                        if (!Directory.Exists(directory))
                        {
                            Directory.CreateDirectory(directory);
                        }
                        FileStream fileStream = File.Create(Terminal.sqliteDatabasePath);
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
            using (var _connection = connection)
            {
                _connection.Open();
                var command = _connection.CreateCommand();
                command.CommandText = @"
                        Create Table accounts (
                        id INTEGER PRIMARY KEY AUTOINCREMENT,
                        username VARCHAR(50) DEFAULT 'Player',
                        password VARCHAR(500) DEFAULT '',
                        device_id VARCHAR(500)DEFAULT '',
                        ip_address VARCHAR(50) DEFAULT '0.0.0.0',
                        client_index INTEGER DEFAULT -1,
                        login_time DATETIME DEFAULT CURRENT_TIMESTAMP,
                        banned INTEGER DEFAULT 0
                        )";
                command.ExecuteNonQuery();
                _connection.Close();
            }
        }

    }
}
