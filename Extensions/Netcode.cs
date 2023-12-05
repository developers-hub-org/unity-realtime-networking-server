using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace DevelopersHub.RealtimeNetworking.Server
{
    class Netcode
    {

        #region Internal

        private static int process_check_period = 60;
        private static DateTime laast_process_check;

        public static void Start()
        {
            string tempPath = Path.GetTempPath().TrimEnd(Path.DirectorySeparatorChar) + string.Format("{0}RealtimeNetworking{0}Extentions{0}Netcode", Path.DirectorySeparatorChar);
            string resultPath = string.Format("{0}Result{1}", tempPath, Path.DirectorySeparatorChar);
            string loadPath = string.Format("{0}Load{1}", tempPath, Path.DirectorySeparatorChar);
            string readyPath = string.Format("{0}Ready{1}", tempPath, Path.DirectorySeparatorChar);
            if (Directory.Exists(resultPath))
            {
                Directory.Delete(resultPath, true);
            }
            if (Directory.Exists(loadPath))
            {
                Directory.Delete(loadPath, true);
            }
            if (Directory.Exists(readyPath))
            {
                Directory.Delete(readyPath, true);
            }
            laast_process_check = DateTime.Now;
        }

        public static void OnExit()
        {
            for (int i = 0; i < games.Count; i++)
            {
                KillGameProcess(i);
            }
        }

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
            public DateTime start;
            public Data.RuntimeGame runtime = null;
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
                path = string.Format("{0}Result{1}", tempPath, Path.DirectorySeparatorChar);
                if (Directory.Exists(path))
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
                                    string serializedData = File.ReadAllText(file);
                                    File.Delete(file);
                                    Data.RuntimeResult result = Tools.Desrialize<Data.RuntimeResult>(Tools.DecompressString(serializedData));
                                    Terminal.OnNetcodeGameResultReceived(result);
                                }
                                catch (Exception)
                                {

                                }
                            }
                        }
                    }
                }
                path = string.Format("{0}Close{1}", tempPath, Path.DirectorySeparatorChar);
                if (Directory.Exists(path))
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
                                    string id = File.ReadAllText(file).Trim();
                                    File.Delete(file);
                                    for (int i = 0; i < games.Count; i++)
                                    {
                                        if (games[i] != null && games[i].id == id)
                                        {
                                            KillGameProcess(i);
                                        }
                                    }
                                }
                                catch (Exception)
                                {

                                }
                            }
                        }
                    }
                }
                if(Terminal.netcode_max_server_life_seconds > 0)
                {
                    double process_check_seconds = (DateTime.Now - laast_process_check).TotalSeconds;
                    if (process_check_seconds >= process_check_period)
                    {
                        laast_process_check = DateTime.Now;
                        for (int i = 0; i < games.Count; i++)
                        {
                            if (games[i] != null && (DateTime.Now - games[i].start).TotalSeconds >= Terminal.netcode_max_server_life_seconds)
                            {
                                KillGameProcess(i);
                            }
                        }
                    }
                }
                updating = false;
            });
        }

        private static void KillGameProcess(int index)
        {
            if (games[index] != null && games[index].process != null)
            {
                var process = games[index].process;
                process.Kill();
                process.WaitForExit();
                process.Dispose();
            }
        }

        private static void ServerIsReady(string id, int port)
        {
            Console.WriteLine("Netcode server is ready." + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            for (int g = 0; g < games.Count; g++)
            {
                if (games[g].id == id)
                {
                    games[g].port = port;
                    for (int i = games[g].game.room.players.Count - 1; i >= 0; i--)
                    {
                        if (Server.clients.ContainsKey(games[g].game.room.players[i].client) && Server.clients[games[g].game.room.players[i].client].accountID == games[g].game.room.players[i].id)
                        {
                            byte[] serializedData = Tools.Compress(Tools.Serialize<Data.RuntimeGame>(games[g].runtime));
                            Packet packet = new Packet();
                            packet.Write((int)Manager.InternalID.NETCODE_STARTED);
                            packet.Write(port);
                            packet.Write(serializedData.Length);
                            packet.Write(serializedData);
                            Manager.SendTCPData(games[g].game.room.players[i].client, packet);
                        }
                        else
                        {
                            games[g].game.room.players.RemoveAt(i);
                        }
                    }
                    break;
                }
            }
        }

        private static void ServerExited(Game game)
        {
            Console.WriteLine("Netcode server closed." + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
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
                    if (File.Exists(Terminal.netcode_server_executable_path))
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
                            Data.RuntimeGame data = _GetRuntimeGame(netcodeGame.game);
                            data.id = netcodeGame.id;
                            string serializedData = Tools.CompressString(Tools.Serialize<Data.RuntimeGame>(data));
                            File.WriteAllText(filePath, serializedData);
                            netcodeGame.process = new Process();
                            netcodeGame.process.StartInfo.FileName = Terminal.netcode_server_executable_path;
                            netcodeGame.process.StartInfo.WindowStyle = ProcessWindowStyle.Minimized;
                            netcodeGame.process.StartInfo.CreateNoWindow = true;
                            netcodeGame.process.StartInfo.UseShellExecute = false;
                            netcodeGame.process.EnableRaisingEvents = true;
                            netcodeGame.process.StartInfo.ArgumentList.Add(netcodeGame.id);
                            netcodeGame.process.Exited += new EventHandler(ProcessExited);
                            netcodeGame.start = DateTime.Now;
                            netcodeGame.runtime = data;
                            games.Add(netcodeGame);
                            netcodeGame.process.Start();
                            Console.WriteLine("Netcode server started. " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.Message);
                        }
                    }
                    else
                    {
                        Console.WriteLine("Server executable is missing.");
                    }
                }
            });
        }

        private static Data.RuntimeGame _GetRuntimeGame(Data.Game game)
        {
            Data.RuntimeGame data = new Data.RuntimeGame();
            data.mapID = game.room.mapID;
            data.gameID = game.room.gameID;
            using (var connection = Sqlite.connection)
            {
                connection.Open();
                for (int i = 0; i < game.room.players.Count; i++)
                {
                    Data.RuntimePlayer player = new Data.RuntimePlayer();
                    player.id = game.room.players[i].id;
                    player.username = game.room.players[i].username;
                    player.team = game.room.players[i].team;
                    player.characters = Manager.GetRuntimeCharacters(player.id, true, true, connection);
                    data.players.Add(player);
                }
                Terminal.OverrideGameInitialData(ref data, connection);
                connection.Close();
            }
            return data;
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
        
        #endregion

    }
}