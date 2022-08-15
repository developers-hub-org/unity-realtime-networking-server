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
        public static void TCP_Send(int clientID, int packetID)
        {
            using (Packet packet = new Packet((int)Packet.ID.NULL))
            {
                packet.Write(packetID);
                SendTCPData(clientID, packet);
            }
        }
        
        public static void TCP_SentToAll(int packetID)
        {
            using (Packet packet = new Packet((int)Packet.ID.NULL))
            {
                packet.Write(packetID);
                SendTCPDataToAll(packet);
            }
        }

        public static void TCP_SentToAllExeptOne(int excludedClientID, int packetID)
        {
            using (Packet packet = new Packet((int)Packet.ID.NULL))
            {
                packet.Write(packetID);
                SendTCPDataToAll(excludedClientID, packet);
            }
        }

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

        public static void TCP_Send(int clientID, int packetID, string data)
        {
            if (data != null && clientID > 0)
            {
                using (Packet packet = new Packet((int)Packet.ID.STRING))
                {
                    packet.Write(packetID);
                    packet.Write(data);
                    SendTCPData(clientID, packet);
                }
            }
        }

        public static void TCP_SentToAll(int packetID, string data)
        {
            if (data != null)
            {
                using (Packet packet = new Packet((int)Packet.ID.STRING))
                {
                    packet.Write(packetID);
                    packet.Write(data);
                    SendTCPDataToAll(packet);
                }
            }
        }

        public static void TCP_SentToAllExeptOne(int excludedClientID, int packetID, string data)
        {
            if (data != null)
            {
                using (Packet packet = new Packet((int)Packet.ID.STRING))
                {
                    packet.Write(packetID);
                    packet.Write(data);
                    SendTCPDataToAll(excludedClientID, packet);
                }
            }
        }

        public static void TCP_Send(int clientID, int packetID, byte data)
        {
            if (clientID > 0)
            {
                using (Packet packet = new Packet((int)Packet.ID.BYTE))
                {
                    packet.Write(packetID);
                    packet.Write(data);
                    SendTCPData(clientID, packet);
                }
            }
        }

        public static void TCP_SentToAll(int packetID, byte data)
        {
            using (Packet packet = new Packet((int)Packet.ID.BYTE))
            {
                packet.Write(packetID);
                packet.Write(data);
                SendTCPDataToAll(packet);
            }
        }

        public static void TCP_SentToAllExeptOne(int excludedClientID, int packetID, byte data)
        {
            using (Packet packet = new Packet((int)Packet.ID.BYTE))
            {
                packet.Write(packetID);
                packet.Write(data);
                SendTCPDataToAll(excludedClientID, packet);
            }
        }

        public static void TCP_Send(int clientID, int packetID, byte[] data)
        {
            if (data != null && clientID > 0)
            {
                using (Packet packet = new Packet((int)Packet.ID.BYTES))
                {
                    packet.Write(packetID);
                    packet.Write(data.Length);
                    packet.Write(data);
                    SendTCPData(clientID, packet);
                }
            }
        }

        public static void TCP_SentToAll(int packetID, byte[] data)
        {
            if (data != null)
            {
                using (Packet packet = new Packet((int)Packet.ID.BYTES))
                {
                    packet.Write(packetID);
                    packet.Write(data.Length);
                    packet.Write(data);
                    SendTCPDataToAll(packet);
                }
            }
        }

        public static void TCP_SentToAllExeptOne(int excludedClientID, int packetID, byte[] data)
        {
            if (data != null)
            {
                using (Packet packet = new Packet((int)Packet.ID.BYTES))
                {
                    packet.Write(packetID);
                    packet.Write(data.Length);
                    packet.Write(data);
                    SendTCPDataToAll(excludedClientID, packet);
                }
            }
        }

        public static void TCP_Send(int clientID, int packetID, Vector3 data)
        {
            if (clientID > 0)
            {
                using (Packet packet = new Packet((int)Packet.ID.VECTOR3))
                {
                    packet.Write(packetID);
                    packet.Write(data);
                    SendTCPData(clientID, packet);
                }
            }
        }

        public static void TCP_SentToAll(int packetID, Vector3 data)
        {
            using (Packet packet = new Packet((int)Packet.ID.VECTOR3))
            {
                packet.Write(packetID);
                packet.Write(data);
                SendTCPDataToAll(packet);
            }
        }

        public static void TCP_SentToAllExeptOne(int excludedClientID, int packetID, Vector3 data)
        {
            using (Packet packet = new Packet((int)Packet.ID.VECTOR3))
            {
                packet.Write(packetID);
                packet.Write(data);
                SendTCPDataToAll(excludedClientID, packet);
            }
        }

        public static void TCP_Send(int clientID, int packetID, Quaternion data)
        {
            if (clientID > 0)
            {
                using (Packet packet = new Packet((int)Packet.ID.QUATERNION))
                {
                    packet.Write(packetID);
                    packet.Write(data);
                    SendTCPData(clientID, packet);
                }
            }
        }

        public static void TCP_SentToAll(int packetID, Quaternion data)
        {
            using (Packet packet = new Packet((int)Packet.ID.QUATERNION))
            {
                packet.Write(packetID);
                packet.Write(data);
                SendTCPDataToAll(packet);
            }
        }

        public static void TCP_SentToAllExeptOne(int excludedClientID, int packetID, Quaternion data)
        {
            using (Packet packet = new Packet((int)Packet.ID.QUATERNION))
            {
                packet.Write(packetID);
                packet.Write(data);
                SendTCPDataToAll(excludedClientID, packet);
            }
        }

        public static void TCP_Send(int clientID, int packetID, int data)
        {
            if (clientID > 0)
            {
                using (Packet packet = new Packet((int)Packet.ID.INTEGER))
                {
                    packet.Write(packetID);
                    packet.Write(data);
                    SendTCPData(clientID, packet);
                }
            }
        }

        public static void TCP_SentToAll(int packetID, int data)
        {
            using (Packet packet = new Packet((int)Packet.ID.INTEGER))
            {
                packet.Write(packetID);
                packet.Write(data);
                SendTCPDataToAll(packet);
            }
        }

        public static void TCP_SentToAllExeptOne(int excludedClientID, int packetID, int data)
        {
            using (Packet packet = new Packet((int)Packet.ID.INTEGER))
            {
                packet.Write(packetID);
                packet.Write(data);
                SendTCPDataToAll(excludedClientID, packet);
            }
        }

        public static void TCP_Send(int clientID, int packetID, bool data)
        {
            if (clientID > 0)
            {
                using (Packet packet = new Packet((int)Packet.ID.BOOLEAN))
                {
                    packet.Write(packetID);
                    packet.Write(data);
                    SendTCPData(clientID, packet);
                }
            }
        }

        public static void TCP_SentToAll(int packetID, bool data)
        {
            using (Packet packet = new Packet((int)Packet.ID.BOOLEAN))
            {
                packet.Write(packetID);
                packet.Write(data);
                SendTCPDataToAll(packet);
            }
        }

        public static void TCP_SentToAllExeptOne(int excludedClientID, int packetID, bool data)
        {
            using (Packet packet = new Packet((int)Packet.ID.BOOLEAN))
            {
                packet.Write(packetID);
                packet.Write(data);
                SendTCPDataToAll(excludedClientID, packet);
            }
        }

        public static void TCP_Send(int clientID, int packetID, float data)
        {
            if (clientID > 0)
            {
                using (Packet packet = new Packet((int)Packet.ID.FLOAT))
                {
                    packet.Write(packetID);
                    packet.Write(data);
                    SendTCPData(clientID, packet);
                }
            }
        }

        public static void TCP_SentToAll(int packetID, float data)
        {
            using (Packet packet = new Packet((int)Packet.ID.FLOAT))
            {
                packet.Write(packetID);
                packet.Write(data);
                SendTCPDataToAll(packet);
            }
        }

        public static void TCP_SentToAllExeptOne(int excludedClientID, int packetID, float data)
        {
            using (Packet packet = new Packet((int)Packet.ID.FLOAT))
            {
                packet.Write(packetID);
                packet.Write(data);
                SendTCPDataToAll(excludedClientID, packet);
            }
        }

        public static void TCP_Send(int clientID, int packetID, long data)
        {
            if (clientID > 0)
            {
                using (Packet packet = new Packet((int)Packet.ID.LONG))
                {
                    packet.Write(packetID);
                    packet.Write(data);
                    SendTCPData(clientID, packet);
                }
            }
        }

        public static void TCP_SentToAll(int packetID, long data)
        {
            using (Packet packet = new Packet((int)Packet.ID.LONG))
            {
                packet.Write(packetID);
                packet.Write(data);
                SendTCPDataToAll(packet);
            }
        }

        public static void TCP_SentToAllExeptOne(int excludedClientID, int packetID, long data)
        {
            using (Packet packet = new Packet((int)Packet.ID.LONG))
            {
                packet.Write(packetID);
                packet.Write(data);
                SendTCPDataToAll(excludedClientID, packet);
            }
        }

        public static void TCP_Send(int clientID, int packetID, short data)
        {
            if (clientID > 0)
            {
                using (Packet packet = new Packet((int)Packet.ID.SHORT))
                {
                    packet.Write(packetID);
                    packet.Write(data);
                    SendTCPData(clientID, packet);
                }
            }
        }

        public static void TCP_SentToAll(int packetID, short data)
        {
            using (Packet packet = new Packet((int)Packet.ID.SHORT))
            {
                packet.Write(packetID);
                packet.Write(data);
                SendTCPDataToAll(packet);
            }
        }

        public static void TCP_SentToAllExeptOne(int excludedClientID, int packetID, short data)
        {
            using (Packet packet = new Packet((int)Packet.ID.SHORT))
            {
                packet.Write(packetID);
                packet.Write(data);
                SendTCPDataToAll(excludedClientID, packet);
            }
        }
        #endregion

        #region UDP
        public static void UDP_Send(int clientID, int packetID)
        {
            using (Packet packet = new Packet((int)Packet.ID.NULL))
            {
                packet.Write(packetID);
                SendUDPData(clientID, packet);
            }
        }

        public static void UDP_SentToAll(int packetID)
        {
            using (Packet packet = new Packet((int)Packet.ID.NULL))
            {
                packet.Write(packetID);
                SendUDPDataToAll(packet);
            }
        }

        public static void UDP_SentToAllExeptOne(int excludedClientID, int packetID)
        {
            using (Packet packet = new Packet((int)Packet.ID.NULL))
            {
                packet.Write(packetID);
                SendUDPDataToAll(excludedClientID, packet);
            }
        }

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

        public static void UDP_Send(int clientID, int packetID, string data)
        {
            if (data != null && clientID > 0)
            {
                using (Packet packet = new Packet((int)Packet.ID.STRING))
                {
                    packet.Write(packetID);
                    packet.Write(data);
                    SendUDPData(clientID, packet);
                }
            }
        }

        public static void UDP_SentToAll(int packetID, string data)
        {
            if (data != null)
            {
                using (Packet packet = new Packet((int)Packet.ID.STRING))
                {
                    packet.Write(packetID);
                    packet.Write(data);
                    SendUDPDataToAll(packet);
                }
            }
        }

        public static void UDP_SentToAllExeptOne(int excludedClientID, int packetID, string data)
        {
            if (data != null)
            {
                using (Packet packet = new Packet((int)Packet.ID.STRING))
                {
                    packet.Write(packetID);
                    packet.Write(data);
                    SendUDPDataToAll(excludedClientID, packet);
                }
            }
        }

        public static void UDP_Send(int clientID, int packetID, byte data)
        {
            if (clientID > 0)
            {
                using (Packet packet = new Packet((int)Packet.ID.BYTE))
                {
                    packet.Write(packetID);
                    packet.Write(data);
                    SendUDPData(clientID, packet);
                }
            }
        }

        public static void UDP_SentToAll(int packetID, byte data)
        {
            using (Packet packet = new Packet((int)Packet.ID.BYTE))
            {
                packet.Write(packetID);
                packet.Write(data);
                SendUDPDataToAll(packet);
            }
        }

        public static void UDP_SentToAllExeptOne(int excludedClientID, int packetID, byte data)
        {
            using (Packet packet = new Packet((int)Packet.ID.BYTE))
            {
                packet.Write(packetID);
                packet.Write(data);
                SendUDPDataToAll(excludedClientID, packet);
            }
        }

        public static void UDP_Send(int clientID, int packetID, byte[] data)
        {
            if (data != null && clientID > 0)
            {
                using (Packet packet = new Packet((int)Packet.ID.BYTES))
                {
                    packet.Write(packetID);
                    packet.Write(data.Length);
                    packet.Write(data);
                    SendUDPData(clientID, packet);
                }
            }
        }

        public static void UDP_SentToAll(int packetID, byte[] data)
        {
            if (data != null)
            {
                using (Packet packet = new Packet((int)Packet.ID.BYTES))
                {
                    packet.Write(packetID);
                    packet.Write(data.Length);
                    packet.Write(data);
                    SendUDPDataToAll(packet);
                }
            }
        }

        public static void UDP_SentToAllExeptOne(int excludedClientID, int packetID, byte[] data)
        {
            if (data != null)
            {
                using (Packet packet = new Packet((int)Packet.ID.BYTES))
                {
                    packet.Write(packetID);
                    packet.Write(data.Length);
                    packet.Write(data);
                    SendUDPDataToAll(excludedClientID, packet);
                }
            }
        }

        public static void UDP_Send(int clientID, int packetID, Vector3 data)
        {
            if (clientID > 0)
            {
                using (Packet packet = new Packet((int)Packet.ID.VECTOR3))
                {
                    packet.Write(packetID);
                    packet.Write(data);
                    SendUDPData(clientID, packet);
                }
            }
        }

        public static void UDP_SentToAll(int packetID, Vector3 data)
        {
            using (Packet packet = new Packet((int)Packet.ID.VECTOR3))
            {
                packet.Write(packetID);
                packet.Write(data);
                SendUDPDataToAll(packet);
            }
        }

        public static void UDP_SentToAllExeptOne(int excludedClientID, int packetID, Vector3 data)
        {
            using (Packet packet = new Packet((int)Packet.ID.VECTOR3))
            {
                packet.Write(packetID);
                packet.Write(data);
                SendUDPDataToAll(excludedClientID, packet);
            }
        }

        public static void UDP_Send(int clientID, int packetID, Quaternion data)
        {
            if (clientID > 0)
            {
                using (Packet packet = new Packet((int)Packet.ID.QUATERNION))
                {
                    packet.Write(packetID);
                    packet.Write(data);
                    SendUDPData(clientID, packet);
                }
            }
        }

        public static void UDP_SentToAll(int packetID, Quaternion data)
        {
            using (Packet packet = new Packet((int)Packet.ID.QUATERNION))
            {
                packet.Write(packetID);
                packet.Write(data);
                SendUDPDataToAll(packet);
            }
        }

        public static void UDP_SentToAllExeptOne(int excludedClientID, int packetID, Quaternion data)
        {
            using (Packet packet = new Packet((int)Packet.ID.QUATERNION))
            {
                packet.Write(packetID);
                packet.Write(data);
                SendUDPDataToAll(excludedClientID, packet);
            }
        }

        public static void UDP_Send(int clientID, int packetID, int data)
        {
            if (clientID > 0)
            {
                using (Packet packet = new Packet((int)Packet.ID.INTEGER))
                {
                    packet.Write(packetID);
                    packet.Write(data);
                    SendUDPData(clientID, packet);
                }
            }
        }

        public static void UDP_SentToAll(int packetID, int data)
        {
            using (Packet packet = new Packet((int)Packet.ID.INTEGER))
            {
                packet.Write(packetID);
                packet.Write(data);
                SendUDPDataToAll(packet);
            }
        }

        public static void UDP_SentToAllExeptOne(int excludedClientID, int packetID, int data)
        {
            using (Packet packet = new Packet((int)Packet.ID.INTEGER))
            {
                packet.Write(packetID);
                packet.Write(data);
                SendUDPDataToAll(excludedClientID, packet);
            }
        }

        public static void UDP_Send(int clientID, int packetID, bool data)
        {
            if (clientID > 0)
            {
                using (Packet packet = new Packet((int)Packet.ID.BOOLEAN))
                {
                    packet.Write(packetID);
                    packet.Write(data);
                    SendUDPData(clientID, packet);
                }
            }
        }

        public static void UDP_SentToAll(int packetID, bool data)
        {
            using (Packet packet = new Packet((int)Packet.ID.BOOLEAN))
            {
                packet.Write(packetID);
                packet.Write(data);
                SendUDPDataToAll(packet);
            }
        }

        public static void UDP_SentToAllExeptOne(int excludedClientID, int packetID, bool data)
        {
            using (Packet packet = new Packet((int)Packet.ID.BOOLEAN))
            {
                packet.Write(packetID);
                packet.Write(data);
                SendUDPDataToAll(excludedClientID, packet);
            }
        }

        public static void UDP_Send(int clientID, int packetID, float data)
        {
            if (clientID > 0)
            {
                using (Packet packet = new Packet((int)Packet.ID.FLOAT))
                {
                    packet.Write(packetID);
                    packet.Write(data);
                    SendUDPData(clientID, packet);
                }
            }
        }

        public static void UDP_SentToAll(int packetID, float data)
        {
            using (Packet packet = new Packet((int)Packet.ID.FLOAT))
            {
                packet.Write(packetID);
                packet.Write(data);
                SendUDPDataToAll(packet);
            }
        }

        public static void UDP_SentToAllExeptOne(int excludedClientID, int packetID, float data)
        {
            using (Packet packet = new Packet((int)Packet.ID.FLOAT))
            {
                packet.Write(packetID);
                packet.Write(data);
                SendUDPDataToAll(excludedClientID, packet);
            }
        }

        public static void UDP_Send(int clientID, int packetID, long data)
        {
            if (clientID > 0)
            {
                using (Packet packet = new Packet((int)Packet.ID.LONG))
                {
                    packet.Write(packetID);
                    packet.Write(data);
                    SendUDPData(clientID, packet);
                }
            }
        }

        public static void UDP_SentToAll(int packetID, long data)
        {
            using (Packet packet = new Packet((int)Packet.ID.LONG))
            {
                packet.Write(packetID);
                packet.Write(data);
                SendUDPDataToAll(packet);
            }
        }

        public static void UDP_SentToAllExeptOne(int excludedClientID, int packetID, long data)
        {
            using (Packet packet = new Packet((int)Packet.ID.LONG))
            {
                packet.Write(packetID);
                packet.Write(data);
                SendUDPDataToAll(excludedClientID, packet);
            }
        }

        public static void UDP_Send(int clientID, int packetID, short data)
        {
            if (clientID > 0)
            {
                using (Packet packet = new Packet((int)Packet.ID.SHORT))
                {
                    packet.Write(packetID);
                    packet.Write(data);
                    SendUDPData(clientID, packet);
                }
            }
        }

        public static void UDP_SentToAll(int packetID, short data)
        {
            using (Packet packet = new Packet((int)Packet.ID.SHORT))
            {
                packet.Write(packetID);
                packet.Write(data);
                SendUDPDataToAll(packet);
            }
        }

        public static void UDP_SentToAllExeptOne(int excludedClientID, int packetID, short data)
        {
            using (Packet packet = new Packet((int)Packet.ID.SHORT))
            {
                packet.Write(packetID);
                packet.Write(data);
                SendUDPDataToAll(excludedClientID, packet);
            }
        }
        #endregion

    }
}