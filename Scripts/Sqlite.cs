﻿using Microsoft.Data.Sqlite;
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
                return new SqliteConnection("Data Source = " + Terminal.sqlite_database_file_path + "");
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
                        health INTEGER DEFAULT 100,
                        speed INTEGER DEFAULT 1,
                        damage INTEGER DEFAULT 20,
                        default_name VARCHAR(50) DEFAULT 'No Name',
                        custom_name VARCHAR(50) DEFAULT ''
                        )";
                command.ExecuteNonQuery();

                command.CommandText = @"
                        Create Table equipments (
                        id INTEGER PRIMARY KEY AUTOINCREMENT,
                        account_id INTEGER DEFAULT 0,
                        character_id INTEGER DEFAULT 0,
                        prefab_id INTEGER DEFAULT 0,
                        level INTEGER DEFAULT 1,
                        armor INTEGER DEFAULT 10,
                        speed INTEGER DEFAULT 1,
                        damage INTEGER DEFAULT 20,
                        range INTEGER DEFAULT 100,
                        capacity INTEGER DEFAULT 1,
                        default_name VARCHAR(50) DEFAULT 'No Name',
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
