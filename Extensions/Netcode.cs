using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters;
using System.Threading.Tasks;

namespace DevelopersHub.RealtimeNetworking.Server
{
    class Netcode
    {

        private const string server_executable_path = @"C:\Users\Armin\Desktop\New folder\Netcode.exe";

        public static void Update()
        {
            if (!updating)
            {
                updating = true;
                _Update();
            }
        }

        private static bool updating = false;

        private static void _Update()
        {
            Task task = Task.Run(() =>
            {
                if (ports.Count > 0)
                {
                    string[] files = Directory.GetFiles(tempPath);
                    for (int i = ports.Count - 1; i >= 0; i--)
                    {
                        bool remove = true;
                        foreach (string file in files)
                        {
                            if(Path.GetFileNameWithoutExtension(file) == ports[i].ToString())
                            {
                                using (var reader = new StreamReader(file))
                                {
                                    int status = int.Parse(reader.ReadLine().Trim());
                                    if (status <= 1)
                                    {
                                        remove = false;
                                    }
                                    else
                                    {
                                        ServerInstanceStarted(ports[i]);
                                    }
                                }
                                break;
                            }
                        }
                        if(remove)
                        {
                            ports.RemoveAt(i);
                        }
                    }
                }
                updating = false;
            });
        }

        private static void ServerInstanceStarted(int port)
        {
            // inform players
        }

        public static List<int> ports = new List<int>();

        private static string tempPath 
        { 
            get 
            {
                return Path.GetTempPath().TrimEnd(Path.DirectorySeparatorChar) + string.Format("{0}Unity{0}Netcode{0}Sessions", Path.DirectorySeparatorChar); 
            } 
        }

        public static void RunServerInstance()
        {
            Task task = Task.Run(() =>
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    if (File.Exists(server_executable_path))
                    {
                        if (Path.GetExtension(server_executable_path).ToLower() == ".exe")
                        {
                            System.Diagnostics.Process.Start(server_executable_path);
                        }
                        else
                        {
                            Console.WriteLine("Netcode server file is not a Windows executable.");
                        }
                    }
                    else
                    {
                        Console.WriteLine("Netcode server executable is missing.");
                    }
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    // ToDo
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    // ToDo
                }
                else
                {
                    Console.WriteLine("Operating System is not supported.");
                }
            });
            /*
            string path = tempPath;
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            int port = Tools.FindFreeTcpPort();
            
            string filePath = path + port.ToString() + ".txt";
            if (!File.Exists(filePath))
            {

                
            }
            else
            {
                Console.WriteLine("Port " + port.ToString() + " is not available.");
            }*/
        }

    }
}