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

        #region Internal
        public const bool enabled = true;

        public static void Initialize()
        {
            games.Clear();
            parties.Clear();
            rooms.Clear();
            Sqlite.Initialize();
            Netcode.Start();
        }

        public static void OnExit()
        {
            Netcode.OnExit();
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

        private static List<Data.Room> rooms = new List<Data.Room>();
        private static List<Data.Game> games = new List<Data.Game>();
        private static bool updateMatchmaking = false;
        private static bool matchmaking = false;
        private static List<Data.Party> parties = new List<Data.Party>();

        private static void Matchmaking()
        {
            List<MatchData> searchingMatches = new List<MatchData>();
            List<MatchData> readyMatches = new List<MatchData>();
            for (int i = 0; i < parties.Count; i++)
            {
                bool added = false;
                for (int j = 0; j < searchingMatches.Count; j++)
                {
                    if (searchingMatches[j].extension == parties[i].extension && searchingMatches[j].teamsPerMatch == parties[i].teamsPerMatch && searchingMatches[j].playersPerTeam == parties[i].playersPerTeam && searchingMatches[j].gameID == parties[i].gameID && searchingMatches[j].mapID == parties[i].mapID)
                    {
                        for (int k = 0; k < searchingMatches[j].teams.Length; k++)
                        {
                            int c = 0;
                            for (int p = 0; p < searchingMatches[j].teams[k].parties.Count; p++)
                            {
                                c += searchingMatches[j].teams[k].parties[p].players.Count;
                            }
                            if(parties[i].players.Count <= searchingMatches[j].playersPerTeam - c)
                            {
                                searchingMatches[j].teams[k].parties.Add(parties[i]);
                                searchingMatches[j].addedPlayers += parties[i].players.Count;
                                if(searchingMatches[j].addedPlayers == searchingMatches[j].playersPerTeam * searchingMatches[j].teamsPerMatch)
                                {
                                    readyMatches.Add(searchingMatches[j]);
                                    searchingMatches.RemoveAt(j);
                                }
                                added = true;
                                break;
                            }
                        }
                    }
                    if (added)
                    {
                        break;
                    }
                }
                if(!added)
                {
                    MatchData data = new MatchData();
                    data.teamsPerMatch = parties[i].teamsPerMatch;
                    data.playersPerTeam = parties[i].playersPerTeam;
                    data.gameID = parties[i].gameID;
                    data.mapID = parties[i].mapID;
                    data.extension = parties[i].extension;
                    data.addedPlayers = 0;
                    data.teams = new MatchTeam[data.teamsPerMatch];
                    for (int k = 0; k < data.teams.Length; k++)
                    {
                        data.teams[k] = new MatchTeam();
                    }
                    data.teams[0].parties.Add(parties[i]);
                    data.addedPlayers += parties[i].players.Count;
                    if (data.addedPlayers == data.playersPerTeam * data.teamsPerMatch)
                    {
                        readyMatches.Add(data);
                    }
                    else
                    {
                        searchingMatches.Add(data);
                    }
                }
            }
            for (int i = 0; i < readyMatches.Count; i++)
            {
                Data.Game game = new Data.Game();
                game.room = new Data.Room();
                game.type = Data.GameType.MATCHED;
                game.room.gameID = readyMatches[i].gameID;
                game.room.mapID = readyMatches[i].mapID;
                game.room.maxPlayers = readyMatches[i].addedPlayers;
                game.extension = readyMatches[i].extension;
                for (int j = 0; j < readyMatches[i].teams.Length; j++)
                {
                    for (int k = 0; k < readyMatches[i].teams[j].parties.Count; k++)
                    {
                        parties.Remove(readyMatches[i].teams[j].parties[k]);
                        for (int l = 0; l < readyMatches[i].teams[j].parties[k].players.Count; l++)
                        {
                            readyMatches[i].teams[j].parties[k].players[l].team = j + 1;
                            Server.clients[readyMatches[i].teams[j].parties[k].players[l].client].game = game;
                            game.room.players.Add(readyMatches[i].teams[j].parties[k].players[l]);
                        }
                    }
                }
                game.room.hostID = game.room.players[0].id;
                game.room.hostUsername = game.room.players[0].username;
                game.room.id = Guid.NewGuid().ToString();
                game.start = DateTime.Now;
                if(game.extension == Data.Extension.NETCODE_SERVER)
                {
                    Netcode.StartGame(game);
                }
                else
                {
                    games.Add(game);
                    byte[] bytes = Tools.Compress(Tools.Serialize<Data.RuntimeGame>(GetStartGameData(game)));
                    for (int j = 0; j < game.room.players.Count; j++)
                    {
                        Packet packet = new Packet();
                        packet.Write((int)InternalID.GAME_STARTED);
                        packet.Write(bytes.Length);
                        packet.Write(bytes);
                        SendTCPData(game.room.players[j].client, packet);
                    }
                }
            }
        }

        private class MatchData
        {
            public int gameID = 0;
            public int mapID = 0;
            public Data.Extension extension = Data.Extension.NONE;
            public int teamsPerMatch = 2;
            public int playersPerTeam = 6;
            public int addedPlayers = 0;
            public MatchTeam[] teams = null;
        }

        private class MatchTeam
        {
            public List<Data.Party> parties =  new List<Data.Party>();
        }

        private async static Task<int> StartMatchmakingAsync(int clientID, int gameID, int mapID, Data.Extension extension)
        {
            Task<int> task = Task.Run(() =>
            {
                return _StartMatchmakingAsync(clientID, gameID, mapID, extension);
            });
            return await task;
        }

        private static int _StartMatchmakingAsync(int clientID, int gameID, int mapID, Data.Extension extension)
        {
            int response = 0;
            if (Server.clients[clientID].party == null)
            {
                Server.clients[clientID].party = new Data.Party();
                Server.clients[clientID].party.id = Guid.NewGuid().ToString();
                Server.clients[clientID].party.auto = true;
                Server.clients[clientID].party.leaderID = Server.clients[clientID].accountID;
                Server.clients[clientID].party.maxPlayers = 1;
                if (Server.clients[clientID].player == null)
                {
                    UpdatePlayer(clientID);
                }
                Server.clients[clientID].party.players.Add(Server.clients[clientID].player);
            }
            Server.clients[clientID].party.extension = extension;
            if (!parties.Contains(Server.clients[clientID].party))
            {
                if (!Server.clients[clientID].party.matchmaking)
                {
                    if (Server.clients[clientID].party.leaderID == Server.clients[clientID].accountID)
                    {
                        var match = Terminal.OverrideMatchmaking(gameID, mapID);
                        if(match.Item1 < 1)
                        {
                            match.Item1 = 1;
                        }
                        if (match.Item2 < 1)
                        {
                            match.Item2 = 1;
                        }
                        if(match.Item2 >= Server.clients[clientID].party.players.Count)
                        {
                            Server.clients[clientID].party.matchmaking = true;
                            Server.clients[clientID].party.teamsPerMatch = match.Item1;
                            Server.clients[clientID].party.playersPerTeam = match.Item2;
                            parties.Add(Server.clients[clientID].party);
                            response = 1;
                            updateMatchmaking = true;
                            // byte[] bytes = Tools.Compress(Tools.Serialize<Data.Party>(Server.clients[clientID].party));
                            for (int i = 0; i < Server.clients[clientID].party.players.Count; i++)
                            {
                                Packet packetUp = new Packet();
                                packetUp.Write((int)InternalID.MATCHMAKING_STARTED);
                                // packetUp.Write(bytes.Length);
                                // packetUp.Write(bytes);
                                SendTCPData(Server.clients[clientID].party.players[i].client, packetUp);
                            }
                        }
                        else
                        {
                            response = 8;
                        }
                    }
                    else
                    {
                        response = 5;
                    }
                }
                else
                {
                    response = 7;
                }
            }
            else
            {
                response = 6;
            }
            Packet packet = new Packet();
            packet.Write((int)InternalID.JOIN_MATCHMAKING);
            packet.Write(response);
            SendTCPData(clientID, packet);
            return response;
        }

        private async static Task<int> StopMatchmakingAsync(int clientID)
        {
            Task<int> task = Task.Run(() =>
            {
                return _StopMatchmakingAsync(clientID);
            });
            return await task;
        }

        private static int _StopMatchmakingAsync(int clientID)
        {
            int response = 0;
            if (Server.clients[clientID].party != null)
            {
                if (parties.Contains(Server.clients[clientID].party))
                {
                    if (Server.clients[clientID].party.matchmaking)
                    {
                        if (Server.clients[clientID].party.leaderID == Server.clients[clientID].accountID)
                        {
                            Server.clients[clientID].party.matchmaking = false;
                            parties.Remove(Server.clients[clientID].party);
                            response = 1;
                            updateMatchmaking = true;
                            // byte[] bytes = Tools.Compress(Tools.Serialize<Data.Party>(Server.clients[clientID].party));
                            for (int i = 0; i < Server.clients[clientID].party.players.Count; i++)
                            {
                                Packet packetUp = new Packet();
                                packetUp.Write((int)InternalID.MATCHMAKING_STOPPED);
                                // packetUp.Write(bytes.Length);
                                // packetUp.Write(bytes);
                                SendTCPData(Server.clients[clientID].party.players[i].client, packetUp);
                            }
                            if (Server.clients[clientID].party.auto)
                            {
                                if(Server.clients[clientID].party.players.Count > 1)
                                {
                                    Server.clients[clientID].party.auto = false;
                                }
                                else
                                {
                                    Server.clients[clientID].party.players.Clear();
                                    Server.clients[clientID].party = null;
                                }
                            }
                        }
                        else
                        {
                            response = 5;
                        }
                    }
                    else
                    {
                        response = 7;
                    }
                }
                else
                {
                    response = 6;
                }
            }
            else
            {
                response = 4;
            }
            Packet packet = new Packet();
            packet.Write((int)InternalID.LEAVE_MATCHMAKING);
            packet.Write(response);
            SendTCPData(clientID, packet);
            return response;
        }

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
                if (Server.clients[clientID].room == null)
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
                                            if (Server.clients[clientID].party.players[i].id == Server.clients[clientID].accountID)
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
                    response = 7;
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
                if (Server.clients[clientID].room == null)
                {
                    Server.clients[clientID].party = new Data.Party();
                    Server.clients[clientID].party.id = Guid.NewGuid().ToString();
                    Server.clients[clientID].party.auto = false;
                    Server.clients[clientID].party.matchmaking = false;
                    Server.clients[clientID].party.leaderID = Server.clients[clientID].accountID;
                    if (maxPlayers < 0)
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
                    // In a room
                    response = 5;
                }
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
            Server.clients[id].game = null;
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
                if (Server.clients[id].game != null)
                {
                    await LeaveGameAsync(id, false, false, true);
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

        public enum InternalID
        {
            AUTH = 1, GET_ROOMS = 2, CREATE_ROOM = 3, JOIN_ROOM = 4, LEAVE_ROOM = 5, DELETE_ROOM = 6, ROOM_UPDATED = 7, KICK_FROM_ROOM = 8, STATUS_IN_ROOM = 9, START_ROOM = 10, SYNC_GAME = 11, SET_HOST = 12, DESTROY_OBJECT = 13, CHANGE_OWNER = 14, CHANGE_OWNER_CONFIRM = 15, CREATE_PARTY = 16, INVITE_PARTY = 17, LEAVE_PARTY = 18, KICK_PARTY_MEMBER = 19, JOIN_MATCHMAKING = 20, LEAVE_MATCHMAKING = 21, PARTY_UPDATED = 22, GET_FRIENDS = 23, ADD_FRIEND = 24, REMOVE_FRIEND = 25, ANSWER_FRIEND = 26, GET_PROFILE = 27, ANSWER_PARTY_INVITE = 28, MATCHMAKING_STARTED = 29, MATCHMAKING_STOPPED = 30, LEAVE_GAME = 31, GAME_STARTED = 32, NETCODE_INIT = 33, NETCODE_STARTED = 34, FRIEND_REQUESTS = 35, PURCHASE = 36, GET_CHARACTERS = 37, GET_EQUIPMENTS = 38, SET_CHARACTER_SELECTED = 39, CHARACTER_EQUIP = 40, CHARACTER_UNEQUIP = 41
        }

        public static void ReceivedPacket(int clientID, Packet packet)
        {
            int id = packet.ReadInt();
            if(id == (int)InternalID.SYNC_GAME)
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
                        int crRoomGame = packet.ReadInt();
                        int crRoomMap = packet.ReadInt();
                        int crRoomTeam = packet.ReadInt();
                        int crMaxPlayers = packet.ReadInt();
                        packet.Dispose();
                        _ = CreateRoomAsync(clientID, Server.clients[clientID].accountID, crRoomPass, crRoomGame, crRoomMap, crRoomTeam, crMaxPlayers);
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
                        int stEx = packet.ReadInt();
                        packet.Dispose();
                        _ = StartRoomAsync(clientID, (Data.Extension)stEx);
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
                    case InternalID.ADD_FRIEND:
                        long afID = packet.ReadLong();
                        packet.Dispose();
                        _ = AddFriendAsync(clientID, afID);
                        break;
                    case InternalID.REMOVE_FRIEND:
                        long rfID = packet.ReadLong();
                        packet.Dispose();
                        _ = RemoveFriendAsync(clientID, rfID);
                        break;
                    case InternalID.ANSWER_FRIEND:
                        long wfID = packet.ReadLong();
                        bool wfRes = packet.ReadBool();
                        packet.Dispose();
                        _ = AnswerFriendAsync(clientID, wfID, wfRes);
                        break;
                    case InternalID.FRIEND_REQUESTS:
                        bool fqSelf = packet.ReadBool();
                        packet.Dispose();
                        _ = GetFriendRequestsAsync(clientID, fqSelf);
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
                    case InternalID.JOIN_MATCHMAKING:
                        int gameID = packet.ReadInt();
                        int mapID = packet.ReadInt();
                        int mapEx = packet.ReadInt();
                        packet.Dispose();
                        _ = StartMatchmakingAsync(clientID, gameID, mapID, (Data.Extension)mapEx);
                        break;
                    case InternalID.LEAVE_MATCHMAKING:
                        packet.Dispose();
                        _ = StopMatchmakingAsync(clientID);
                        break;
                    case InternalID.LEAVE_GAME:
                        packet.Dispose();
                        _ = LeaveGameAsync(clientID, true, true, true);
                        break;
                    case InternalID.PURCHASE:
                        int purCat = packet.ReadInt();
                        int purID = packet.ReadInt();
                        int purLvl = packet.ReadInt();
                        int purCur = packet.ReadInt();
                        packet.Dispose();
                        _ = PurchaseAsync(clientID, Server.clients[clientID].accountID, purCat, purID, purLvl, purCur);
                        break;
                    case InternalID.GET_CHARACTERS:
                        long gcID = packet.ReadLong();
                        bool onlySelected = packet.ReadBool();
                        bool includeEquipments = packet.ReadBool();
                        packet.Dispose();
                        _ = GetCharactersAsync(clientID, gcID, onlySelected, includeEquipments);
                        break;
                    case InternalID.GET_EQUIPMENTS:
                        long geID = packet.ReadLong();
                        bool geEx = packet.ReadBool();
                        packet.Dispose();
                        _ = GetEquipmentsAsync(clientID, geID, geEx);
                        break;
                    case InternalID.CHARACTER_EQUIP:
                        long ceIDc = packet.ReadLong();
                        long ceIDe = packet.ReadLong();
                        bool ceOth = packet.ReadBool();
                        packet.Dispose();
                        _ = EquipCharacterAsync(clientID, Server.clients[clientID].accountID, ceIDc, ceIDe, ceOth);
                        break;
                    case InternalID.CHARACTER_UNEQUIP:
                        long cuIDc = packet.ReadLong();
                        long cvuIDe = packet.ReadLong();
                        packet.Dispose();
                        _ = UnquipCharacterAsync(clientID, Server.clients[clientID].accountID, cuIDc, cvuIDe);
                        break;
                    case InternalID.SET_CHARACTER_SELECTED:
                        long cseIDc = packet.ReadLong();
                        bool cseISt = packet.ReadBool();
                        bool cseIOt = packet.ReadBool();
                        packet.Dispose();
                        _ = CharacterSelectStatusAsync(clientID, Server.clients[clientID].accountID, cseIDc, cseISt, cseIOt);
                        break;
                }
            }
        }

        private async static Task<int> CharacterSelectStatusAsync(int clientID, long accountID, long characterID, bool selected, bool deselectOthers)
        {
            Task<int> task = Task.Run(() =>
            {
                return Retry.Do(() => _CharacterSelectStatusAsync(clientID, accountID, characterID, selected, deselectOthers), TimeSpan.FromSeconds(0.1), 5, false);
            });
            return await task;
        }

        private static int _CharacterSelectStatusAsync(int clientID, long accountID, long characterID, bool selected, bool deselectOthers)
        {
            int response = 0;
            using (var connection = Sqlite.connection)
            {
                connection.Open();
                bool isOwner = true;
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = string.Format(@"SELECT id FROM characters WHERE id = {0} AND account_id = {1};", characterID, accountID);
                    using (var reader = command.ExecuteReader())
                    {
                        if (!reader.HasRows)
                        {
                            isOwner = false;
                        }
                    }
                }
                if (isOwner)
                {
                    if (deselectOthers)
                    {
                        using (var command = connection.CreateCommand())
                        {
                            command.CommandText = string.Format(@"UPDATE characters SET selected = 0 WHERE account_id = {0};", accountID);
                            command.ExecuteNonQuery();
                        }
                    }
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = string.Format(@"UPDATE characters SET selected = {0} WHERE id = {1};", selected ? 1 : 0, characterID);
                        command.ExecuteNonQuery();
                    }
                    response = 1;
                }
                else
                {
                    response = 4;
                }
                connection.Close();
            }
            Packet packet = new Packet();
            packet.Write((int)InternalID.SET_CHARACTER_SELECTED);
            packet.Write(response);
            SendTCPData(clientID, packet);
            return response;
        }

        private async static Task<int> UnquipCharacterAsync(int clientID, long accountID, long characterID, long equipmentID)
        {
            Task<int> task = Task.Run(() =>
            {
                return Retry.Do(() => _UnquipCharacterAsync(clientID, accountID, characterID, equipmentID), TimeSpan.FromSeconds(0.1), 5, false);
            });
            return await task;
        }

        private static int _UnquipCharacterAsync(int clientID, long accountID, long characterID, long equipmentID)
        {
            int response = 0;
            using (var connection = Sqlite.connection)
            {
                connection.Open();
                bool isOwner = true;
                /*
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = string.Format(@"SELECT id FROM characters WHERE id = {0} AND account_id = {1};", characterID, accountID);
                    using (var reader = command.ExecuteReader())
                    {
                        if (!reader.HasRows)
                        {
                            isOwner = false;
                        }
                    }
                }
                */
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = string.Format(@"SELECT id FROM equipments WHERE id = {0} AND account_id = {1};", equipmentID, accountID);
                    using (var reader = command.ExecuteReader())
                    {
                        if (!reader.HasRows)
                        {
                            isOwner = false;
                        }
                    }
                }
                if (isOwner)
                {
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = string.Format(@"UPDATE equipments SET character_id = 0 WHERE id = {0};", equipmentID);
                        command.ExecuteNonQuery();
                    }
                    response = 1;
                }
                else
                {
                    response = 4;
                }
                connection.Close();
            }
            Packet packet = new Packet();
            packet.Write((int)InternalID.CHARACTER_UNEQUIP);
            packet.Write(response);
            SendTCPData(clientID, packet);
            return response;
        }

        private async static Task<int> EquipCharacterAsync(int clientID, long accountID, long characterID, long equipmentID, bool unequipOthersOfThisType)
        {
            Task<int> task = Task.Run(() =>
            {
                return Retry.Do(() => _EquipCharacterAsync(clientID, accountID, characterID, equipmentID, unequipOthersOfThisType), TimeSpan.FromSeconds(0.1), 5, false);
            });
            return await task;
        }

        private static int _EquipCharacterAsync(int clientID, long accountID, long characterID, long equipmentID, bool unequipOthersOfThisType)
        {
            int response = 0;
            using (var connection = Sqlite.connection)
            {
                connection.Open();
                bool isOwner = true;
                int type = 0;
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = string.Format(@"SELECT id FROM characters WHERE id = {0} AND account_id = {1};", characterID, accountID);
                    using (var reader = command.ExecuteReader())
                    {
                        if (!reader.HasRows)
                        {
                            isOwner = false;
                        }
                    }
                }
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = string.Format(@"SELECT type FROM equipments WHERE id = {0} AND account_id = {1};", equipmentID, accountID);
                    using (var reader = command.ExecuteReader())
                    {
                        if (!reader.HasRows)
                        {
                            isOwner = false;
                        }
                        else
                        {
                            while (reader.Read())
                            {
                                type = reader.GetInt32("type");
                            }
                        }
                    }
                }
                if (isOwner)
                {
                    if (unequipOthersOfThisType)
                    {
                        using (var command = connection.CreateCommand())
                        {
                            command.CommandText = string.Format(@"UPDATE equipments SET character_id = 0 WHERE account_id = {0} AND character_id = {1} AND type = {2};", accountID, characterID, type);
                            command.ExecuteNonQuery();
                        }
                    }
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = string.Format(@"UPDATE equipments SET character_id = {0} WHERE id = {1};", characterID, equipmentID);
                        command.ExecuteNonQuery();
                    }
                    response = 1;
                }
                else
                {
                    response = 4;
                }
                connection.Close();
            }
            Packet packet = new Packet();
            packet.Write((int)InternalID.CHARACTER_EQUIP);
            packet.Write(response);
            SendTCPData(clientID, packet);
            return response;
        }

        private async static Task<int> GetEquipmentsAsync(int clientID, long id, bool excludeEquipped)
        {
            Task<int> task = Task.Run(() =>
            {
                return Retry.Do(() => _GetEquipmentsAsync(clientID, id, excludeEquipped), TimeSpan.FromSeconds(0.1), 5, false);
            });
            return await task;
        }

        private static int _GetEquipmentsAsync(int clientID, long id, bool excludeEquipped)
        {
            List<Data.RuntimeEquipment> equipments = new List<Data.RuntimeEquipment>();
            int response = 1;
            using (var connection = Sqlite.connection)
            {
                connection.Open();
                equipments = GetRuntimeEquipments(id, 0, excludeEquipped, connection);
                connection.Close();
            }
            Packet packet = new Packet();
            packet.Write((int)InternalID.GET_EQUIPMENTS);
            packet.Write(response);
            byte[] data = Tools.Compress(Tools.Serialize<List<Data.RuntimeEquipment>>(equipments));
            packet.Write(data.Length);
            packet.Write(data);
            SendTCPData(clientID, packet);
            return response;
        }

        private async static Task<int> GetCharactersAsync(int clientID, long id, bool onlySelected, bool includeEquipments)
        {
            Task<int> task = Task.Run(() =>
            {
                return Retry.Do(() => _GetCharactersAsync(clientID, id, onlySelected, includeEquipments), TimeSpan.FromSeconds(0.1), 5, false);
            });
            return await task;
        }

        private static int _GetCharactersAsync(int clientID, long id, bool onlySelected, bool includeEquipments)
        {
            List<Data.RuntimeCharacter> characters = new List<Data.RuntimeCharacter>();
            int response = 1;
            using (var connection = Sqlite.connection)
            {
                connection.Open();
                characters = GetRuntimeCharacters(id, onlySelected, includeEquipments, connection);
                connection.Close();
            }
            Packet packet = new Packet();
            packet.Write((int)InternalID.GET_CHARACTERS);
            packet.Write(response);
            byte[] data = Tools.Compress(Tools.Serialize<List<Data.RuntimeCharacter>>(characters));
            packet.Write(data.Length);
            packet.Write(data);
            SendTCPData(clientID, packet);
            return response;
        }

        public static void SendTCPData(int clientID, Packet packet)
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
            bool online = false;
            Data.PlayerProfile profile = null;
            using (var connection = Sqlite.connection)
            {
                bool signup = false;
                connection.Open();
                if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
                {
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = string.Format(@"SELECT id, client_index, banned FROM accounts WHERE LOWER(username) = '{0}' AND password = '{1}';", username.ToLower(), password);
                        using (var reader = command.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                while (reader.Read())
                                {
                                    id = reader.GetInt64("id");
                                    banned = reader.GetInt32("banned");
                                    online = reader.GetInt32("client_index") > 0;
                                }
                            }
                        }
                    }
                }
                if (id >= 0)
                {
                    if (online)
                    {
                        // Another User Is Online
                        id = -1;
                        response = 3;
                    }
                    else if (banned > 0)
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
                            username = "Player"  + (count + 1).ToString("D8");
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
                                signup = true;
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
                    Terminal.OnAuthenticated(id, signup, connection);
                }
                profile = _GetPlayer(id, connection);
                connection.Close();
            }
            Packet packet = new Packet();
            packet.Write((int)InternalID.AUTH);
            packet.Write(response);
            packet.Write(id);
            packet.Write(banned);
            packet.Write(username);
            packet.Write(password);
            if (response == 1 && profile != null)
            {
                byte[] data = Tools.Compress(Tools.Serialize<Data.PlayerProfile>(profile));
                packet.Write(data.Length);
                packet.Write(data);
            }
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
        
        private async static Task<int> GetFriendRequestsAsync(int clientID, bool self)
        {
            Task<int> task = Task.Run(() =>
            {
                return Retry.Do(() => _GetFriendRequestsAsync(clientID, self), TimeSpan.FromSeconds(0.1), 5, false);
            });
            return await task;
        }

        private static int _GetFriendRequestsAsync(int clientID, bool self)
        {
            List<Data.FriendRequest> requests = new List<Data.FriendRequest>();
            int response = 0;
            using (var connection = Sqlite.connection)
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    if (self)
                    {
                        command.CommandText = string.Format(@"SELECT accounts.id, accounts.username, accounts.client_index, friends.action_time FROM friends LEFT JOIN accounts ON accounts.id = friends.account_id_2 WHERE friends.account_id_1 = {0} AND friends.status <= 0;", Server.clients[clientID].accountID);
                    }
                    else
                    {
                        command.CommandText = string.Format(@"SELECT friends.id AS request, accounts.id, accounts.username, accounts.client_index, friends.action_time FROM friends LEFT JOIN accounts ON accounts.id = friends.account_id_1 WHERE friends.account_id_2 = {0} AND friends.status <= 0;", Server.clients[clientID].accountID);
                    }
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.HasRows)
                        {
                            while (reader.Read())
                            {
                                Data.FriendRequest request = new Data.FriendRequest();
                                request.id = reader.GetInt64("request");
                                request.playerID = reader.GetInt64("id");
                                request.username = reader.GetString("username");
                                request.online = reader.GetInt32("client_index") >= 0;
                                request.time = reader.GetDateTime("action_time");
                                requests.Add(request);
                            }
                        }
                    }
                }
                connection.Close();
            }
            response = requests.Count;
            Packet packet = new Packet();
            packet.Write((int)InternalID.FRIEND_REQUESTS);
            packet.Write(response);
            packet.Write(self);
            if (response > 0)
            {
                byte[] data = Tools.Compress(Tools.Serialize<List<Data.FriendRequest>>(requests));
                packet.Write(data.Length);
                packet.Write(data);
            }
            SendTCPData(clientID, packet);
            return response;
        }

        private async static Task<int> AddFriendAsync(int clientID, long id)
        {
            Task<int> task = Task.Run(() =>
            {
                return Retry.Do(() => _AddFriendAsyncAsync(clientID, id), TimeSpan.FromSeconds(0.1), 5, false);
            });
            return await task;
        }

        private static int _AddFriendAsyncAsync(int clientID, long id)
        {
            int response = 0;
            if(id != Server.clients[clientID].accountID && Server.clients[clientID].accountID >= 0)
            {
                using (var connection = Sqlite.connection)
                {
                    connection.Open();
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = string.Format(@"SELECT id FROM accounts WHERE id = {0};", id);
                        using (var reader = command.ExecuteReader())
                        {
                            if (!reader.HasRows)
                            {
                                response = 4;
                            }
                        }
                    }
                    if (response == 0)
                    {
                        using (var command = connection.CreateCommand())
                        {
                            command.CommandText = string.Format(@"SELECT status FROM friends WHERE (account_id_1 = {0} AND account_id_2 = {1}) OR (account_id_1 = {1} AND account_id_2 = {0});", Server.clients[clientID].accountID, id);
                            using (var reader = command.ExecuteReader())
                            {
                                if (reader.HasRows)
                                {
                                    while (reader.Read())
                                    {
                                        int status = reader.GetInt32("status");
                                        if (status > 0)
                                        {
                                            response = 6;
                                        }
                                        else
                                        {
                                            response = 5;
                                        }
                                    }
                                }
                            }
                        }
                        if (response == 0)
                        {
                            using (var command = connection.CreateCommand())
                            {
                                command.CommandText = string.Format(@"INSERT INTO friends WHERE (account_id_1, account_id_2) VALUES({0}, {1});", Server.clients[clientID].accountID, id);
                                command.ExecuteNonQuery();
                                response = 1;
                            }
                        }
                    } 
                    connection.Close();
                }
            }
            Packet packet = new Packet();
            packet.Write((int)InternalID.ADD_FRIEND);
            packet.Write(response);
            SendTCPData(clientID, packet);
            return response;
        }

        private async static Task<int> RemoveFriendAsync(int clientID, long id)
        {
            Task<int> task = Task.Run(() =>
            {
                return Retry.Do(() => _RemoveFriendAsyncAsync(clientID, id), TimeSpan.FromSeconds(0.1), 5, false);
            });
            return await task;
        }

        private static int _RemoveFriendAsyncAsync(int clientID, long id)
        {
            int response = 0;
            if (id != Server.clients[clientID].accountID && Server.clients[clientID].accountID >= 0)
            {
                using (var connection = Sqlite.connection)
                {
                    connection.Open();
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = string.Format(@"SELECT id FROM accounts WHERE id = {0};", id);
                        using (var reader = command.ExecuteReader())
                        {
                            if (!reader.HasRows)
                            {
                                response = 4;
                            }
                        }
                    }
                    if (response == 0)
                    {
                        long friendshipID = -1;
                        using (var command = connection.CreateCommand())
                        {
                            command.CommandText = string.Format(@"SELECT id FROM friends WHERE (account_id_1 = {0} AND account_id_2 = {1}) OR (account_id_1 = {1} AND account_id_2 = {0});", Server.clients[clientID].accountID, id);
                            using (var reader = command.ExecuteReader())
                            {
                                if (reader.HasRows)
                                {
                                    while (reader.Read())
                                    {
                                        friendshipID = reader.GetInt64("id");
                                    }
                                }
                            }
                        }
                        if (friendshipID >= 0)
                        {
                            using (var command = connection.CreateCommand())
                            {
                                command.CommandText = string.Format(@"DELETE FROM friends WHERE id = {0};", friendshipID);
                                command.ExecuteNonQuery();
                                response = 1;
                            }
                        }
                        else
                        {
                            response = 5;
                        }
                    }
                    connection.Close();
                }
            }
            Packet packet = new Packet();
            packet.Write((int)InternalID.REMOVE_FRIEND);
            packet.Write(response);
            SendTCPData(clientID, packet);
            return response;
        }

        private async static Task<int> AnswerFriendAsync(int clientID, long id, bool accept)
        {
            Task<int> task = Task.Run(() =>
            {
                return Retry.Do(() => _AnswerFriendAsync(clientID, id, accept), TimeSpan.FromSeconds(0.1), 5, false);
            });
            return await task;
        }

        private static int _AnswerFriendAsync(int clientID, long id, bool accept)
        {
            int response = 0;
            if (id != Server.clients[clientID].accountID && Server.clients[clientID].accountID >= 0)
            {
                using (var connection = Sqlite.connection)
                {
                    connection.Open();
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = string.Format(@"SELECT account_id_1 FROM friends WHERE id = {0} AND status <= 0;", id);
                        using (var reader = command.ExecuteReader())
                        {
                            if (!reader.HasRows)
                            {
                                response = 4;
                            }
                            else
                            {
                                while (reader.Read())
                                {
                                    long senderID = reader.GetInt64("account_id_1");
                                    if (senderID == Server.clients[clientID].accountID)
                                    {
                                        response = 4;
                                    }
                                }
                            }
                        }
                    }
                    if (response == 0)
                    {
                        using (var command = connection.CreateCommand())
                        {
                            if (accept)
                            {
                                command.CommandText = string.Format(@"UPDATE friends SET status = 1, action_time = CURRENT_TIMESTAMP WHERE id = {0};", id);
                            }
                            else
                            {
                                command.CommandText = string.Format(@"DELETE FROM friends WHERE id = {0};", id);
                            }
                            command.ExecuteNonQuery();
                            response = 1;
                        }
                    }
                    connection.Close();
                }
            }
            Packet packet = new Packet();
            packet.Write((int)InternalID.ANSWER_FRIEND);
            packet.Write(response);
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
            packet.Write((int)InternalID.GET_PROFILE);
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
                profile = _GetPlayer(accountID, connection);
                connection.Close();
            }
            return profile;
        }

        private static Data.PlayerProfile _GetPlayer(long accountID, Microsoft.Data.Sqlite.SqliteConnection connection)
        {
            Data.PlayerProfile profile = null;
            using (var command = connection.CreateCommand())
            {
                command.CommandText = string.Format(@"SELECT username, client_index, coins, score, level, xp, login_time FROM accounts WHERE id = {0};", accountID);
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
                            profile.coins = reader.GetInt32("coins");
                            profile.score = reader.GetInt32("score");
                            profile.level = reader.GetInt32("level");
                            profile.xp = reader.GetInt32("xp");
                            profile.login = reader.GetDateTime("login_time");
                            break;
                        }
                    }
                }
            }
            return profile;
        }

        public static List<Data.RuntimeCharacter> GetRuntimeCharacters(long accountID, bool onlySelected, bool includeEquipments, Microsoft.Data.Sqlite.SqliteConnection connection)
        {
            List<Data.RuntimeCharacter> characters = new List<Data.RuntimeCharacter>();
            using (var command = connection.CreateCommand())
            {
                command.CommandText = string.Format(@"SELECT id, prefab_id, tag, xp, level, health, speed, damage, strength, agility, constitution, dexterity, vitality, endurance, intelligence, wisdom, charisma, perception, luck, willpower, selected, default_name, custom_name FROM characters WHERE account_id = {0}{1};", accountID, onlySelected ? " AND selected > 0" : "");
                using (var reader = command.ExecuteReader())
                {
                    if (reader.HasRows)
                    {
                        while (reader.Read())
                        {
                            Data.RuntimeCharacter character = new Data.RuntimeCharacter();
                            character.id = reader.GetInt64("id");
                            character.tag = reader.GetString("tag");
                            character.prefabID = reader.GetInt32("prefab_id");
                            character.level = reader.GetInt32("level");
                            character.xp = reader.GetInt32("xp");
                            character.health = reader.GetDouble("health");
                            character.speed = reader.GetDouble("speed");
                            character.damage = reader.GetDouble("damage");
                            character.strength = reader.GetInt32("strength");
                            character.agility = reader.GetInt32("agility");
                            character.constitution = reader.GetInt32("constitution");
                            character.dexterity = reader.GetInt32("dexterity");
                            character.vitality = reader.GetInt32("vitality");
                            character.endurance = reader.GetInt32("endurance");
                            character.intelligence = reader.GetInt32("intelligence");
                            character.wisdom = reader.GetInt32("wisdom");
                            character.charisma = reader.GetInt32("charisma");
                            character.perception = reader.GetInt32("perception");
                            character.luck = reader.GetInt32("luck");
                            character.willpower = reader.GetInt32("willpower");
                            character.selected = reader.GetInt32("selected") > 0;
                            character.name = reader.GetString("default_name");
                            character.customName = reader.GetString("custom_name");
                            characters.Add(character);
                        }
                    }
                }
            }
            if (includeEquipments)
            {
                for (int i = 0; i < characters.Count; i++)
                {
                    characters[i].equipments = GetRuntimeEquipments(accountID, characters[i].id, false, connection);
                }
            }
            return characters;
        }

        public async static Task<long> CreateCharacterAsync(long accountID, Data.RuntimeCharacter character)
        {
            Task<long> task = Task.Run(() =>
            {
                return CreateCharacter(accountID, character);
            });
            return await task;
        }

        public static long CreateCharacter(long accountID, Data.RuntimeCharacter character)
        {
            long id = 0;
            using (var connection = Sqlite.connection)
            {
                connection.Open();
                id = CreateCharacter(accountID, character, connection);
                connection.Close();
            }
            return id;
        }

        public static long CreateCharacter(long accountID, Data.RuntimeCharacter character, Microsoft.Data.Sqlite.SqliteConnection connection)
        {
            long id = 0;
            if (character != null)
            {
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = string.Format(@"SELECT id FROM accounts WHERE id = {0};", accountID);
                    using (var reader = command.ExecuteReader())
                    {
                        if (!reader.HasRows)
                        {
                            accountID = 0;
                        }
                    }
                }
                if(accountID > 0)
                {
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = string.Format(@"INSERT INTO characters (account_id, prefab_id, xp, level, health, speed, damage, strength, agility, constitution, dexterity, endurance, intelligence, wisdom, charisma, perception, luck, willpower, vitality, selected, default_name, custom_name, tag) VALUES({0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}, {9}, {10}, {11}, {12}, {13}, {14}, {15}, {16}, {17}, {18}, {19}, '{20}', '{21}', '{22}'); SELECT LAST_INSERT_ROWID();", accountID, character.prefabID, character.xp, character.level, character.health, character.speed, character.damage, character.strength, character.agility, character.constitution, character.dexterity, character.endurance, character.intelligence, character.wisdom, character.charisma, character.perception, character.luck, character.willpower, character.vitality, character.selected, character.name, character.customName, character.tag);
                        id = Convert.ToInt64(command.ExecuteScalar());
                    }
                }
            }
            return id;
        }

        public async static Task<long> CreateEquipmentAsync(long accountID, long characterID, Data.RuntimeEquipment equipment)
        {
            Task<long> task = Task.Run(() =>
            {
                return CreateEquipment(accountID, characterID, equipment);
            });
            return await task;
        }

        public static long CreateEquipment(long accountID, long characterID, Data.RuntimeEquipment equipment)
        {
            long id = 0;
            using (var connection = Sqlite.connection)
            {
                connection.Open();
                id = CreateEquipment(accountID, characterID, equipment, connection);
                connection.Close();
            }
            return id;
        }

        public static long CreateEquipment(long accountID, long characterID, Data.RuntimeEquipment equipment, Microsoft.Data.Sqlite.SqliteConnection connection)
        {
            long id = 0;
            if (equipment != null)
            {
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = string.Format(@"SELECT id FROM accounts WHERE id = {0};", accountID);
                    using (var reader = command.ExecuteReader())
                    {
                        if (!reader.HasRows)
                        {
                            accountID = 0;
                        }
                    }
                }
                if (characterID > 0)
                {
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = string.Format(@"SELECT id FROM characters WHERE id = {0};", characterID);
                        using (var reader = command.ExecuteReader())
                        {
                            if (!reader.HasRows)
                            {
                                characterID = 0;
                            }
                        }
                    }
                }
                else
                {
                    characterID = 0;
                }
                if (accountID > 0)
                {
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = string.Format(@"INSERT INTO equipments (account_id, character_id, prefab_id, range, level, armor, speed, damage, weight, accuracy, capacity, default_name, custom_name, tag, type) VALUES({0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}, {9}, {10}, '{11}', '{12}', '{13}', {14}); SELECT LAST_INSERT_ROWID();", accountID, characterID, equipment.prefabID, equipment.range, equipment.level, equipment.armor, equipment.speed, equipment.damage, equipment.weight, equipment.accuracy, equipment.capacity, equipment.name, equipment.customName, equipment.tag, equipment.type);
                        id = Convert.ToInt64(command.ExecuteScalar());
                    }
                }
            }
            return id;
        }

        public async static Task<bool> SpendCoinsAsync(long accountID, uint amount)
        {
            Task<bool> task = Task.Run(() =>
            {
                return SpendCoins(accountID, amount);
            });
            return await task;
        }

        public static bool SpendCoins(long accountID, uint amount)
        {
            bool spent = false;
            using (var connection = Sqlite.connection)
            {
                connection.Open();
                spent = SpendCoins(accountID, amount, connection);
                connection.Close();
            }
            return spent;
        }

        public static bool SpendCoins(long accountID, uint amount, Microsoft.Data.Sqlite.SqliteConnection connection)
        {
            bool spent = false;
            if (amount > 0)
            {
                int coins = GetCoins(accountID, connection);
                if (coins >= amount)
                {
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = string.Format(@"UPDATE accounts SET coins = coins - {0} WHERE id = {1};", amount, accountID);
                        command.ExecuteNonQuery();
                    }
                }
            }
            return spent;
        }

        public async static Task<int> GetCoinsAsync(long accountID)
        {
            Task<int> task = Task.Run(() =>
            {
                return GetCoins(accountID);
            });
            return await task;
        }

        public static int GetCoins(long accountID)
        {
            int coins = 0;
            using (var connection = Sqlite.connection)
            {
                connection.Open();
                coins = GetCoins(accountID, connection);
                connection.Close();
            }
            return coins;
        }

        public static int GetCoins(long accountID, Microsoft.Data.Sqlite.SqliteConnection connection)
        {
            int coins = 0;
            using (var command = connection.CreateCommand())
            {
                command.CommandText = string.Format(@"SELECT coins FROM accounts WHERE id = {0};", accountID);
                using (var reader = command.ExecuteReader())
                {
                    if (reader.HasRows)
                    {
                        while (reader.Read())
                        {
                            coins = reader.GetInt32("coins");
                            break;
                        }
                    }
                }
            }
            return coins;
        }

        public static List<Data.RuntimeEquipment> GetRuntimeEquipments(long accountID, long characterID, bool excludeEquipped, Microsoft.Data.Sqlite.SqliteConnection connection)
        {
            List<Data.RuntimeEquipment> equipments = new List<Data.RuntimeEquipment>();
            using (var command = connection.CreateCommand())
            {
                command.CommandText = string.Format(@"SELECT id, character_id, prefab_id, type, range, level, armor, speed, damage, weight, accuracy, capacity, default_name, tag, custom_name FROM equipments WHERE account_id = {0}{1}{2};", accountID, characterID > 0 ? " AND character_id = " + characterID.ToString() : "", excludeEquipped ? " AND character_id <= 0" : "");
                using (var reader = command.ExecuteReader())
                {
                    if (reader.HasRows)
                    {
                        while (reader.Read())
                        {
                            Data.RuntimeEquipment equipment = new Data.RuntimeEquipment();
                            equipment.id = reader.GetInt64("id");
                            equipment.characterID = reader.GetInt64("character_id");
                            equipment.prefabID = reader.GetInt32("prefab_id");
                            equipment.type = reader.GetInt32("type");
                            equipment.level = reader.GetInt32("level");
                            equipment.range = reader.GetDouble("range");
                            equipment.armor = reader.GetDouble("armor");
                            equipment.speed = reader.GetDouble("speed");
                            equipment.damage = reader.GetDouble("damage");
                            equipment.weight = reader.GetDouble("weight");
                            equipment.accuracy = reader.GetDouble("accuracy");
                            equipment.capacity = reader.GetInt32("capacity");
                            equipment.name = reader.GetString("default_name");
                            equipment.customName = reader.GetString("custom_name");
                            equipment.tag = reader.GetString("tag");
                            equipments.Add(equipment);
                        }
                    }
                }
            }
            return equipments;
        }

        private async static Task<int> PurchaseAsync(int clientID, long accountID, int itemCategory, int itemID, int itemLevel, int currencyID)
        {
            Task<int> task = Task.Run(() =>
            {
                return Retry.Do(() => _PurchaseAsync(clientID, accountID, itemCategory, itemID, itemLevel, currencyID), TimeSpan.FromSeconds(0.1), 5, false);
            });
            return await task;
        }

        private static int _PurchaseAsync(int clientID, long accountID, int itemCategory, int itemID, int itemLevel, int currencyID)
        {
            int result = 0;
            int price = 0;
            using (var connection = Sqlite.connection)
            {
                connection.Open();
                var purchase = Terminal.OverridePurchase(accountID, itemCategory, itemID, itemLevel, currencyID, connection);
                connection.Close();
                result = (int)purchase.Item1;
                price = purchase.Item2;
            }
            Packet packet = new Packet();
            packet.Write((int)InternalID.PURCHASE);
            packet.Write(result);
            packet.Write(itemCategory);
            packet.Write(itemID);
            packet.Write(itemLevel);
            packet.Write(currencyID);
            packet.Write(price);
            SendTCPData(clientID, packet);
            return 1;
        }

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
                    if (rooms[i] == null) continue;
                    _rooms.Add(rooms[i]);
                }
                return _rooms;
            });
            return await task;
        }

        private async static Task<int> CreateRoomAsync(int clientID, long account_id, string password, int gameID, int mapID, int team, int maxPlayers)
        {
            Task<int> task = Task.Run(() =>
            {
                return Retry.Do(() => _CreateRoomAsync(clientID, account_id, password, gameID, mapID, team, maxPlayers), TimeSpan.FromSeconds(0.1), 1, false);
            });
            return await task;
        }

        private static int _CreateRoomAsync(int clientID, long accountID, string password, int gameID, int mapID, int team, int maxPlayers)
        {
            int response = 0;
            Data.Room room = null;
            if (GetPlayerRoom(accountID) != null)
            {
                response = 4;
            }
            else
            {
                if (Server.clients[clientID].party == null)
                {
                    if(Server.clients[clientID].game != null || GetPlayerGame(Server.clients[clientID].accountID) >= 0)
                    {
                        response = 6;
                    }
                    else
                    {
                        if (Server.clients[clientID].player == null)
                        {
                            UpdatePlayer(clientID);
                        }
                        if (Server.clients[clientID].player != null)
                        {
                            room = new Data.Room();
                            room.id = Guid.NewGuid().ToString();
                            room.gameID = gameID;
                            room.mapID = mapID;
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
                }
                else
                {
                    response = 5;
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
                    if (Server.clients[id].game != null && Server.clients[id].player != null)
                    {
                        int sceneHost = -1;
                        bool checkHost = false;
                        bool setHost = false;
                        for (int i = 0; i < Server.clients[id].game.sceneHostsKeys.Count; i++)
                        {
                            if (Server.clients[id].game.sceneHostsKeys[i] == scene)
                            {
                                sceneHost = i;
                                break;
                            }
                        }
                        if(sceneHost >= 0)
                        {
                            if(Server.clients[id].game.sceneHostsValues[sceneHost] != Server.clients[id].accountID)
                            {
                                checkHost = true;
                            }
                        }
                        else
                        {
                            setHost = true;
                            sceneHost = Server.clients[id].game.sceneHostsKeys.Count;
                            Server.clients[id].game.sceneHostsKeys.Add(scene);
                            Server.clients[id].game.sceneHostsValues.Add(Server.clients[id].accountID);
                        }
                        Server.clients[id].player.scene = scene;
                        if (checkHost)
                        {
                            for (int i = 0; i < Server.clients[id].game.room.players.Count; i++)
                            {
                                if (Server.clients[id].game.room.players[i].id == Server.clients[id].accountID) { continue; }
                                if (Server.clients[id].game.room.players[i].id == Server.clients[id].game.sceneHostsValues[sceneHost])
                                {
                                    if (Server.clients[id].game.room.players[i].scene != scene || (DateTime.Now - Server.clients[Server.clients[id].game.room.players[i].client].lastTick).TotalSeconds > 1d)
                                    {
                                        setHost = true;
                                        Server.clients[id].game.sceneHostsValues[sceneHost] = Server.clients[id].accountID;
                                    }
                                    checkHost = false;
                                    break;
                                }
                            }
                        }
                        if (checkHost)
                        {
                            setHost = true;
                            Server.clients[id].game.sceneHostsValues[sceneHost] = Server.clients[id].accountID;
                        }
                        if (setHost)
                        {
                            Packet packet = new Packet();
                            packet.Write((int)InternalID.SET_HOST);
                            packet.Write(scene);
                            packet.Write(Server.clients[id].game.sceneHostsValues[sceneHost]);
                            SendTCPData(id, packet);
                        }
                        Server.clients[id].lastTick = DateTime.Now;
                        for (int i = 0; i < Server.clients[id].game.room.players.Count; i++)
                        {
                            if (Server.clients[id].game.room.players[i].id == Server.clients[id].accountID) { continue; }
                            Packet packet = new Packet();
                            packet.Write((int)InternalID.SYNC_GAME);
                            packet.Write(scene);
                            packet.Write(Server.clients[id].accountID);
                            packet.Write(Server.clients[id].game.sceneHostsValues[sceneHost]);
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
                            SendUDPData(Server.clients[id].game.room.players[i].client, packet);
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
                    if (Server.clients[id].game != null && Server.clients[id].player != null)
                    {
                        for (int i = 0; i < Server.clients[id].game.room.players.Count; i++)
                        {
                            if (Server.clients[id].game.room.players[i].id == Server.clients[id].accountID) { continue; }
                            Packet packet = new Packet();
                            packet.Write((int)InternalID.DESTROY_OBJECT);
                            packet.Write(scene);
                            packet.Write(Server.clients[id].accountID);
                            packet.Write(objectID);
                            packet.Write(position);
                            SendTCPData(Server.clients[id].game.room.players[i].client, packet);
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
                    if (Server.clients[id].game != null && Server.clients[id].player != null)
                    {
                        int sceneHost = -1;
                        for (int i = 0; i < Server.clients[id].game.sceneHostsKeys.Count; i++)
                        {
                            if (Server.clients[id].game.sceneHostsKeys[i] == scene)
                            {
                                sceneHost = i;
                                break;
                            }
                        }
                        if (sceneHost >= 0)
                        {
                            for (int i = 0; i < Server.clients[id].game.room.players.Count; i++)
                            {
                                if (Server.clients[id].game.room.players[i].id != Server.clients[id].game.sceneHostsValues[sceneHost]) { continue; }
                                Packet packet = new Packet();
                                packet.Write((int)InternalID.CHANGE_OWNER);
                                packet.Write(scene);
                                packet.Write(Server.clients[id].accountID);
                                packet.Write(objects.Length);
                                packet.Write(objects);
                                packet.Write(newOwner);
                                SendTCPData(Server.clients[id].game.room.players[i].client, packet);
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
                    if (Server.clients[id].game != null && Server.clients[id].player != null)
                    {
                        for (int i = 0; i < Server.clients[id].game.room.players.Count; i++)
                        {
                            Packet packet = new Packet();
                            packet.Write((int)InternalID.CHANGE_OWNER_CONFIRM);
                            packet.Write(scene);
                            packet.Write(objectID);
                            packet.Write(position);
                            packet.Write(newOwner);
                            SendTCPData(Server.clients[id].game.room.players[i].client, packet);
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
        
        private static int GetPlayerGame(long accountID)
        {
            for (int i = 0; i < games.Count; i++)
            {
                if(games[i].room != null)
                {
                    for (int j = 0; j < games[i].room.players.Count; j++)
                    {
                        if (games[i].room.players[j].id == accountID)
                        {
                            return i;
                        }
                    }
                }
            }
            return -1;
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
            PLAYER_JOINED = 1, PLAYER_LEFT = 2, PLAYER_KICKED = 3
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
                if (Server.clients[clientID].party == null)
                {
                    if (Server.clients[clientID].game != null || GetPlayerGame(Server.clients[clientID].accountID) >= 0)
                    {
                        response = 9;
                    }
                    else
                    {
                        for (int i = 0; i < rooms.Count; i++)
                        {
                            if (rooms[i].id == roomID)
                            {
                                if (rooms[i].maxPlayers <= 0 || rooms[i].maxPlayers > rooms[i].players.Count)
                                {
                                    if (string.IsNullOrEmpty(rooms[i].password) || rooms[i].password == password)
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
                        if (room != null)
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
                }
                else
                {
                    response = 8;
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
                if(room.hostID == Server.clients[clientID].accountID || room.players.Count <= 1)
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
                if (room.hostID == Server.clients[clientID].accountID || room.players.Count <= 1)
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

        private async static Task<int> LeaveGameAsync(int clientID, bool respond = false, bool notifySelf = true, bool notifyOthers = true)
        {
            Task<int> task = Task.Run(() =>
            {
                return Retry.Do(() => _LeaveGameAsync(clientID, respond, notifySelf, notifyOthers), TimeSpan.FromSeconds(0.1), 5, false);
            });
            return await task;
        }

        private static int _LeaveGameAsync(int clientID, bool respond = false, bool notifySelf = true, bool notifyOthers = true)
        {
            int response = 0;
            if(Server.clients[clientID].game != null)
            {
                response = 1;
                for (int i = 0; i < games.Count; i++)
                {
                    if (games[i].room != null)
                    {
                        for (int j = 0; j < games[i].room.players.Count; j++)
                        {
                            if (games[i].room.players[j].id == Server.clients[clientID].accountID)
                            {
                                games[i].room.players.RemoveAt(j);
                                if (games[i].room.players.Count <= 0)
                                {
                                    Data.Game game = games[i];
                                    games.RemoveAt(i);
                                    Terminal.OnGameFinished(game);
                                }
                                else
                                {
                                    if (notifyOthers)
                                    {
                                        // ToDo: Notify other players in the game
                                    }
                                }
                                break;
                            }
                        }
                    }
                }
                Server.clients[clientID].game = null;
                if(response == 1 && notifySelf)
                {
                    // ToDo: Notify yourself with the same packet id as other players in the game
                }
            }
            else
            {
                response = 4;
            }
            if (respond)
            {
                Packet packet = new Packet();
                packet.Write((int)InternalID.LEAVE_GAME);
                packet.Write(response);
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
                if (room.hostID == Server.clients[clientID].accountID && Server.clients[clientID].accountID != targetID)
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

        private async static Task<int> StartRoomAsync(int clientID, Data.Extension extension)
        {
            Task<int> task = Task.Run(() =>
            {
                return Retry.Do(() => _StartRoomAsync(clientID, extension), TimeSpan.FromSeconds(0.1), 5, false);
            });
            return await task;
        }

        private static int _StartRoomAsync(int clientID, Data.Extension extension, bool notifyCallerInUpdate = true)
        {
            int response = 0;
            Data.Room room = GetPlayerRoom(Server.clients[clientID].accountID);
            Data.Game game = null;
            if (room == null)
            {
                // Not in Any Room
                response = 4;
            }
            else
            {
                if (room.hostID == Server.clients[clientID].accountID)
                {
                    game = new Data.Game();
                    game.room = room;
                    game.type = Data.GameType.HOSTED;
                    game.start = DateTime.Now;
                    game.extension = extension;
                    for (int i = game.room.players.Count - 1; i >= 0; i--)
                    {
                        if(Server.clients[game.room.players[i].client].game != null)
                        {
                            Server.clients[game.room.players[i].client].room = null;
                            game.room.players.RemoveAt(i);
                        }
                        else
                        {
                            Server.clients[game.room.players[i].client].game = game;
                        }
                    }
                    if (game.extension == Data.Extension.NONE)
                    {
                        games.Add(game);
                    }
                    rooms.Remove(room);
                    response = 1;
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
                if (game.extension == Data.Extension.NETCODE_SERVER)
                {
                    Netcode.StartGame(game);
                }
                else
                {
                    // byte[] playerBytes = Tools.Compress(Tools.Serialize<Data.Player>(Server.clients[clientID].player));
                    byte[] bytes = Tools.Compress(Tools.Serialize<Data.RuntimeGame>(GetStartGameData(game)));
                    for (int i = 0; i < room.players.Count; i++)
                    {
                        if (room.players[i].id == Server.clients[clientID].accountID && !notifyCallerInUpdate) { continue; }
                        Packet othersPacket = new Packet();
                        othersPacket.Write((int)InternalID.GAME_STARTED);
                        othersPacket.Write(bytes.Length);
                        othersPacket.Write(bytes);
                        // othersPacket.Write(playerBytes.Length);
                        // othersPacket.Write(playerBytes);
                        SendTCPData(room.players[i].client, othersPacket);
                    }
                }
            }
            SendTCPData(clientID, packet);
            return response;
        }
        
        private static Data.RuntimeGame GetStartGameData(Data.Game game)
        {
            Data.RuntimeGame runtimeGame = new Data.RuntimeGame();
            if(game == null)
            {
                return null;
            }
            runtimeGame.id = game.room.id;
            runtimeGame.gameID = game.room.gameID;
            runtimeGame.mapID = game.room.mapID;
            runtimeGame.players = new List<Data.RuntimePlayer>();
            using (var connection = Sqlite.connection)
            {
                connection.Open();
                for (int i = 0; i < game.room.players.Count; i++)
                {
                    Data.RuntimePlayer player = new Data.RuntimePlayer();
                    player.id = game.room.players[i].id;
                    player.team = game.room.players[i].team;
                    player.username = game.room.players[i].username;
                    player.characters = GetRuntimeCharacters(player.id, true, true, connection);
                    runtimeGame.players.Add(player);
                }
                connection.Close();
            }
            return runtimeGame;
        }
        #endregion

    }
}