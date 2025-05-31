using ArchipeLemmeGo.Archipelago.ArchipeLemmeGo.Archipelago;
using ArchipeLemmeGo.Datamodel.Infos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchipeLemmeGo.Archipelago
{
    public class ArchipelagoService
    {
        /// <summary>
        /// Attempts to connect to a room and collect the roominfo associated with it.
        /// </summary>
        /// <param name="host">The host url</param>
        /// <param name="port">The port to connect to</param>
        /// <param name="password">The password to connect with</param>
        /// <returns></returns>
        public static async Task<RoomInfo> RegisterRoomInfo(ulong adminId, string host, int port, string password = null, bool allowExisting = true)
        {
            var roomInfo = new RoomInfo
            {
                Host = host,
                Port = port,
                Password = password,
                AdminId = adminId
            };
            var client = new ArchipelagoLowLevelClient(roomInfo);
            try
            {
                await client.ConnectAsync();
                await client.SetupRoomInfo();
            }
            finally
            {
                await client.DisconnectAsync();
            }
            if (roomInfo.Uri.Exists())
            {
                if (allowExisting)
                {
                    var existingRoomInfo = roomInfo.Uri.Load<RoomInfo>();
                    existingRoomInfo.Host = host;
                    existingRoomInfo.Port = port;
                    existingRoomInfo.Save();
                    return existingRoomInfo;
                }
                else
                {
                    throw new Exception("Room already exists!!!");
                }
            }
            roomInfo.Save();
            return roomInfo;
        }

        /// <summary>
        /// Attempts to connect to the room via the slot name, get information about the slot and the game being played, and save that information to the SlotInfo
        /// </summary>
        /// <param name="roomInfo"></param>
        /// <param name="slotName"></param>
        /// <returns></returns>
        public static async Task<SlotInfo> RegisterSlotInfo(ulong discordId, RoomInfo roomInfo, string slotName)
        {
            var existingSlot = roomInfo.SlotInfos.FirstOrDefault(s => s.Name == slotName);
            if (existingSlot != null)
            { 
                existingSlot.DiscordId = discordId;
                roomInfo.Save();
                return existingSlot;
            }
            var slotInfo = new SlotInfo
            {
                Name = slotName,
                DiscordId = discordId,
            };

            var client = new ArchipelagoClient(roomInfo, slotInfo);

            try
            {
                await client.ConnectAsync();
                var dataPackage = await client.GetDataPackageAsync();
                var gamePackage = dataPackage.DataPackage.Games[slotInfo.Game];

                slotInfo.SlotId = client.SlotId;
                slotInfo.ItemLookup = gamePackage.ItemLookup;
                slotInfo.LocationLookup = gamePackage.LocationLookup;

                roomInfo.SlotInfos.Add(slotInfo);
                roomInfo.Save();
            }
            finally
            {
                await client.Disconnect();
            }

            return slotInfo;
        }
    }
}
