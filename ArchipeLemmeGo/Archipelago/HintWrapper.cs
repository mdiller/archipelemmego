using Archipelago.MultiClient.Net.Models;
using ArchipeLemmeGo.Datamodel.Infos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;

namespace ArchipeLemmeGo.Archipelago
{
    public class HintWrapper
    {
        /// <summary>
        /// The thint
        /// </summary>
        public RequestedHintInfo HintInfo { get; set; }

        public string Location { get; set; }

        public string Item { get; set; }

        public string Finder { get; set; }

        public string Reciever { get; set; }

        /// <summary>
        /// weheee
        /// </summary>
        /// <param name="hint"></param>
        public HintWrapper(RequestedHintInfo hint, RoomInfo roomInfo)
        {
            HintInfo = hint;

            var finderInfo = roomInfo.SlotInfos.FirstOrDefault(s => s.SlotId == hint.FinderSlot);
            var recieverInfo = roomInfo.SlotInfos.FirstOrDefault(s => s.SlotId == hint.RequesterSlot);

            Reciever = $"Player in Slot {hint.RequesterSlot}";
            Finder = $"Player in Slot {hint.FinderSlot}";
            Item = $"Item @id={hint.ItemId}";
            Location = $"Location @id={hint.LocationId}";

            if (finderInfo != null)
            {
                Finder = finderInfo.ToSignature();
                Location = finderInfo.LocationLookup
                    .Where(kvp => kvp.Value == hint.LocationId)
                    .Select(kvp => kvp.Key)
                    .FirstOrDefault() ?? Location;
            }

            if (recieverInfo != null)
            {
                Reciever = recieverInfo.ToSignature();
                Item = recieverInfo.ItemLookup
                    .Where(kvp => kvp.Value == hint.ItemId)
                    .Select(kvp => kvp.Key)
                    .FirstOrDefault() ?? Location;
            }
        }
    }
}
