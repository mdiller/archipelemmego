using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Models;
using Archipelago.MultiClient.Net.Packets;
using ArchipeLemmeGo.Datamodel.Infos;
using global::Archipelago.MultiClient.Net;
using global::Archipelago.MultiClient.Net.Packets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ArchipeLemmeGo.Archipelago
{
    namespace ArchipeLemmeGo.Archipelago
    {
        /// <summary>
        /// Provides low level connection and information to a room without connecting to a specific slot
        /// </summary>
        public class ArchipelagoLowLevelClient
        {
            private ArchipelagoSession _session;
            private TaskCompletionSource<RoomInfoPacket> _roomInfoTcs;

            public RoomInfo RoomInfo { get; private set; }

            public ArchipelagoLowLevelClient(RoomInfo roomInfo)
            {
                RoomInfo = roomInfo;
            }

            public async Task ConnectAsync()
            {
                _roomInfoTcs = new TaskCompletionSource<RoomInfoPacket>();

                _session = ArchipelagoSessionFactory.CreateSession(RoomInfo.Host, RoomInfo.Port);
                _session.Socket.PacketReceived += OnPacketReceived;

                await _session.Socket.ConnectAsync();

                var timeoutTask = Task.Delay(5000);
                var completed = await Task.WhenAny(_roomInfoTcs.Task, timeoutTask);
                if (completed == timeoutTask)
                    throw new TimeoutException("Timed out waiting for RoomInfoPacket");
            }

            private void OnPacketReceived(ArchipelagoPacketBase packet)
            {
                if (packet is RoomInfoPacket roomInfo)
                {
                    _session.Socket.PacketReceived -= OnPacketReceived;
                    _roomInfoTcs.TrySetResult(roomInfo);
                }
            }

            internal async Task SetupRoomInfo()
            {
                var roomInfoPacket = await _roomInfoTcs.Task;
                RoomInfo.Seed = roomInfoPacket.SeedName;
                RoomInfo.HintCostPercentage = roomInfoPacket.HintCostPercentage;
                RoomInfo.Games = roomInfoPacket.Games.Where(g => g != "Archipelago").ToArray();
            }

            public async Task DisconnectAsync()
            {
                if (_session?.Socket.Connected == true)
                {
                    await _session.Socket.DisconnectAsync();
                }
            }
        }
    }

}
