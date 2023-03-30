using System;
using System.Numerics;

namespace DevelopersHub.RealtimeNetworking.Server
{
    class Terminal
    {

        #region Update
        public const int updatesPerSecond = 30;
        public static void Start()
        {
            Console.WriteLine("Server Started.");
        }

        public static void Update()
        {
            
        }
        #endregion

        #region Connection
        public const int maxPlayers = 100000;
        public static int onlinePlayers = 0;
        public const int port = 5555;

        public static void OnClientConnected(int id, string ip)
        {
            onlinePlayers++;
        }

        public static void OnClientDisconnected(int id, string ip)
        {
            onlinePlayers--;
        }
        #endregion

        #region Data
        public static void ReceivedPacket(int clientID, Packet packet)
        {
            // For test, remove it ->
            int integerValue = packet.ReadInt();
            string stringValue = packet.ReadString();
            float floatValue = packet.ReadFloat();
            Quaternion quaternionValue = packet.ReadQuaternion();
            bool boolValue = packet.ReadBool();
            Console.WriteLine("Int:{0} String:{1}, Float:{2}, Quaternion:{3}, Bool:{4}.", integerValue, stringValue, floatValue, quaternionValue, boolValue);
            // <-
        }

        public static void ReceivedBytes(int clientID, int packetID, byte[] data)
        {
            
        }

        public static void ReceivedString(int clientID, int packetID, string data)
        {
            // For test, remove it ->
            if(packetID == 123)
            {
                Console.WriteLine(data);

                Packet packet = new Packet();
                packet.Write(555);
                packet.Write(DateTime.Now.ToString());
                Sender.TCP_Send(clientID, packet);
            }
            // <-
        }

        public static void ReceivedInteger(int clientID, int packetID, int data)
        {
            
        }

        public static void ReceivedFloat(int clientID, int packetID, float data)
        {

        }

        public static void ReceivedBoolean(int clientID, int packetID, bool data)
        {

        }

        public static void ReceivedVector3(int clientID, int packetID, Vector3 data)
        {

        }

        public static void ReceivedQuaternion(int clientID, int packetID, Quaternion data)
        {

        }

        public static void ReceivedLong(int clientID, int packetID, long data)
        {

        }

        public static void ReceivedShort(int clientID, int packetID, short data)
        {

        }

        public static void ReceivedByte(int clientID, int packetID, byte data)
        {

        }

        public static void ReceivedEvent(int clientID, int packetID)
        {

        }
        #endregion

    }
}