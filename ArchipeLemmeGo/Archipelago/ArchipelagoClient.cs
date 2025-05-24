using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.Models;
using Archipelago.MultiClient.Net.Packets;
using ArchipeLemmeGo.Datamodel.Infos;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ArchipeLemmeGo.Archipelago
{
    public class ArchipelagoDisconnectedException : UserError
    {
        public ArchipelagoDisconnectedException(RoomInfo roomInfo, SlotInfo slotInfo)
            : base($"Failed to connect to Archipelago server.\nSlot: {slotInfo.SlotId}\nRoom: {roomInfo.Uri}") { }
    }

    public class ArchipelagoClient
    {
        private ArchipelagoSession _session;

        public RoomInfo RoomInfo { get; }
        public SlotInfo SlotInfo { get; }

        public bool IsConnected => _session?.Socket.Connected ?? false;

        public ArchipelagoClient(RoomInfo roomInfo, SlotInfo slotInfo)
        {
            RoomInfo = roomInfo;
            SlotInfo = slotInfo;
        }

        public async Task ConnectAsync()
        {
            var result = await Task.Run(() =>
            {
                _session = ArchipelagoSessionFactory.CreateSession(RoomInfo.Host, RoomInfo.Port);
                // TO ADD THIS BACK IN FUTURE, MAKE SURE TO DO PROPERLY, CUZ WE CREATE NEW SESSION LATYER SOMETIMES
                // _session.MessageLog.OnMessageReceived += m => Console.WriteLine($"[AP:{SlotInfo.Name}] {string.Join("__", m.Parts.Select(p => p.Text))}");

                if (SlotInfo.Game == null)
                { // If we dont have a game assigned, try em till we find one that works
                    foreach (var game in RoomInfo.Games)
                    {
                        var result = _session.TryConnectAndLogin(game, SlotInfo.Name, ItemsHandlingFlags.AllItems, password: RoomInfo.Password);
                        if (result is LoginSuccessful)
                        {
                            SlotInfo.Game = game;
                            return true;
                        }
                        else
                        {
                            _session = ArchipelagoSessionFactory.CreateSession(RoomInfo.Host, RoomInfo.Port);
                        }
                    }
                    return false;
                }
                else
                {
                    var result = _session.TryConnectAndLogin(SlotInfo.Game, SlotInfo.Name, ItemsHandlingFlags.AllItems, password: RoomInfo.Password);

                    return result is LoginSuccessful;
                }
            });
            if (result == false)
            {
                throw new ArchipelagoDisconnectedException(RoomInfo, SlotInfo);
            }
        }

        public int SlotId => _session.ConnectionInfo.Slot;

        public async Task Disconnect()
        {
            if (_session?.Socket.Connected == true)
            {
                await _session.Socket.DisconnectAsync();
            }
        }

        private void EnsureConnectedOrThrow()
        {
            if (!IsConnected)
            {
                throw new ArchipelagoDisconnectedException(RoomInfo, SlotInfo);
            }
        }

        public async Task<Hint[]> GetHints()
        {
            EnsureConnectedOrThrow();

            return await _session.DataStorage.GetHintsAsync(SlotInfo.SlotId);
        }

        public async Task Say(string message)
        {
            var sayPacket = new SayPacket
            {
                Text = message
            };
            _session.Socket.SendPacket(sayPacket);
        }

        /// <summary>
        /// Ensures connection is valid; reconnects if needed. Throws if reconnect fails.
        /// </summary>
        public async Task EnsureConnectedAsync()
        {
            if (!IsConnected)
            {
                await ConnectAsync();
            }
        }

        /// <summary>
        /// Gets all of the location names for the connected slot
        /// </summary>
        /// <returns>An array of location names</returns>
        public string[] GetLocationNames()
        {
            EnsureConnectedOrThrow();

            return _session.Locations.AllLocations
                .Select(id => _session.Locations.GetLocationNameFromId(id))
                .ToArray();
        }

        public async Task<DataPackagePacket> GetDataPackageAsync()
        {
            var tcs = new TaskCompletionSource<DataPackagePacket>();

            // Temporary handler
            void OnPacketReceived(ArchipelagoPacketBase packet)
            {
                if (packet is DataPackagePacket dataPackage)
                {
                    _session.Socket.PacketReceived -= OnPacketReceived;
                    tcs.TrySetResult(dataPackage);
                }
            }

            _session.Socket.PacketReceived += OnPacketReceived;

            // Send the GetDataPackage request
            _session.Socket.SendPacket(new GetDataPackagePacket
            {
                Games = new[] { SlotInfo.Game }
            });

            // Wait for the packet (with optional timeout)
            var timeoutTask = Task.Delay(5000); // Optional timeout
            var completedTask = await Task.WhenAny(tcs.Task, timeoutTask);

            if (completedTask == timeoutTask)
            {
                _session.Socket.PacketReceived -= OnPacketReceived;
                throw new TimeoutException("Timed out waiting for DataPackagePacket.");
            }

            return await tcs.Task;
        }

        /// <summary>
        /// Gets all of the item names for the connected slot
        /// </summary>
        /// <returns>An array of item names</returns>
        //public async Task<Dictionary<string, long>> GetItemLookup()
        //{
        //    EnsureConnectedOrThrow();
        //    var dataPackage = await GetDataPackageAsync();
        //    return dataPackage.DataPackage.Games[SlotInfo.Game].LocationLookup;
        //}
    }

}
