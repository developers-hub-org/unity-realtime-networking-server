using System;
using System.Collections.Generic;
using System.Data;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace DevelopersHub.RealtimeNetworking.Server
{
    class Manager
    {

        public const bool enabled = true;

        private static void Matchmaking()
        {

        }

        public static void Initialize()
        {
            rooms.Clear();
            Sqlite.Initialize();
        }

        public static void Update()
        {
            if (updateMatchmaking && !matchmaking && parties.Count > 0)
            {
                updateMatchmaking = false;
                matchmaking = true;
                Task task = Task.Run(() =>
                {
                    Matchmaking();
                    matchmaking = false;
                });
            }
            Netcode.Update();
        }

        private static bool updateMatchmaking = false;
        private static bool matchmaking = false;
        private static List<Data.Party> parties = new List<Data.Party>();

        private async static Task<int> InvitePartyAsync(int clientID, long id)
        {
            Task<int> task = Task.Run(() =>
            {
                return _InvitePartyAsync(clientID, id);
            });
            return await task;
        }

        private static int _InvitePartyAsync(int clientID, long id)
        {
            int response = 0;
            if (Server.clients[clientID].party != null)
            {
                int targetClientID = -1;
                bool found = false;
                Data.PlayerProfile profile = _GetPlayer(id);
                using (var connection = Sqlite.connection)
                {
                    connection.Open();
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = string.Format(@"SELECT client_index FROM accounts WHERE id = {0};", id);
                        using (var reader = command.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                while (reader.Read())
                                {
                                    targetClientID = reader.GetInt32("client_index");
                                    found = true;
                                    break;
                                }
                            }
                        }
                    }
                    connection.Close();
                }
                if(found && profile != null)
                {
                    if(targetClientID >= 0)
                    {
                        if (!Server.clients[clientID].party.invites.Contains(id))
                        {
                            Server.clients[clientID].party.invites.Add(id);
                            response = 1;
                            Packet trPacket = new Packet();
                            trPacket.Write((int)InternalID.INVITE_PARTY);
                            trPacket.Write(2);
                            trPacket.Write(Server.clients[clientID].party.id);
                            byte[] data = Tools.Compress(Tools.Serialize<Data.PlayerProfile>(profile));
                            trPacket.Write(data.Length);
                            trPacket.Write(data);
                            SendTCPData(targetClientID, trPacket);
                        }
                        else
                        {
                            // Already invited
                            response = 6;
                        }
                    }
                    else
                    {
                        // Not online
                        response = 5;
                    }
                }
            }
            else
            {
                response = 4;
            }
            Packet packet = new Packet();
            packet.Write((int)InternalID.INVITE_PARTY);
            packet.Write(1);
            packet.Write(response);
            SendTCPData(clientID, packet);
            return response;
        }

        private async static Task<int> AnswerInvitePartyAsync(int clientID, string id, bool answer)
        {
            Task<int> task = Task.Run(() =>
            {
                return _AnswerInvitePartyAsync(clientID, id, answer);
            });
            return await task;
        }

        private static int _AnswerInvitePartyAsync(int clientID, string id, bool answer)
        {
            int response = 0;
            if(Server.clients[clientID].party == null)
            {
                for (int i = 0; i < Server.clients.Count; i++)
                {
                    if (Server.clients[i].accountID < 0 || Server.clients[i].party == null)
                    {
                        continue;
                    }
                    if (Server.clients[i].party.id == id)
                    {
                        if (answer && (Server.clients[i].party.maxPlayers <= 0 || Server.clients[i].party.players.Count < Server.clients[i].party.maxPlayers))
                        {
                            if (Server.clients[i].party.invites.Remove(Server.clients[clientID].accountID))
                            {
                                if (answer)
                                {
                                    if (Server.clients[clientID].player == null)
                                    {
                                        UpdatePlayer(clientID);
                                    }
                                    Server.clients[clientID].party = Server.clients[i].party;
                                    Server.clients[clientID].party.players.Add(Server.clients[clientID].player);

                                    // Notify others that a player joined the party
                                    byte[] bytes = Tools.Compress(Tools.Serialize<Data.Party>(Server.clients[clientID].party));
                                    byte[] playerBytes = Tools.Compress(Tools.Serialize<Data.Player>(Server.clients[clientID].player));
                                    for (int j = 0; j < Server.clients[clientID].party.players.Count; j++)
                                    {
                                        if(Server.clients[clientID].party.players[i].id == Server.clients[clientID].accountID)
                                        {
                                            continue;
                                        }
                                        Packet packetUp = new Packet();
                                        packetUp.Write((int)InternalID.PARTY_UPDATED);
                                        packetUp.Write((int)PartyUpdateType.PLAYER_JOINED);
                                        packetUp.Write(bytes.Length);
                                        packetUp.Write(bytes);
                                        packetUp.Write(playerBytes.Length);
                                        packetUp.Write(playerBytes);
                                        SendTCPData(Server.clients[clientID].party.players[i].client, packetUp);
                                    }
                                }
                                response = 1;
                            }
                            else
                            {
                                response = 4;
                            }
                        }
                        else
                        {
                            response = 5;
                        }
                        break;
                    }
                }
            }
            else
            {
                response = 6;
            }
            Packet packet = new Packet();
            packet.Write((int)InternalID.ANSWER_PARTY_INVITE);
            packet.Write(response);
            packet.Write(Server.clients[clientID].party == null);
            if(Server.clients[clientID].party != null)
            {
                byte[] bytes = Tools.Compress(Tools.Serialize<Data.Party>(Server.clients[clientID].party));
                packet.Write(bytes.Length);
                packet.Write(bytes);
            }
            SendTCPData(clientID, packet);
            return response;
        }

        private async static Task<int> CreatePartyAsync(int clientID, int maxPlayers)
        {
            Task<int> task = Task.Run(() =>
            {
                return Retry.Do(() => _CreatePartyAsync(clientID, maxPlayers), TimeSpan.FromSeconds(0.1), 5, false);
            });
            return await task;
        }

        private static int _CreatePartyAsync(int clientID, int maxPlayers)
        {
            int response = 0;
            if (Server.clients[clientID].party != null)
            {
                Server.clients[clientID].party = new Data.Party();
                Server.clients[clientID].party.id = Guid.NewGuid().ToString();
                Server.clients[clientID].party.leaderID = Server.clients[clientID].accountID;
                if(maxPlayers < 0)
                {
                    maxPlayers = 0;
                }
                Server.clients[clientID].party.maxPlayers = maxPlayers;
                if (Server.clients[clientID].player == null)
                {
                    UpdatePlayer(clientID);
                }
                Server.clients[clientID].party.players.Add(Server.clients[clientID].player);
                response = 1;
            }
            else
            {
                // Already in a party
                response = 4;
            }
            Packet packet = new Packet();
            packet.Write((int)InternalID.CREATE_PARTY);
            packet.Write(response);
            if (response == 1)
            {
                byte[] bytes = Tools.Compress(Tools.Serialize<Data.Party>(Server.clients[clientID].party));
                packet.Write(bytes.Length);
                packet.Write(bytes);
            }
            SendTCPData(clientID, packet);
            return response;
        }

        private async static Task<int> LeavePartyAsync(int clientID, bool notifySelf = false)
        {
            Task<int> task = Task.Run(() =>
            {
                return _LeavePartyAsync(clientID, notifySelf);
            });
            return await task;
        }
        
        private static int _LeavePartyAsync(int clientID, bool notifySelf = false)
        {
            int response = 0;
            if (Server.clients[clientID].party != null)
            {
                for (int i = 0; i < Server.clients[clientID].party.players.Count; i++)
                {
                    if (Server.clients[clientID].party.players[i].id == Server.clients[clientID].accountID)
                    {
                        Server.clients[clientID].party.players.RemoveAt(i);
                        if(Server.clients[clientID].party.leaderID == Server.clients[clientID].accountID && Server.clients[clientID].party.players.Count > 0)
                        {
                            Server.clients[clientID].party.leaderID = Server.clients[clientID].party.players[0].id;
                        }
                        break;
                    }
                }
                if(Server.clients[clientID].party.players.Count <= 0)
                {
                    parties.Remove(Server.clients[clientID].party);
                }
                else
                {
                    // Notify others that a player left the party
                    byte[] bytes = Tools.Compress(Tools.Serialize<Data.Party>(Server.clients[clientID].party));
                    byte[] playerBytes = Tools.Compress(Tools.Serialize<Data.Player>(Server.clients[clientID].player));
                    for (int i = 0; i < Server.clients[clientID].party.players.Count; i++)
                    {
                        Packet packet = new Packet();
                        packet.Write((int)InternalID.PARTY_UPDATED);
                        packet.Write((int)PartyUpdateType.PLAYER_LEFT);
                        packet.Write(bytes.Length);
                        packet.Write(bytes);
                        packet.Write(playerBytes.Length);
                        packet.Write(playerBytes);
                        SendTCPData(Server.clients[clientID].party.players[i].client, packet);
                    }
                }
                Server.clients[clientID].party = null;
                response = 1;
                updateMatchmaking = true;
            }
            else
            {
                response = 4;
            }
            if (notifySelf)
            {
                Packet packet = new Packet();
                packet.Write((int)InternalID.LEAVE_PARTY);
                packet.Write(response);
                SendTCPData(clientID, packet);
            }
            return response;
        }

        private async static Task<int> KickPartyAsync(int clientID, long targetID)
        {
            Task<int> task = Task.Run(() =>
            {
                return _KickPartyAsync(clientID, targetID);
            });
            return await task;
        }

        private static int _KickPartyAsync(int clientID, long targetID)
        {
            int response = 0;
            Data.Party party = Server.clients[clientID].party;
            Data.Player player = null;
            if (party != null)
            {
                if (party.leaderID == Server.clients[clientID].accountID)
                {
                    for (int i = 0; i < party.players.Count; i++)
                    {
                        if (party.players[i].id == targetID)
                        {
                            player = party.players[i];
                            party.players.RemoveAt(i);
                            if (party.leaderID == targetID && party.players.Count > 0)
                            {
                                party.leaderID = party.players[0].id;
                            }
                            Server.clients[player.client].party = null;
                            response = 1;
                            break;
                        }
                    }
                    if (response != 1)
                    {
                        response = 6;
                    }
                }
                else
                {
                    response = 5;
                }
                if (response == 1)
                {
                    if (party.players.Count <= 0)
                    {
                        parties.Remove(party);
                    }
                    else
                    {
                        // Notify others that a player left the party
                        byte[] bytes = Tools.Compress(Tools.Serialize<Data.Party>(party));
                        byte[] playerBytes = Tools.Compress(Tools.Serialize<Data.Player>(player));
                        for (int i = 0; i < party.players.Count; i++)
                        {
                            Packet packet1 = new Packet();
                            packet1.Write((int)InternalID.PARTY_UPDATED);
                            packet1.Write((int)PartyUpdateType.PLAYER_KICKED);
                            packet1.Write(bytes.Length);
                            packet1.Write(bytes);
                            packet1.Write(playerBytes.Length);
                            packet1.Write(playerBytes);
                            SendTCPData(party.players[i].client, packet1);
                        }
                        Packet packet2 = new Packet();
                        packet2.Write((int)InternalID.PARTY_UPDATED);
                        packet2.Write((int)PartyUpdateType.PLAYER_KICKED);
                        packet2.Write(bytes.Length);
                        packet2.Write(bytes);
                        packet2.Write(playerBytes.Length);
                        packet2.Write(playerBytes);
                        SendTCPData(player.client, packet2);
                    }
                    updateMatchmaking = true;
                }
            }
            else
            {
                response = 4;
            }
            Packet packet = new Packet();
            packet.Write((int)InternalID.KICK_PARTY_MEMBER);
            packet.Write(1);
            packet.Write(response);
            SendTCPData(clientID, packet);
            return response;
        }

        public static void OnClientConnected(int id, string ip)
        {
            Server.clients[id].ipAddress = ip;
            Server.clients[id].room = null;
            Server.clients[id].player = null;
            Server.clients[id].party = null;
        }

        public async static void OnClientDisconnected(int id, string ip)
        {
            if(Server.clients[id].accountID >= 0 && Server.clients[id].disconnecting == false)
            {
                Server.clients[id].disconnecting = true;
                await PlayerDisconnectedAsync(Server.clients[id].accountID);
                if(Server.clients[id].room != null)
                {
                    await LeaveRoomAsync(id, false, false);
                }
                await LeavePartyAsync(id);
                Server.clients[id].room = null;
                Server.clients[id].player = null;
                Server.clients[id].party = null;
                Server.clients[id].ipAddress = "";
                Server.clients[id].accountID = -1;
                Server.clients[id].disconnecting = false;
            }
        }

        private async static Task<int> PlayerDisconnectedAsync(long accountID)
        {
            Task<int> task = Task.Run(() =>
            {
                return Retry.Do(() => _PlayerDisconnectedAsync(accountID), TimeSpan.FromSeconds(0.1), 5, false);
            });
            return await task;
        }

        private static int _PlayerDisconnectedAsync(long accountID)
        {
            using (var connection = Sqlite.connection)
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = string.Format(@"UPDATE accounts SET client_index = -1 WHERE id = {0}", accountID);
                    command.ExecuteNonQuery();
                }
                connection.Close();
            }
            return 0;
        }

        private enum InternalID
        {
            AUTH = 1, GET_ROOMS = 2, CREATE_ROOM = 3, JOIN_ROOM = 4, LEAVE_ROOM = 5, DELETE_ROOM = 6, ROOM_UPDATED = 7, KICK_FROM_ROOM = 8, STATUS_IN_ROOM = 9, START_ROOM = 10, SYNC_ROOM_PLAYER = 11, SET_HOST = 12, DESTROY_OBJECT = 13, CHANGE_OWNER = 14, CHANGE_OWNER_CONFIRM = 15, CREATE_PARTY = 16, INVITE_PARTY = 17, LEAVE_PARTY = 18, KICK_PARTY_MEMBER = 19, JOIN_MATCHMAKING = 20, LEAVE_MATCHMAKING = 21, PARTY_UPDATED = 22, GET_FRIENDS = 23, ADD_FRIEND = 24, REMOVE_FRIEND = 25, FRIEND_UPDATED = 26, GET_PROFILE = 27, ANSWER_PARTY_INVITE = 28
        }

        public static void ReceivedPacket(int clientID, Packet packet)
        {
            int id = packet.ReadInt();
            if(id == (int)InternalID.SYNC_ROOM_PLAYER)
            {
                int syScene = packet.ReadInt();
                int syDataLen1 = packet.ReadInt();
                int syDataLen2 = packet.ReadInt();
                int syDataLen3 = packet.ReadInt();
                byte[] syData1 = null;
                byte[] syData2 = null;
                byte[] syData3 = null;
                if (syDataLen1 > 0)
                {
                    syData1 = packet.ReadBytes(syDataLen1);
                }
                if (syDataLen2 > 0)
                {
                    syData2 = packet.ReadBytes(syDataLen2);
                }
                if (syDataLen3 > 0)
                {
                    syData3 = packet.ReadBytes(syDataLen3);
                }
                packet.Dispose();
                SyncPlayer(clientID, syScene, syData1, syData2, syData3);
            }
            else
            {
                switch ((InternalID)id)
                {
                    case InternalID.AUTH:
                        string authDevice = packet.ReadString();
                        bool authCreate = packet.ReadBool();
                        string authUser = packet.ReadString();
                        string authPass = packet.ReadString();
                        packet.Dispose();
                        _ = AuthAsync(clientID, authDevice, authUser, authPass, authCreate);
                        break;
                    case InternalID.CREATE_ROOM:
                        string crRoomPass = packet.ReadString();
                        int crRoomScene = packet.ReadInt();
                        int crRoomTeam = packet.ReadInt();
                        int crMaxPlayers = packet.ReadInt();
                        packet.Dispose();
                        _ = CreateRoomAsync(clientID, Server.clients[clientID].accountID, crRoomPass, crRoomScene, crRoomTeam, crMaxPlayers);
                        break;
                    case InternalID.GET_ROOMS:
                        packet.Dispose();
                        GetRooms(clientID);
                        break;
                    case InternalID.JOIN_ROOM:
                        string jnRoomID = packet.ReadString();
                        string jnRoomPass = packet.ReadString();
                        int hnRoomTeam = packet.ReadInt();
                        packet.Dispose();
                        _ = JoinRoomAsync(clientID, Server.clients[clientID].accountID, jnRoomID, jnRoomPass, hnRoomTeam);
                        break;
                    case InternalID.DELETE_ROOM:
                        packet.Dispose();
                        _ = DeleteRoomAsync(clientID);
                        break;
                    case InternalID.LEAVE_ROOM:
                        packet.Dispose();
                        _ = LeaveRoomAsync(clientID);
                        break;
                    case InternalID.KICK_FROM_ROOM:
                        long kcRoomTar = packet.ReadLong();
                        packet.Dispose();
                        _ = KickFromRoomAsync(clientID, kcRoomTar);
                        break;
                    case InternalID.STATUS_IN_ROOM:
                        bool stRoomRdy = packet.ReadBool();
                        packet.Dispose();
                        _ = ChangeRoomStatusAsync(clientID, stRoomRdy);
                        break;
                    case InternalID.START_ROOM:
                        packet.Dispose();
                        _ = StartRoomAsync(clientID);
                        break;
                    case InternalID.DESTROY_OBJECT:
                        int dsScene = packet.ReadInt();
                        string dsID = packet.ReadString();
                        Vector3 dsPis = packet.ReadVector3();
                        packet.Dispose();
                        DestroyObject(clientID, dsScene, dsID, dsPis);
                        break;
                    case InternalID.CHANGE_OWNER:
                        int coScene = packet.ReadInt();
                        int coDataLen = packet.ReadInt();
                        byte[] coData = packet.ReadBytes(coDataLen);
                        long coOwner = packet.ReadLong();
                        packet.Dispose();
                        ChangeOwner(clientID, coScene, coData, coOwner);
                        break;
                    case InternalID.CHANGE_OWNER_CONFIRM:
                        int cfScene = packet.ReadInt();
                        string cfID = packet.ReadString();
                        Vector3 cfPis = packet.ReadVector3();
                        long cfOwner = packet.ReadLong();
                        long cfAcc = packet.ReadLong();
                        packet.Dispose();
                        ChangeOwner(clientID, cfScene, cfID, cfPis, cfOwner, cfAcc);
                        break;
                    case InternalID.CREATE_PARTY:
                        int cpMaxPlayers = packet.ReadInt();
                        packet.Dispose();
                        _ = CreatePartyAsync(clientID, cpMaxPlayers);
                        break;
                    case InternalID.GET_FRIENDS:
                        packet.Dispose();
                        _ = GetFriendsAsync(clientID);
                        break;
                    case InternalID.GET_PROFILE:
                        int gpID = packet.ReadInt();
                        packet.Dispose();
                        _ = GetPlayerAsync(clientID, gpID);
                        break;
                    case InternalID.INVITE_PARTY:
                        long ipID = packet.ReadInt();
                        packet.Dispose();
                        _ = InvitePartyAsync(clientID, ipID);
                        break;
                    case InternalID.ANSWER_PARTY_INVITE:
                        string aipID = packet.ReadString();
                        bool aipAns = packet.ReadBool();
                        packet.Dispose();
                        _ = AnswerInvitePartyAsync(clientID, aipID, aipAns);
                        break;
                    case InternalID.KICK_PARTY_MEMBER:
                        long pkID = packet.ReadInt();
                        packet.Dispose();
                        _ = KickPartyAsync(clientID, pkID);
                        break;
                    case InternalID.LEAVE_PARTY:
                        packet.Dispose();
                        _ = LeavePartyAsync(clientID, true);
                        break;
                }
            }
        }

        private static void SendTCPData(int clientID, Packet packet)
        {
            if(packet == null)
            {
                return;
            }
            packet.SetID((int)Packet.ID.INTERNAL);
            packet.WriteLength();
            Server.clients[clientID].tcp.SendData(packet);
        }

        private static void SendUDPData(int clientID, Packet packet)
        {
            if (packet == null)
            {
                return;
            }
            packet.SetID((int)Packet.ID.INTERNAL);
            packet.WriteLength();
            Server.clients[clientID].udp.SendData(packet);
        }

        private async static Task<int> AuthAsync(int clientID, string device, string username, string password, bool create)
        {
            Task<int> task = Task.Run(() =>
            {
                return Retry.Do(() => _AuthAsync(clientID, device, username, password, create), TimeSpan.FromSeconds(0.1), 5, false);
            });
            return await task;
        }

        private static int _AuthAsync(int clientID, string device, string username, string password, bool create)
        {
            long id = -1;
            int response = 0;
            int banned = 0;
            using (var connection = Sqlite.connection)
            {
                connection.Open();
                if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
                {
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = string.Format(@"SELECT id, banned FROM accounts WHERE LOWER(username) = '{0}' AND password = '{1}';", username.ToLower(), password);
                        using (var reader = command.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                while (reader.Read())
                                {
                                    id = reader.GetInt64("id");
                                    banned = reader.GetInt32("banned");
                                }
                            }
                        }
                    }
                }
                if (id >= 0)
                {
                    if (banned > 0)
                    {
                        // Banned From Game
                        id = -1;
                        response = 7;
                    }
                    else
                    {
                        // Auth Successful
                        response = 1;
                    }
                }
                else
                {
                    if (create)
                    {
                        if (string.IsNullOrEmpty(username))
                        {
                            int count = 0;
                            using (var command = connection.CreateCommand())
                            {
                                command.CommandText = @"SELECT COUNT(id) AS count FROM accounts";
                                using (var reader = command.ExecuteReader())
                                {
                                    if (reader.HasRows)
                                    {
                                        while (reader.Read())
                                        {
                                            count = reader.GetInt32("count");
                                        }
                                    }
                                }
                            }
                            string token = Tools.GenerateToken().Substring(0, 5);
                            username = "Player_" + token + (count + 1).ToString();
                        }
                        using (var command = connection.CreateCommand())
                        {
                            command.CommandType = CommandType.Text;
                            command.CommandText = string.Format(@"SELECT id FROM accounts WHERE LOWER(username) = '{0}';", username.ToLower());
                            using (var reader = command.ExecuteReader())
                            {
                                if (reader.HasRows)
                                {
                                    while (reader.Read())
                                    {
                                        id = reader.GetInt64("id");
                                    }
                                }
                            }
                        }
                        if (id >= 0)
                        {
                            // Username is taken
                            id = -1;
                            response = 5;
                        }
                        else
                        {
                            if (string.IsNullOrEmpty(password))
                            {
                                password = Tools.EncrypteToMD5(Tools.GenerateToken());
                            }
                            using (var command = connection.CreateCommand())
                            {
                                command.CommandText = string.Format(@"INSERT INTO accounts (username, password) VALUES('{0}', '{1}'); SELECT LAST_INSERT_ROWID();", username, password);
                                id = Convert.ToInt64(command.ExecuteScalar());
                            }
                            if(id >= 0)
                            {
                                // Auth Successful
                                response = 1;
                            }
                        }
                    }
                    else
                    {
                        // Wrong Creds
                        response = 6;
                    }
                }
                if(id >= 0 && response == 1)
                {
                    Server.clients[clientID].accountID = id;
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = string.Format(@"UPDATE accounts SET device_id = '{0}', ip_address = '{1}', client_index = {2}, login_time = CURRENT_TIMESTAMP WHERE id = {3};", device, Server.clients[clientID].ipAddress, clientID, id);
                        command.ExecuteNonQuery();
                    }
                }
                connection.Close();
            }
            Packet packet = new Packet();
            packet.Write((int)InternalID.AUTH);
            packet.Write(response);
            packet.Write(id);
            packet.Write(banned);
            packet.Write(username);
            packet.Write(password);
            SendTCPData(clientID, packet);
            return 1;
        }

        private async static Task<int> GetFriendsAsync(int clientID)
        {
            Task<int> task = Task.Run(() =>
            {
                return Retry.Do(() => _GetFriendsAsync(clientID), TimeSpan.FromSeconds(0.1), 5, false);
            });
            return await task;
        }

        private static int _GetFriendsAsync(int clientID)
        {
            List<Data.Friend> friends = new List<Data.Friend>();
            int response = 0;
            using (var connection = Sqlite.connection)
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = string.Format(@"SELECT accounts.id, accounts.username, accounts.client_index FROM friends LEFT JOIN accounts ON (accounts.id = friends.account_id_1 OR accounts.id = friends.account_id_2) AND accounts.id != {0} WHERE (friends.account_id_1 = {0} OR friends.account_id_2 = {0}) AND friends.status > 0;", Server.clients[clientID].accountID);
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.HasRows)
                        {
                            while (reader.Read())
                            {
                                Data.Friend friend = new Data.Friend();
                                friend.id = reader.GetInt64("id");
                                friend.username = reader.GetString("username");
                                friend.online = reader.GetInt32("client_index") >= 0;
                                friends.Add(friend);
                            }
                        }
                    }
                }
                connection.Close();
            }
            response = friends.Count;
            Packet packet = new Packet();
            packet.Write((int)InternalID.GET_FRIENDS);
            packet.Write(response);
            if(response > 0)
            {
                byte[] data = Tools.Compress(Tools.Serialize<List<Data.Friend>>(friends));
                packet.Write(data.Length);
                packet.Write(data);
            }
            SendTCPData(clientID, packet);
            return response;
        }
        
        private async static Task<int> GetPlayerAsync(int clientID, long accountID)
        {
            Task<int> task = Task.Run(() =>
            {
                return Retry.Do(() => _GetPlayerAsync(clientID, accountID), TimeSpan.FromSeconds(0.1), 5, false);
            });
            return await task;
        }

        private static int _GetPlayerAsync(int clientID, long accountID)
        {
            Data.PlayerProfile profile = _GetPlayer(accountID);
            Packet packet = new Packet();
            packet.Write((int)InternalID.GET_FRIENDS);
            if (profile == null)
            {
                packet.Write(0);
            }
            else
            {
                packet.Write(1);
                byte[] data = Tools.Compress(Tools.Serialize<Data.PlayerProfile>(profile));
                packet.Write(data.Length);
                packet.Write(data);
            }
            SendTCPData(clientID, packet);
            return 1;
        }

        private static Data.PlayerProfile _GetPlayer(long accountID)
        {
            Data.PlayerProfile profile = null;
            using (var connection = Sqlite.connection)
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = string.Format(@"SELECT username, client_index, login_time FROM accounts WHERE id = {0};", accountID);
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.HasRows)
                        {
                            while (reader.Read())
                            {
                                profile = new Data.PlayerProfile();
                                profile.id = accountID;
                                profile.username = reader.GetString("username");
                                profile.online = reader.GetInt32("client_index") >= 0;
                                profile.login = reader.GetDateTime("login_time");
                                break;
                            }
                        }
                    }
                }
                connection.Close();
            }
            return profile;
        }

        private static List<Data.Room> rooms = new List<Data.Room>();

        private async static void GetRooms(int id)
        {
            byte[] bytes = await Tools.CompressAsync(await Tools.SerializeAsync<List<Data.Room>>(await GetRoomsAsync()));
            Packet packet = new Packet();
            packet.Write((int)InternalID.GET_ROOMS);
            packet.Write(bytes.Length);
            packet.Write(bytes);
            SendTCPData(id, packet);
        }

        private async static Task<List<Data.Room>> GetRoomsAsync()
        {
            Task<List<Data.Room>> task = Task.Run(() =>
            {
                List<Data.Room> _rooms = new List<Data.Room>();
                for (int i = 0; i < rooms.Count; i++)
                {
                    if (rooms[i] == null || rooms[i].started || rooms[i].type != Data.RoomType.HOST_MANAGED) continue;
                    _rooms.Add(rooms[i]);
                }
                return _rooms;
            });
            return await task;
        }

        private async static Task<int> CreateRoomAsync(int clientID, long account_id, string password, int gameID, int team, int maxPlayers)
        {
            Task<int> task = Task.Run(() =>
            {
                return Retry.Do(() => _CreateRoomAsync(clientID, account_id, password, gameID, team, maxPlayers), TimeSpan.FromSeconds(0.1), 1, false);
            });
            return await task;
        }

        private static int _CreateRoomAsync(int clientID, long accountID, string password, int gameID, int team, int maxPlayers)
        {
            int response = 0;
            Data.Room room = null;
            if (GetPlayerRoom(accountID) != null)
            {
                response = 4;
            }
            else
            {
                if(Server.clients[clientID].player == null)
                {
                    UpdatePlayer(clientID);
                }
                if (Server.clients[clientID].player != null)
                {
                    room = new Data.Room();
                    room.id = Guid.NewGuid().ToString();
                    room.type = Data.RoomType.HOST_MANAGED;
                    room.started = false;
                    room.gameID = gameID;
                    room.hostID = accountID;
                    room.hostUsername = Server.clients[clientID].player.username;
                    room.password = password;
                    if (maxPlayers < 0)
                    {
                        maxPlayers = 0;
                    }
                    room.maxPlayers = maxPlayers;
                    Server.clients[clientID].player.ready = false;
                    Server.clients[clientID].player.scene = -1;
                    Server.clients[clientID].player.team = team;
                    room.players.Add(Server.clients[clientID].player);
                    Server.clients[clientID].room = room;
                    rooms.Add(room);
                    response = 1;
                }
            }
            Packet packet = new Packet();
            packet.Write((int)InternalID.CREATE_ROOM);
            packet.Write(response);
            if (response == 1)
            {
                byte[] bytes = Tools.Compress(Tools.Serialize<Data.Room>(room));
                packet.Write(bytes.Length);
                packet.Write(bytes);
            }
            SendTCPData(clientID, packet);
            return response;
        }

        private static void UpdatePlayer(int clientID)
        {
            using (var connection = Sqlite.connection)
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = string.Format("SELECT username, client_index FROM accounts WHERE id = {0}", Server.clients[clientID].accountID);
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.HasRows)
                        {
                            while (reader.Read())
                            {
                                if (Server.clients[clientID].player == null)
                                {
                                    Server.clients[clientID].player = new Data.Player();
                                }
                                Server.clients[clientID].player.id = Server.clients[clientID].accountID;
                                Server.clients[clientID].player.username = reader.GetString("username");
                                Server.clients[clientID].player.client = clientID;
                                Server.clients[clientID].player.online = reader.GetInt32("client_index") >= 0;
                                break;
                            }
                        }
                    }
                }
                connection.Close();
            }
        }

        private static void SyncPlayer(int id, int scene, byte[] data1, byte[] data2, byte[] data3)
        {
            Task task = Task.Run(() =>
            {
                try
                {
                    if (Server.clients[id].room != null && Server.clients[id].room.started && Server.clients[id].player != null)
                    {
                        int sceneHost = -1;
                        bool checkHost = false;
                        bool setHost = false;
                        for (int i = 0; i < Server.clients[id].room.sceneHostsKeys.Count; i++)
                        {
                            if (Server.clients[id].room.sceneHostsKeys[i] == scene)
                            {
                                sceneHost = i;
                                break;
                            }
                        }
                        if(sceneHost >= 0)
                        {
                            if(Server.clients[id].room.sceneHostsValues[sceneHost] != Server.clients[id].accountID)
                            {
                                checkHost = true;
                            }
                        }
                        else
                        {
                            setHost = true;
                            sceneHost = Server.clients[id].room.sceneHostsKeys.Count;
                            Server.clients[id].room.sceneHostsKeys.Add(scene);
                            Server.clients[id].room.sceneHostsValues.Add(Server.clients[id].accountID);
                        }
                        Server.clients[id].player.scene = scene;
                        if (checkHost)
                        {
                            for (int i = 0; i < Server.clients[id].room.players.Count; i++)
                            {
                                if (Server.clients[id].room.players[i].id == Server.clients[id].accountID) { continue; }
                                if (Server.clients[id].room.players[i].id == Server.clients[id].room.sceneHostsValues[sceneHost])
                                {
                                    if (Server.clients[id].room.players[i].scene != scene || (DateTime.Now - Server.clients[Server.clients[id].room.players[i].client].lastTick).TotalSeconds > 1d)
                                    {
                                        setHost = true;
                                        Server.clients[id].room.sceneHostsValues[sceneHost] = Server.clients[id].accountID;
                                    }
                                    checkHost = false;
                                    break;
                                }
                            }
                        }
                        if (checkHost)
                        {
                            setHost = true;
                            Server.clients[id].room.sceneHostsValues[sceneHost] = Server.clients[id].accountID;
                        }
                        if (setHost)
                        {
                            Packet packet = new Packet();
                            packet.Write((int)InternalID.SET_HOST);
                            packet.Write(scene);
                            packet.Write(Server.clients[id].room.sceneHostsValues[sceneHost]);
                            SendTCPData(id, packet);
                        }
                        Server.clients[id].lastTick = DateTime.Now;
                        for (int i = 0; i < Server.clients[id].room.players.Count; i++)
                        {
                            if (Server.clients[id].room.players[i].id == Server.clients[id].accountID) { continue; }
                            Packet packet = new Packet();
                            packet.Write((int)InternalID.SYNC_ROOM_PLAYER);
                            packet.Write(scene);
                            packet.Write(Server.clients[id].accountID);
                            packet.Write(Server.clients[id].room.sceneHostsValues[sceneHost]);
                            packet.Write(data1 == null ? 0 : data1.Length);
                            packet.Write(data2 == null ? 0 : data2.Length);
                            packet.Write(data3 == null ? 0 : data3.Length);
                            if (data1 != null)
                            {
                                packet.Write(data1);
                            }
                            if (data2 != null)
                            {
                                packet.Write(data2);
                            }
                            if (data3 != null)
                            {
                                packet.Write(data3);
                            }
                            SendUDPData(Server.clients[id].room.players[i].client, packet);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message + "\n" + ex.StackTrace);
                }
            });
        }

        private static void DestroyObject(int id, int scene, string objectID, Vector3 position)
        {
            Task task = Task.Run(() =>
            {
                try
                {
                    if (Server.clients[id].room != null && Server.clients[id].room.started && Server.clients[id].player != null)
                    {
                        for (int i = 0; i < Server.clients[id].room.players.Count; i++)
                        {
                            if (Server.clients[id].room.players[i].id == Server.clients[id].accountID) { continue; }
                            Packet packet = new Packet();
                            packet.Write((int)InternalID.DESTROY_OBJECT);
                            packet.Write(scene);
                            packet.Write(Server.clients[id].accountID);
                            packet.Write(objectID);
                            packet.Write(position);
                            SendTCPData(Server.clients[id].room.players[i].client, packet);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message + "\n" + ex.StackTrace);
                }
            });
        }

        private static void ChangeOwner(int id, int scene, byte[] objects, long newOwner)
        {
            Task task = Task.Run(() =>
            {
                try
                {
                    if (Server.clients[id].room != null && Server.clients[id].room.started && Server.clients[id].player != null)
                    {
                        int sceneHost = -1;
                        for (int i = 0; i < Server.clients[id].room.sceneHostsKeys.Count; i++)
                        {
                            if (Server.clients[id].room.sceneHostsKeys[i] == scene)
                            {
                                sceneHost = i;
                                break;
                            }
                        }
                        if (sceneHost >= 0)
                        {
                            for (int i = 0; i < Server.clients[id].room.players.Count; i++)
                            {
                                if (Server.clients[id].room.players[i].id != Server.clients[id].room.sceneHostsValues[sceneHost]) { continue; }
                                Packet packet = new Packet();
                                packet.Write((int)InternalID.CHANGE_OWNER);
                                packet.Write(scene);
                                packet.Write(Server.clients[id].accountID);
                                packet.Write(objects.Length);
                                packet.Write(objects);
                                packet.Write(newOwner);
                                SendTCPData(Server.clients[id].room.players[i].client, packet);
                                break;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message + "\n" + ex.StackTrace);
                }
            });
        }

        private static void ChangeOwner(int id, int scene, string objectID, Vector3 position, long newOwner, long accountID)
        {
            Task task = Task.Run(() =>
            {
                try
                {
                    if (Server.clients[id].room != null && Server.clients[id].room.started && Server.clients[id].player != null)
                    {
                        for (int i = 0; i < Server.clients[id].room.players.Count; i++)
                        {
                            Packet packet = new Packet();
                            packet.Write((int)InternalID.CHANGE_OWNER_CONFIRM);
                            packet.Write(scene);
                            packet.Write(objectID);
                            packet.Write(position);
                            packet.Write(newOwner);
                            SendTCPData(Server.clients[id].room.players[i].client, packet);
                            break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message + "\n" + ex.StackTrace);
                }
            });
        }

        private static Data.Room GetPlayerRoom(long accountID)
        {
            for (int i = 0; i < rooms.Count; i++)
            {
                for (int j = 0; j < rooms[i].players.Count; j++)
                {
                    if (rooms[i].players[j].id == accountID)
                    {
                        return rooms[i];
                    }
                }
            }
            return null;
        }

        private async static Task<int> JoinRoomAsync(int clientID, long accountID, string roomID, string password, int team)
        {
            Task<int> task = Task.Run(() =>
            {
                return Retry.Do(() => _JoinRoomAsync(clientID, accountID, roomID, password, team), TimeSpan.FromSeconds(0.1), 5, false);
            });
            return await task;
        }

        public enum RoomUpdateType
        {
            UNKNOWN = 0, ROOM_DELETED = 1, PLAYER_JOINED = 2, PLAYER_LEFT = 3, PLAYER_STATUS_CHANGED = 4, PLAYER_KICKED = 5, GAME_STARTED = 6
        }

        public enum PartyUpdateType
        {
            PLAYER_JOINED = 1, PLAYER_LEFT = 2, PLAYER_KICKED = 3, MATCHMAKING_STARTED = 4, MATCHMAKING_STOPPED = 5
        }

        private static int _JoinRoomAsync(int clientID, long accountID, string roomID, string password, int team, bool notifyCallerInUpdate = true)
        {
            int response = 0;
            Data.Room room = null;
            if (GetPlayerRoom(accountID) != null)
            {
                // Already in Anorher Room
                response = 4;
            }
            else
            {
                for (int i = 0; i < rooms.Count; i++)
                {
                    if (rooms[i].id == roomID) 
                    {
                        if(rooms[i].maxPlayers > 0 && rooms[i].maxPlayers > rooms[i].players.Count)
                        {
                            if(rooms[i].started)
                            {
                                // Already Started
                                response = 7;
                            }
                            else if (string.IsNullOrEmpty(rooms[i].password) || rooms[i].password == password)
                            {
                                room = rooms[i];
                            }
                            else
                            {
                                // Wrong Password
                                response = 5;
                            }
                        }
                        else
                        {
                            // Full Capacity
                            response = 6;
                        }
                        break;
                    }
                }
                if(room != null)
                {
                    if (Server.clients[clientID].player == null)
                    {
                        UpdatePlayer(clientID);
                    }
                    if (Server.clients[clientID].player != null)
                    {
                        Server.clients[clientID].player.scene = -1;
                        Server.clients[clientID].player.ready = false;
                        Server.clients[clientID].player.team = team;
                        room.players.Add(Server.clients[clientID].player);
                        Server.clients[clientID].room = room;
                        response = 1;
                    }
                }
            }
            Packet packet = new Packet();
            packet.Write((int)InternalID.JOIN_ROOM);
            packet.Write(response);
            if (response == 1)
            {
                byte[] bytes = Tools.Compress(Tools.Serialize<Data.Room>(room));
                packet.Write(bytes.Length);
                packet.Write(bytes);
                byte[] playerBytes = Tools.Compress(Tools.Serialize<Data.Player>(Server.clients[clientID].player));
                for (int i = 0; i < room.players.Count; i++)
                {
                    if (room.players[i].id == accountID && !notifyCallerInUpdate) { continue; }
                    Packet othersPacket = new Packet();
                    othersPacket.Write((int)InternalID.ROOM_UPDATED);
                    othersPacket.Write((int)RoomUpdateType.PLAYER_JOINED);
                    othersPacket.Write(bytes.Length);
                    othersPacket.Write(bytes);
                    othersPacket.Write(playerBytes.Length);
                    othersPacket.Write(playerBytes);
                    SendTCPData(room.players[i].client, othersPacket);
                }
            }
            SendTCPData(clientID, packet);
            return response;
        }
        
        private async static Task<int> DeleteRoomAsync(int clientID)
        {
            Task<int> task = Task.Run(() =>
            {
                return Retry.Do(() => _DeleteRoomAsync(clientID), TimeSpan.FromSeconds(0.1), 5, false);
            });
            return await task;
        }

        private static int _DeleteRoomAsync(int clientID, bool hostLeave = false, bool notifyCallerInUpdate = true, bool notifyCaller = true)
        {
            int response = 0;
            Data.Room room = GetPlayerRoom(Server.clients[clientID].accountID);
            if (room == null)
            {
                // Not in Any Room
                response = 4;
            }
            else
            {
                if((room.hostID == Server.clients[clientID].accountID && room.type == Data.RoomType.HOST_MANAGED && !room.started) || (room.started && room.players.Count <= 1))
                {
                    response = 1;
                }
                else
                {
                    // Not Have Permission
                    response = 5;
                }
            }
            Packet packet = new Packet();
            packet.Write((int)InternalID.DELETE_ROOM);
            packet.Write(response);
            if (response == 1)
            {
                byte[] bytes = Tools.Compress(Tools.Serialize<Data.Room>(room));
                byte[] playerBytes = Tools.Compress(Tools.Serialize<Data.Player>(Server.clients[clientID].player));
                for (int i = 0; i < room.players.Count; i++)
                {
                    if (room.players[i].id == Server.clients[clientID].accountID && !notifyCallerInUpdate) { continue; }
                    Packet othersPacket = new Packet();
                    othersPacket.Write((int)InternalID.ROOM_UPDATED);
                    othersPacket.Write((int)RoomUpdateType.ROOM_DELETED);
                    othersPacket.Write(bytes.Length);
                    othersPacket.Write(bytes);
                    othersPacket.Write(playerBytes.Length);
                    othersPacket.Write(playerBytes);
                    SendTCPData(room.players[i].client, othersPacket);
                    Server.clients[room.players[i].client].room = null;
                }
                Server.clients[clientID].room = null;
                for (int i = 0; i < rooms.Count; i++)
                {
                    if (rooms[i].id == room.id)
                    {
                        rooms.RemoveAt(i);
                        break;
                    }
                }
            }
            if (!hostLeave && notifyCaller)
            {
                SendTCPData(clientID, packet);
            }
            return response;
        }

        private async static Task<int> LeaveRoomAsync(int clientID, bool notifyCallerInUpdate = true, bool notifyCaller = true)
        {
            Task<int> task = Task.Run(() =>
            {
                return Retry.Do(() => _LeaveRoomAsync(clientID, notifyCallerInUpdate, notifyCaller), TimeSpan.FromSeconds(0.1), 5, false);
            });
            return await task;
        }

        private static int _LeaveRoomAsync(int clientID, bool notifyCallerInUpdate = true, bool notifyCaller = true)
        {
            int response = 0;
            Data.Room room = GetPlayerRoom(Server.clients[clientID].accountID);
            bool deleted = false;
            if (room == null)
            {
                // Not in Any Room
                response = 4;
            }
            else
            {
                if ((room.hostID == Server.clients[clientID].accountID && room.type == Data.RoomType.HOST_MANAGED && !room.started) || (room.started && room.players.Count <= 1))
                {
                    deleted = (_DeleteRoomAsync(clientID, true, notifyCallerInUpdate, notifyCaller) == 1);
                }
                response = 1;
            }
            Packet packet = new Packet();
            packet.Write((int)InternalID.LEAVE_ROOM);
            packet.Write(response);
            if (response == 1)
            {
                if(!deleted)
                {
                    if (room.hostID == Server.clients[clientID].accountID)
                    {
                        for (int i = 0; i < room.players.Count; i++)
                        {
                            if (room.players[i].id != room.hostID)
                            {
                                room.hostID = room.players[i].id;
                                room.hostUsername = room.players[i].username;
                                break;
                            }
                        }
                    }
                    byte[] playerBytes = Tools.Compress(Tools.Serialize<Data.Player>(Server.clients[clientID].player));
                    room.players.Remove(Server.clients[clientID].player);
                    byte[] bytes = Tools.Compress(Tools.Serialize<Data.Room>(room));
                    for (int i = 0; i < room.players.Count; i++)
                    {
                        Packet othersPacket = new Packet();
                        othersPacket.Write((int)InternalID.ROOM_UPDATED);
                        othersPacket.Write((int)RoomUpdateType.PLAYER_LEFT);
                        othersPacket.Write(bytes.Length);
                        othersPacket.Write(bytes);
                        othersPacket.Write(playerBytes.Length);
                        othersPacket.Write(playerBytes);
                        SendTCPData(room.players[i].client, othersPacket);
                    }
                    if (notifyCallerInUpdate)
                    {
                        Packet othersPacket = new Packet();
                        othersPacket.Write((int)InternalID.ROOM_UPDATED);
                        othersPacket.Write((int)RoomUpdateType.PLAYER_LEFT);
                        othersPacket.Write(bytes.Length);
                        othersPacket.Write(bytes);
                        othersPacket.Write(playerBytes.Length);
                        othersPacket.Write(playerBytes);
                        SendTCPData(clientID, othersPacket);
                    }
                    Server.clients[clientID].room = null;
                }
            }
            if (notifyCaller)
            {
                SendTCPData(clientID, packet);
            }
            return response;
        }

        private async static Task<int> KickFromRoomAsync(int clientID, long targetID)
        {
            Task<int> task = Task.Run(() =>
            {
                return Retry.Do(() => _KickFromRoomAsync(clientID, targetID), TimeSpan.FromSeconds(0.1), 5, false);
            });
            return await task;
        }

        private static int _KickFromRoomAsync(int clientID, long targetID, bool notifyCallerInUpdate = true)
        {
            int response = 0;
            Data.Room room = GetPlayerRoom(Server.clients[clientID].accountID);
            Data.Player target = null;
            if (room == null)
            {
                // Not in Any Room
                response = 4;
            }
            else
            {
                if (room.hostID == Server.clients[clientID].accountID && room.type == Data.RoomType.HOST_MANAGED && !room.started && Server.clients[clientID].accountID != targetID)
                {
                    for (int i = 0; i < room.players.Count; i++)
                    {
                        if (room.players[i].id == targetID)
                        {
                            target = room.players[i];
                            break;
                        }
                    }
                    if(target == null)
                    {
                        // No Target
                        response = 6;
                    }
                    else
                    {
                        response = 1;
                    }
                }
                else
                {
                    // No Permission
                    response = 5;
                }
            }
            Packet packet = new Packet();
            packet.Write((int)InternalID.KICK_FROM_ROOM);
            packet.Write(response);
            if (response == 1)
            {
                if (room.hostID == target.id)
                {
                    for (int i = 0; i < room.players.Count; i++)
                    {
                        if (room.players[i].id != room.hostID)
                        {
                            room.hostID = room.players[i].id;
                            room.hostUsername = room.players[i].username;
                            break;
                        }
                    }
                }
                room.players.Remove(target);
                Server.clients[target.client].room = null;
                byte[] targetBytes = Tools.Compress(Tools.Serialize<Data.Player>(target));
                byte[] playerBytes = Tools.Compress(Tools.Serialize<Data.Player>(Server.clients[clientID].player));
                byte[] bytes = Tools.Compress(Tools.Serialize<Data.Room>(room));
                for (int i = 0; i < room.players.Count; i++)
                {
                    if (room.players[i].id == Server.clients[clientID].accountID && !notifyCallerInUpdate) { continue; }
                    Packet othersPacket = new Packet();
                    othersPacket.Write((int)InternalID.ROOM_UPDATED);
                    othersPacket.Write((int)RoomUpdateType.PLAYER_KICKED);
                    othersPacket.Write(bytes.Length);
                    othersPacket.Write(bytes);
                    othersPacket.Write(playerBytes.Length);
                    othersPacket.Write(playerBytes);
                    othersPacket.Write(targetBytes.Length);
                    othersPacket.Write(targetBytes);
                    SendTCPData(room.players[i].client, othersPacket);
                }
                Packet targrtPacket = new Packet();
                targrtPacket.Write((int)InternalID.ROOM_UPDATED);
                targrtPacket.Write((int)RoomUpdateType.PLAYER_KICKED);
                targrtPacket.Write(bytes.Length);
                targrtPacket.Write(bytes);
                targrtPacket.Write(playerBytes.Length);
                targrtPacket.Write(playerBytes);
                targrtPacket.Write(targetBytes.Length);
                targrtPacket.Write(targetBytes);
                SendTCPData(target.client, targrtPacket);
            }
            SendTCPData(clientID, packet);
            return response;
        }

        private async static Task<int> ChangeRoomStatusAsync(int clientID, bool ready)
        {
            Task<int> task = Task.Run(() =>
            {
                return Retry.Do(() => _ChangeRoomStatusAsync(clientID, ready), TimeSpan.FromSeconds(0.1), 5, false);
            });
            return await task;
        }

        private static int _ChangeRoomStatusAsync(int clientID, bool ready, bool notifyCallerInUpdate = true)
        {
            int response = 0;
            Data.Room room = GetPlayerRoom(Server.clients[clientID].accountID);
            if (room == null)
            {
                // Not in Any Room
                response = 4;
            }
            else
            {
                if(Server.clients[clientID].player.ready == ready)
                {
                    response = 5;
                }
                else
                {
                    Server.clients[clientID].player.ready = ready;
                    response = 1;
                }
            }
            Packet packet = new Packet();
            packet.Write((int)InternalID.STATUS_IN_ROOM);
            packet.Write(response);
            packet.Write(ready);
            if (response == 1)
            {
                byte[] playerBytes = Tools.Compress(Tools.Serialize<Data.Player>(Server.clients[clientID].player));
                byte[] bytes = Tools.Compress(Tools.Serialize<Data.Room>(room));
                for (int i = 0; i < room.players.Count; i++)
                {
                    if (room.players[i].id == Server.clients[clientID].accountID && !notifyCallerInUpdate) { continue; }
                    Packet othersPacket = new Packet();
                    othersPacket.Write((int)InternalID.ROOM_UPDATED);
                    othersPacket.Write((int)RoomUpdateType.PLAYER_STATUS_CHANGED);
                    othersPacket.Write(bytes.Length);
                    othersPacket.Write(bytes);
                    othersPacket.Write(playerBytes.Length);
                    othersPacket.Write(playerBytes);
                    SendTCPData(room.players[i].client, othersPacket);
                }
            }
            SendTCPData(clientID, packet);
            return response;
        }

        private async static Task<int> StartRoomAsync(int clientID)
        {
            Task<int> task = Task.Run(() =>
            {
                return Retry.Do(() => _StartRoomAsync(clientID), TimeSpan.FromSeconds(0.1), 5, false);
            });
            return await task;
        }

        private static int _StartRoomAsync(int clientID, bool notifyCallerInUpdate = true)
        {
            int response = 0;
            Data.Room room = GetPlayerRoom(Server.clients[clientID].accountID);
            if (room == null)
            {
                // Not in Any Room
                response = 4;
            }
            else
            {
                if (room.hostID == Server.clients[clientID].accountID && room.type == Data.RoomType.HOST_MANAGED)
                {
                    if(room.started)
                    {
                        // Already Started
                        response = 6;
                    }
                    else
                    {
                        room.started = true;
                        response = 1;
                    }
                }
                else
                {
                    // No Permission
                    response = 5;
                }
            }
            Packet packet = new Packet();
            packet.Write((int)InternalID.START_ROOM);
            packet.Write(response);
            if (response == 1)
            {
                byte[] playerBytes = Tools.Compress(Tools.Serialize<Data.Player>(Server.clients[clientID].player));
                byte[] bytes = Tools.Compress(Tools.Serialize<Data.Room>(room));
                for (int i = 0; i < room.players.Count; i++)
                {
                    if (room.players[i].id == Server.clients[clientID].accountID && !notifyCallerInUpdate) { continue; }
                    Packet othersPacket = new Packet();
                    othersPacket.Write((int)InternalID.ROOM_UPDATED);
                    othersPacket.Write((int)RoomUpdateType.GAME_STARTED);
                    othersPacket.Write(bytes.Length);
                    othersPacket.Write(bytes);
                    othersPacket.Write(playerBytes.Length);
                    othersPacket.Write(playerBytes);
                    SendTCPData(room.players[i].client, othersPacket);
                }
            }
            SendTCPData(clientID, packet);
            return response;
        }

    }
}