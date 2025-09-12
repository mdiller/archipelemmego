using Archipelago.MultiClient.Net.Models;
using ArchipeLemmeGo.Archipelago;
using ArchipeLemmeGo.Datamodel.Arch;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace ArchipeLemmeGo.Datamodel.Infos
{
    /// <summary>
    /// Info about a requested hint
    /// </summary>
    public class RequestedHintInfo
    {
        /// <summary>
        /// The Item that is being requested
        /// </summary>
        [JsonIgnore]
        public ArchItem Item { get; set; }

        /// <summary>
        /// The location needed to unlock the item
        /// </summary>
        [JsonIgnore]
        public ArchLocation Location{ get; set; }

        /// <summary>
        /// The slot ID of the person who requested this
        /// </summary>
        public int RequesterSlot
        {
            get => Item?.Slot ?? -1;
            set {
                if (Item == null)
                {
                    Item = new ArchItem();
                }
                Item.Slot = value;
            }
        }

        /// <summary>
        /// The slot ID of the person who can find this
        /// </summary>
        public int FinderSlot
        {
            get => Location?.Slot ?? -1;
            set
            {
                if (Location == null)
                {
                    Location = new ArchLocation();
                }
                Location.Slot = value;
            }
        }

        /// <summary>
        /// The ID of the location that needs to be unlocked
        /// </summary>
        public long LocationId
        {
            get => Location?.LocationId ?? -1;
            set
            {
                if (Location == null)
                {
                    Location = new ArchLocation();
                }
                Location.LocationId = value;
            }
        }

        /// <summary>
        /// The ItemId of the item that we're looking for
        /// </summary>
        public long ItemId
        {
            get => Item?.ItemId ?? -1;
            set
            {
                if (Item == null)
                {
                    Item = new ArchItem();
                }
                Item.ItemId = value;
            }
        }


        /// <summary>
        /// Information/notes associated with this request
        /// </summary>
        public string Information { get; set; }

        /// <summary>
        /// The priority of this request
        /// </summary>
        public int Priority { get; set; }

        /// <summary>
        /// The progression level requested for this item
        /// </summary>
        public int Count
        {
            get => Item?.ProgressionLevel ?? 0;
            set
            {
                if (Item == null)
                {
                    Item = new ArchItem();
                }
                Item.ProgressionLevel = value;
            }
        }

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
        /// <returns>The list of hintinfos that were removed</returns>
        public static List<RequestedHintInfo> UpdateHintInfos(List<RequestedHintInfo> hintInfos, List<Hint> hints)
        {
            var removedList = new List<RequestedHintInfo>();
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
            hintInfos.ForEach(hint =>
            {
                var key = $"{hint.RequesterSlot}_{hint.ItemId}";
                if (!hint.IsFound && finishedList.Contains(key))
                {
                    removedList.Add(hint);
                }
            });
            hintInfos.RemoveAll(hint =>
            {
                var key = $"{hint.RequesterSlot}_{hint.ItemId}";
                return !hint.IsFound && finishedList.Contains(key);
            });
            return removedList;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="roomInfo"></param>
        /// <returns></returns>
        //public HintWrapper ToHintWrapper(RoomInfo roomInfo)
        //{
        //    return new HintWrapper(this, roomInfo);
        //}

        /// <summary>
        /// Creates a RequestedHintInfo from the hint and the input args
        /// </summary>
        public static RequestedHintInfo Create(Hint hint, string information, int priority, int progressionLevel, RoomInfo roomInfo)
        {
            return new RequestedHintInfo
            {
                Item = new ArchItem
                {
                    ItemId = hint.ItemId,
                    Slot = hint.ReceivingPlayer,
                    ProgressionLevel = progressionLevel,
                    RoomInfo = roomInfo
                },
                Location = new ArchLocation
                {
                    LocationId = hint.ItemId,
                    Slot = hint.ReceivingPlayer,
                    RoomInfo = roomInfo
                },
                Information = information,
                Priority = priority
            };
        }
    }
}
