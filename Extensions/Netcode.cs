using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace DevelopersHub.RealtimeNetworking.Server
{
    class Netcode
    {

        private const string server_executable_path = @"C:\Users\Test\Desktop\Server\Netcode.exe";

        public static void Update()
        {
            if (!updating)
            {
                updating = true;
                _Update();
            }
        }

        private static bool updating = false;
        private static List<Game> games = new List<Game>();

        private class Game
        {
            public string id = string.Empty;
            public Data.Game game = null;
            public Process process = null;
            public int port = 7777;
        }

        private static string tempPath
        {
            get
            {
                return Path.GetTempPath().TrimEnd(Path.DirectorySeparatorChar) + string.Format("{0}RealtimeNetworking{0}Extentions{0}Netcode", Path.DirectorySeparatorChar);
            }
        }

        private static void _Update()
        {
            Task task = Task.Run(() =>
            {
                string path = string.Format("{0}Ready{1}", tempPath, Path.DirectorySeparatorChar);
                if(Directory.Exists(path)) 
                {
                    string[] files = Directory.GetFiles(path);
                    if (files != null && files.Length > 0)
                    {
                        foreach (string file in files)
                        {
                            if (Path.GetExtension(file).ToLower() == ".txt")
                            {
                                try
                                {
                                    string id = Path.GetFileNameWithoutExtension(file);
                                    int port = 7777;
                                    using (var reader = new StreamReader(file))
                                    {
                                        port = int.Parse(reader.ReadLine().Trim());
                                    }
                                    File.Delete(file);
                                    ServerIsReady(id, port);
                                }
                                catch (Exception)
                                {

                                }
                            }
                        }
                    }
                }
                updating = false;
            });
        }

        private static void ServerIsReady(string id, int port)
        {
            for (int g = 0; g < games.Count; g++)
            {
                if (games[g].id == id)
                {
                    games[g].port = port;
                    for (int i = games[g].game.room.players.Count - 1; i >= 0; i--)
                    {
                        if (Server.clients.ContainsKey(games[g].game.room.players[i].client) && Server.clients[games[g].game.room.players[i].client].accountID == games[g].game.room.players[i].id)
                        {
                            Packet packet = new Packet();
                            packet.Write((int)Manager.InternalID.NETCODE_STARTED);
                            packet.Write(port);
                            Manager.SendTCPData(games[g].game.room.players[i].client, packet);
                        }
                        else
                        {
                            games[g].game.room.players.RemoveAt(i);
                        }
                    }
                    games.RemoveAt(g);
                    break;
                }
            }
        }

        private static void ServerExited(Game game)
        {

        }

        public static void StartGame(Data.Game game)
        {
            Task task = Task.Run(() =>
            {
                for (int i = game.room.players.Count - 1; i >= 0; i--)
                {
                    if (Server.clients.ContainsKey(game.room.players[i].client) && Server.clients[game.room.players[i].client].accountID == game.room.players[i].id)
                    {
                        Packet packet = new Packet();
                        packet.Write((int)Manager.InternalID.NETCODE_INIT);
                        Manager.SendTCPData(game.room.players[i].client, packet);
                    }
                    else
                    {
                        game.room.players.RemoveAt(i);
                    }
                }
                if (game.room.players.Count > 0)
                {
                    if (File.Exists(server_executable_path))
                    {
                        try
                        {
                            Game netcodeGame = new Game();
                            netcodeGame.id = Guid.NewGuid().ToString().Trim();
                            netcodeGame.game = game;
                            string path = string.Format("{0}Load{1}", tempPath, Path.DirectorySeparatorChar);
                            if (!Directory.Exists(path))
                            {
                                Directory.CreateDirectory(path);
                            }
                            string filePath = path + DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss-fff") + ".txt";
                            if (File.Exists(filePath))
                            {
                                File.Delete(filePath);
                            }
                            using (StreamWriter writer = File.CreateText(filePath))
                            {
                                writer.WriteLine(netcodeGame.id);
                            }
                            netcodeGame.process = new Process();
                            netcodeGame.process.StartInfo.FileName = server_executable_path;
                            netcodeGame.process.StartInfo.WindowStyle = ProcessWindowStyle.Minimized;
                            netcodeGame.process.EnableRaisingEvents = true;
                            netcodeGame.process.StartInfo.ArgumentList.Add(netcodeGame.id);
                            netcodeGame.process.Exited += new EventHandler(ProcessExited);
                            games.Add(netcodeGame);
                            netcodeGame.process.Start();
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex);
                        }
                    }
                    else
                    {
                        Console.WriteLine("Server executable is missing.");
                    }
                }
            });
        }

        private static void ProcessExited(object sender, EventArgs e)
        {
            Process process = (Process)sender;
            if(process.StartInfo.ArgumentList.Count > 0)
            {
                string id = process.StartInfo.ArgumentList[0];
                for (int i = 0; i < games.Count; i++)
                {
                    if (games[i].id == id)
                    {
                        ServerExited(games[i]);
                        games.RemoveAt(i);
                        break;
                    }
                }
            }
        }

    }
}