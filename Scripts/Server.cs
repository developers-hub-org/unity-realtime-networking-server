﻿using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace DevelopersHub.RealtimeNetworking.Server
{
    class Server
    {

        public static int MaxPlayers { get; private set; }
        public static int Port { get; private set; }
        public static Dictionary<int, Client> clients = new Dictionary<int, Client>();
        public delegate void PacketHandler(int clientID, Packet packet);
        public static Dictionary<int, PacketHandler> packetHandlers;

        private static TcpListener tcpListener;
        private static UdpClient udpListener;

        public static void Start(int maxPlayers, int port)
        {
            MaxPlayers = maxPlayers;
            Port = port;
            for (int i = 1; i <= MaxPlayers; i++)
            {
                clients.Add(i, new Client(i));
            }
            packetHandlers = new Dictionary<int, PacketHandler>()
            {
                { (int)Packet.ID.INITIALIZATION, Receiver.Initialization },
                { (int)Packet.ID.CUSTOM, Receiver.ReceiveCustom },
                { (int)Packet.ID.INTERNAL, Receiver.ReceiveInternal },
            };
            tcpListener = new TcpListener(IPAddress.Any, Port);
            tcpListener.Start();
            tcpListener.BeginAcceptTcpClient(OnConnectedTCP, null);
            if (Terminal.udp_active)
            {
                udpListener = new UdpClient(Port);
                udpListener.BeginReceive(OnConnectedUDP, null);
            }
            Terminal.Start();
        }

        private static void OnConnectedTCP(IAsyncResult result)
        {
            TcpClient client = tcpListener.EndAcceptTcpClient(result);
            tcpListener.BeginAcceptTcpClient(OnConnectedTCP, null);
            Console.WriteLine("Incoming connection from {0}.", client.Client.RemoteEndPoint);
            for (int i = 1; i <= MaxPlayers; i++)
            {
                if (clients[i].tcp.socket == null && clients[i].accountID < 0 && clients[i].disconnecting == false)
                {
                    clients[i].tcp.Initialize(client);
                    IPEndPoint ip = client.Client.RemoteEndPoint as IPEndPoint;
                    Terminal.ClientConnected(i, ip.Address.ToString());
                    if (Manager.enabled)
                    {
                        Manager.OnClientConnected(i, ip.Address.ToString());
                    }
                    return;
                }
            }
            Console.WriteLine("{0} failed to connect. Server is at full capacity.", client.Client.RemoteEndPoint);
        }

        private static void OnConnectedUDP(IAsyncResult result)
        {
            try
            {
                IPEndPoint clientEndPoint = new IPEndPoint(IPAddress.Any, 0);
                byte[] data = udpListener.EndReceive(result, ref clientEndPoint);
                udpListener.BeginReceive(OnConnectedUDP, null);
                if (data.Length < 4)
                {
                    return;
                }
                using (Packet packet = new Packet(data))
                {
                    int id = packet.ReadInt();
                    if (id == 0)
                    {
                        return;
                    }
                    if (clients[id].udp.endPoint == null)
                    {
                        clients[id].udp.Connect(clientEndPoint);
                        return;
                    }
                    if (clients[id].udp.endPoint.ToString() == clientEndPoint.ToString())
                    {
                        clients[id].udp.CheckData(packet);
                    }
                }
            }
            catch (Exception ex)
            {
                Tools.LogError(ex.Message, ex.StackTrace);
            }
        }

        public static void SendDataUDP(IPEndPoint clientEndPoint, Packet packet)
        {
            try
            {
                if (clientEndPoint != null)
                {
                    udpListener.BeginSend(packet.ToArray(), packet.Length(), clientEndPoint, null, null);
                }
            }
            catch (Exception ex)
            {
                Tools.LogError(ex.Message, ex.StackTrace);
            }
        }

    }
}