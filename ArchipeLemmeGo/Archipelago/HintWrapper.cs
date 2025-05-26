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

        public string FinderName { get; set; }
        public string FinderMention { get; set; }

        public string RecieverName { get; set; }
        public string RecieverMention { get; set; }

        public bool IsFound { get; set; }

        /// <summary>
        /// weheee
        /// </summary>
        /// <param name="hint"></param>
        public HintWrapper(RequestedHintInfo hint, RoomInfo roomInfo)
        {
            HintInfo = hint;

            var finderInfo = roomInfo.SlotInfos.FirstOrDefault(s => s.SlotId == hint.FinderSlot);
            var recieverInfo = roomInfo.SlotInfos.FirstOrDefault(s => s.SlotId == hint.RequesterSlot);

            RecieverName = $"Player in Slot {hint.RequesterSlot}";
            RecieverMention = RecieverName;
            FinderName = $"Player in Slot {hint.FinderSlot}";
            FinderMention = FinderName;
            Item = $"Item @id={hint.ItemId}";
            Location = $"Location @id={hint.LocationId}";

            if (finderInfo != null)
            {
                FinderName = finderInfo.ToSignature(false);
                FinderMention = finderInfo.ToSignature();
                Location = finderInfo.LocationLookup
                    .Where(kvp => kvp.Value == hint.LocationId)
                    .Select(kvp => kvp.Key)
                    .FirstOrDefault() ?? Location;
            }

            if (recieverInfo != null)
            {
                RecieverName = recieverInfo.ToSignature(false);
                RecieverMention = recieverInfo.ToSignature();
                Item = recieverInfo.ItemLookup
                    .Where(kvp => kvp.Value == hint.ItemId)
                    .Select(kvp => kvp.Key)
                    .FirstOrDefault() ?? Location;
            }
        }
    }
}
