using Google.Protobuf.WellKnownTypes;
using Microsoft.Data.Sqlite;
using System;
using System.IO;
using System.Threading.Tasks;

namespace DevelopersHub.RealtimeNetworking.Server
{
    class Sqlite
    {

        public static SqliteConnection connection
        {
            get
            {
                return new SqliteConnection("Data Source = " + Terminal.sqlite_database_file_path + "; Pooling = True;");
            }
        }

        public static async void Initialize()
        {
            bool created = await CreateDatabase();
            if(created)
            {
                CreateTables();
            }
            ResetAccountsIndex();
            // Test("A");
            // Test("B");
            // Test("C");
            // Test("D");
            // Test("E");
            // Test("F");
            // Test("G");
            // Test("H");
            // Test("I");
            // Test("J");
        }

        /*
        private static void Test(string id)
        {
            Task task = Task.Run(() =>
            {
                int count = 0;
                while (true)
                {
                    using (var _connection = connection)
                    {
                        long id = 1;
                        _connection.Open();
                        using (var command = _connection.CreateCommand())
                        {
                            command.CommandText = string.Format(@"INSERT INTO accounts (username, password) VALUES('{0}', '{1}'); SELECT LAST_INSERT_ROWID();", "whatever", "whatever");
                            id = Convert.ToInt64(command.ExecuteScalar());
                        }
                        using (var command = _connection.CreateCommand())
                        {
                            command.CommandText = string.Format(@"SELECT username, client_index, coins, score, level, xp, login_time FROM accounts WHERE id = {0};", id);
                            using (var reader = command.ExecuteReader())
                            {
                                if (reader.HasRows)
                                {
                                    while (reader.Read())
                                    {
                                        Data.PlayerProfile profile = new Data.PlayerProfile();
                                        profile.id = id;
                                        profile.username = reader.GetString("username");
                                        profile.online = reader.GetInt32("client_index") >= 0;
                                        profile.coins = reader.GetInt32("coins");
                                        profile.score = reader.GetInt32("score");
                                        profile.level = reader.GetInt32("level");
                                        profile.xp = reader.GetInt32("xp");
                                        profile.login = reader.GetDateTime("login_time");
                                        break;
                                    }
                                }
                            }
                        }
                        _connection.Close();
                    }
                    count++;
                    Console.WriteLine(id + " " + count.ToString());
                    if (count >= 100)
                    {
                        break;
                    }
                }
            });
        }
        */

        private static void ResetAccountsIndex()
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
                    if (!File.Exists(Terminal.sqlite_database_file_path))
                    {
                        string directory = Path.GetDirectoryName(Terminal.sqlite_database_file_path);
                        if (!Directory.Exists(directory))
                        {
                            Directory.CreateDirectory(directory);
                        }
                        FileStream fileStream = File.Create(Terminal.sqlite_database_file_path);
                        return true;
                    }
                    else
                    {
                        // TODO: Validate the file
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
                        device_id VARCHAR(500) DEFAULT '',
                        ip_address VARCHAR(50) DEFAULT '0.0.0.0',
                        client_index INTEGER DEFAULT -1,
                        coins INTEGER DEFAULT 0,
                        score INTEGER DEFAULT 0,
                        level INTEGER DEFAULT 1,
                        xp INTEGER DEFAULT 0,
                        login_time DATETIME DEFAULT CURRENT_TIMESTAMP,
                        banned INTEGER DEFAULT 0,
                        ban_reason INTEGER DEFAULT 0
                        )";
                command.ExecuteNonQuery();

                command.CommandText = @"
                        Create Table characters (
                        id INTEGER PRIMARY KEY AUTOINCREMENT,
                        account_id INTEGER DEFAULT 0,
                        selected INTEGER DEFAULT 0,
                        prefab_id INTEGER DEFAULT 0,
                        xp INTEGER DEFAULT 0,
                        level INTEGER DEFAULT 1,
                        health REAL DEFAULT 100,
                        speed REAL DEFAULT 0,
                        damage REAL DEFAULT 0,
                        strength INTEGER DEFAULT 0,
                        agility INTEGER DEFAULT 0,
                        constitution INTEGER DEFAULT 0,
                        dexterity INTEGER DEFAULT 0,
                        vitality INTEGER DEFAULT 0,
                        endurance INTEGER DEFAULT 0,
                        intelligence INTEGER DEFAULT 0,
                        wisdom INTEGER DEFAULT 0,
                        charisma INTEGER DEFAULT 0,
                        perception INTEGER DEFAULT 0,
                        luck INTEGER DEFAULT 0,
                        willpower INTEGER DEFAULT 0,
                        default_name VARCHAR(50) DEFAULT 'No Name',
                        tag VARCHAR(50) DEFAULT '',
                        custom_name VARCHAR(50) DEFAULT ''
                        )";
                command.ExecuteNonQuery();

                command.CommandText = @"
                        Create Table equipments (
                        id INTEGER PRIMARY KEY AUTOINCREMENT,
                        account_id INTEGER DEFAULT 0,
                        character_id INTEGER DEFAULT 0,
                        prefab_id INTEGER DEFAULT 0,
                        type INTEGER DEFAULT 0,
                        level INTEGER DEFAULT 1,
                        armor REAL DEFAULT 0,
                        speed REAL DEFAULT 0,
                        damage REAL DEFAULT 0,
                        range REAL DEFAULT 0,
                        weight REAL DEFAULT 0,
                        accuracy REAL DEFAULT 0,
                        capacity INTEGER DEFAULT 0,
                        default_name VARCHAR(50) DEFAULT 'No Name',
                        tag VARCHAR(50) DEFAULT '',
                        custom_name VARCHAR(50) DEFAULT ''
                        )";
                command.ExecuteNonQuery();

                command.CommandText = @"
                        Create Table friends (
                        id INTEGER PRIMARY KEY AUTOINCREMENT,
                        account_id_1 INTEGER DEFAULT 0,
                        account_id_2 INTEGER DEFAULT 0,
                        status INTEGER DEFAULT 0,
                        action_time DATETIME DEFAULT CURRENT_TIMESTAMP
                        )";
                command.ExecuteNonQuery();

                command.CommandText = @"
                        Create Table user_blocking (
                        id INTEGER PRIMARY KEY AUTOINCREMENT,
                        blocker_id INTEGER DEFAULT 0,
                        blocked_id INTEGER DEFAULT 0,
                        reason INTEGER DEFAULT 0,
                        action_time DATETIME DEFAULT CURRENT_TIMESTAMP
                        )";
                command.ExecuteNonQuery();

                command.CommandText = @"
                        Create Table ip_banning (
                        id INTEGER PRIMARY KEY AUTOINCREMENT,
                        ip_address VARCHAR(50) DEFAULT '0.0.0.0',
                        reason INTEGER DEFAULT 0,
                        action_time DATETIME DEFAULT CURRENT_TIMESTAMP
                        )";
                command.ExecuteNonQuery();

                _connection.Close();
            }
        }

    }
}
