using System;
using System.Numerics;

namespace DevelopersHub.RealtimeNetworking.Server
{
    class Sender
    {

        #region Core
        /// <summary>Sends a packet to a client via TCP.</summary>
        /// <param name="clientID">The client to send the packet the packet to.</param>
        /// <param name="packet">The packet to send to the client.</param>
        private static void SendTCPData(int clientID, Packet packet)
        {
            packet.WriteLength();
            Server.clients[clientID].tcp.SendData(packet);
        }

        /// <summary>Sends a packet to a client via UDP.</summary>
        /// <param name="clientID">The client to send the packet the packet to.</param>
        /// <param name="packet">The packet to send to the client.</param>
        private static void SendUDPData(int clientID, Packet packet)
        {
            packet.WriteLength();
            Server.clients[clientID].udp.SendData(packet);
        }

        /// <summary>Sends a packet to all clients via TCP.</summary>
        /// <param name="packet">The packet to send.</param>
        private static void SendTCPDataToAll(Packet packet)
        {
            packet.WriteLength();
            for (int i = 1; i <= Server.MaxPlayers; i++)
            {
                Server.clients[i].tcp.SendData(packet);
            }
        }

        /// <summary>Sends a packet to all clients except one via TCP.</summary>
        /// <param name="exceptClientID">The client to NOT send the data to.</param>
        /// <param name="packet">The packet to send.</param>
        private static void SendTCPDataToAll(int exceptClientID, Packet packet)
        {
            packet.WriteLength();
            for (int i = 1; i <= Server.MaxPlayers; i++)
            {
                if (i != exceptClientID)
                {
                    Server.clients[i].tcp.SendData(packet);
                }
            }
        }

        /// <summary>Sends a packet to all clients via UDP.</summary>
        /// <param name="packet">The packet to send.</param>
        private static void SendUDPDataToAll(Packet packet)
        {
            packet.WriteLength();
            for (int i = 1; i <= Server.MaxPlayers; i++)
            {
                Server.clients[i].udp.SendData(packet);
            }
        }

        /// <summary>Sends a packet to all clients except one via UDP.</summary>
        /// <param name="exceptClientID">The client to NOT send the data to.</param>
        /// <param name="packet">The packet to send.</param>
        private static void SendUDPDataToAll(int exceptClientID, Packet packet)
        {
            packet.WriteLength();
            for (int i = 1; i <= Server.MaxPlayers; i++)
            {
                if (i != exceptClientID)
                {
                    Server.clients[i].udp.SendData(packet);
                }
            }
        }
        #endregion

        #region TCP
        public static void TCP_Send(int clientID, Packet packet)
        {
            if (packet != null)
            {
                packet.SetID((int)Packet.ID.CUSTOM);
                SendTCPData(clientID, packet);
            }
        }

        public static void TCP_SentToAll(Packet packet)
        {
            if (packet != null)
            {
                packet.SetID((int)Packet.ID.CUSTOM);
                SendTCPDataToAll(packet);
            }
        }

        public static void TCP_SentToAllExeptOne(int excludedClientID, Packet packet)
        {
            if (packet != null)
            {
                packet.SetID((int)Packet.ID.CUSTOM);
                SendTCPDataToAll(excludedClientID, packet);
            }
        }
        #endregion

        #region UDP
        public static void UDP_Send(int clientID, Packet packet)
        {
            if (packet != null)
            {
                packet.SetID((int)Packet.ID.CUSTOM);
                SendUDPData(clientID, packet);
            }
        }

        public static void UDP_SentToAll(Packet packet)
        {
            if (packet != null)
            {
                packet.SetID((int)Packet.ID.CUSTOM);
                SendUDPDataToAll(packet);
            }
        }

        public static void UDP_SentToAllExeptOne(int excludedClientID, Packet packet)
        {
            if (packet != null)
            {
                packet.SetID((int)Packet.ID.CUSTOM);
                SendUDPDataToAll(excludedClientID, packet);
            }
        }
        #endregion

    }
}