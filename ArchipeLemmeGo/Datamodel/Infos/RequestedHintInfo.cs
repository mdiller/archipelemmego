using Archipelago.MultiClient.Net.Models;
using ArchipeLemmeGo.Archipelago;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchipeLemmeGo.Datamodel.Infos
{
    /// <summary>
    /// Info about a requested hint
    /// </summary>
    public class RequestedHintInfo
    {
        /// <summary>
        /// The slot ID of the person who requested this
        /// </summary>
        public int RequesterSlot { get; set; }

        /// <summary>
        /// The slot ID of the person who can find this
        /// </summary>
        public int FinderSlot { get; set; }
        
        /// <summary>
        /// The ID of the location that needs to be unlocked
        /// </summary>
        public long LocationId { get; set; }

        /// <summary>
        /// The ItemId of the item that we're looking for
        /// </summary>
        public long ItemId { get; set; }


        /// <summary>
        /// Information/notes associated with this request
        /// </summary>
        public string Information { get; set; }

        /// <summary>
        /// The priority of this request
        /// </summary>
        public int Priority { get; set; }

        /// <summary>
        /// The number of this item that are needed
        /// </summary>
        public int Count { get; set; }

        /// <summary>
        /// Whether or not this hint has been found yet
        /// </summary>
        public bool IsFound { get; set; } = false;

        /// <summary>
        /// Whether or not the two hintinfos are referring to the same item
        /// </summary>
        public bool SameItem(RequestedHintInfo info)
        {
            return ItemId == info.ItemId && RequesterSlot == info.RequesterSlot;
        }

        /// <summary>
        /// Updates the hintinfos with the found information from the hints
        /// </summary>
        public static void UpdateHintInfos(List<RequestedHintInfo> hintInfos, List<Hint> hints)
        {
            hints.ForEach(hint =>
            {
                var hintInfo = hintInfos.FirstOrDefault(h =>
                            h.ItemId == hint.ItemId
                            && h.LocationId == hint.LocationId
                            && h.FinderSlot == hint.FindingPlayer
                            && h.RequesterSlot == hint.ReceivingPlayer);
                if (hintInfo != null)
                {
                    hintInfo.IsFound = hint.Found;
                }
            });
            var finishedList = new List<string>();
            var foundCountMap = new Dictionary<string, int>();
            hintInfos.ForEach(hint =>
            {
                var key = $"{hint.RequesterSlot}_{hint.ItemId}";
                if (hint.IsFound && !finishedList.Contains(key))
                {
                    if (!foundCountMap.ContainsKey(key))
                    {
                        foundCountMap[key] = 0;
                    }
                    foundCountMap[key]++;
                    if (hint.Count <= foundCountMap[key])
                    {
                        finishedList.Add(key);
                    }
                }
            });
            // TODO: track new removals and report to user that they are done!
            hintInfos.RemoveAll(hint =>
            {
                var key = $"{hint.RequesterSlot}_{hint.ItemId}";
                return !hint.IsFound && finishedList.Contains(key);
            });
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="roomInfo"></param>
        /// <returns></returns>
        public HintWrapper ToHintWrapper(RoomInfo roomInfo)
        {
            return new HintWrapper(this, roomInfo);
        }

        /// <summary>
        /// Creates a RequestedHintInfo from the hint and the input args
        /// </summary>
        public static RequestedHintInfo Create(Hint hint, string information, int priority, int count)
        {
            return new RequestedHintInfo
            {
                RequesterSlot = hint.ReceivingPlayer,
                FinderSlot = hint.FindingPlayer,
                LocationId = hint.LocationId,
                ItemId = hint.ItemId,
                Information = information,
                Priority = priority,
                Count = count
            };
        }
    }
}
