using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace DevelopersHub.RealtimeNetworking.Server
{
    class Receiver
    {

        public static void Initialization(int clientID, Packet packet)
        {
            string token = packet.ReadString();
            Server.clients[clientID].receiveToken = token;
        }

        public static void ReceiveCustom(int clientID, Packet packet)
        {
            if (packet != null)
            {
                Terminal.PacketReceived(clientID, packet);
            }
        }

        public static void ReceiveInternal(int clientID, Packet packet)
        {
            if (packet != null && Manager.enabled)
            {
                Manager.ReceivedPacket(clientID, packet);
            }
        }

    }
}