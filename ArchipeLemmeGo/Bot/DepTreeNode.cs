using ArchipeLemmeGo.Datamodel.Arch;
using ArchipeLemmeGo.Datamodel.Infos;
using Discord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchipeLemmeGo.Bot
{
    /// <summary>
    /// Node status
    /// </summary>
    public enum DepTreeNodeStatus
    {
        CanDo,
        Blocked,
        Done,
        Unknown,
        Unreachable
    }

    /// <summary>
    /// A node in the dep tree, representing either an item or a location, and its children dependancies
    /// </summary>
    public abstract class DepTreeNode
    {
        public DepTreeNode? Parent { get; set; } = null;
        public abstract string Text { get; }
        public abstract string SubText { get; }

        public DepTreeNodeStatus Status { get; set; } = DepTreeNodeStatus.Unknown;

        public List<DepTreeNode> Children { get; set; } = new List<DepTreeNode>();

        protected DepTreeNode(DepTreeNode parent)
        {
            this.Parent = parent;
        }

        /// <summary>
        /// Calculates the status of this node and its children, and trims the children from nodes that are done
        /// </summary>
        public virtual void CalcStatusAndTrim()
        {
            Children.ForEach(c => c.CalcStatusAndTrim());
            if (Status == DepTreeNodeStatus.Unknown && Children.All(c => c.Status == DepTreeNodeStatus.Done))
            {
                Status = DepTreeNodeStatus.CanDo;
            }
            // calc status for children
            if (Status == DepTreeNodeStatus.Done)
            {
                Children.Clear();
                return;
            }
        }

        /// <summary>
        /// Checks to see if any ancestors match the given predicate
        /// </summary>
        public bool AnyAncestorsMatch(Func<DepTreeNode, bool> predicate)
        {
            var current = Parent;
            while (current != null)
            {
                if (predicate(current))
                {
                    return true;
                }
                current = current.Parent;
            }
            return false;
        }
    }

    public class DepTreeItemNode : DepTreeNode
    {
        public DepTreeItemNode(ArchItem item, DepTreeNode parent) : base(parent)
        {
            var roomInfo = item.RoomInfo;
            Item = item;

            if (AnyAncestorsMatch(node => node is DepTreeItemNode itn && itn.Item == item))
            { // circular dependancy detected, mark as unreachable and return
                Status = DepTreeNodeStatus.Unreachable;
                return;
            }

            var hintInfos = roomInfo.RequestedHints.Where(h => h.Item.Equals(item)).ToList();

            // if no hintinfos, set status unknown
            if (hintInfos.Count == 0)
            {
                Status = DepTreeNodeStatus.Unknown;
                return;
            }

            // Set status as done/blocked/inprogress
            if (Item.ProgressionLevel == 0)
            {
                Children.AddRange(hintInfos
                    .Where(h => !h.IsFound)
                    .Select(h => new DepTreeLocationNode(h.Location, this)));

                Status = Children.Count > 0 ? DepTreeNodeStatus.Blocked : DepTreeNodeStatus.Done;
                // TODO: iterate to recalulate status at end mebbe?
            }
            else
            {
                var remainingNeeded = Item.ProgressionLevel - hintInfos.Count(h => h.IsFound);
                if (remainingNeeded <= 0)
                { // maybe in future we'll want to show stuff that is done too?
                    Status = DepTreeNodeStatus.Done;
                }
                else
                {
                    Status = DepTreeNodeStatus.Blocked;
                    var locNodes = hintInfos
                        .Where(h => !h.IsFound)
                        .Select(h => new DepTreeLocationNode(h.Location, this))
                        .AsEnumerable<DepTreeNode>()
                        .ToList();
                    if (remainingNeeded == hintInfos.Count)
                    {
                        Children.AddRange(hintInfos
                            .Where(h => !h.IsFound)
                            .Select(h => new DepTreeLocationNode(h.Location, this))
                            .AsEnumerable<DepTreeNode>());
                    }
                    else
                    {
                        var multiNode = new DepTreeMultiNode<DepTreeLocationNode>(this)
                        {
                            RequiredCount = remainingNeeded
                        };
                        multiNode.Children = hintInfos
                            .Where(h => !h.IsFound)
                            .Select(h => new DepTreeLocationNode(h.Location, multiNode))
                            .AsEnumerable<DepTreeNode>()
                            .ToList();
                        Children.Add(multiNode);
                    }
                }
            }
        }

        public ArchItem Item { get; set; }

        public List<RequestedHintInfo> Hints { get; set; }

        public override string Text => $"{Item.Name}";
        public override string SubText => $"({Item.Player.Name})";
    }

    public class DepTreeLocationNode : DepTreeNode
    {
        public DepTreeLocationNode(ArchLocation location, DepTreeNode parent) : base(parent)
        {
            var roomInfo = location.RoomInfo;
            Location = location;

            if (AnyAncestorsMatch(node => node is DepTreeLocationNode itn && itn.Location == location))
            { // circular dependancy detected, mark as unreachable and return
                Status = DepTreeNodeStatus.Unreachable;
                return;
            }

            var dependancyLinks = roomInfo.Dependancies.Where(dep => dep.Dependant == location).ToList();

            // if no dependancies, set status unknown
            if (dependancyLinks.Count == 0)
            {
                Status = DepTreeNodeStatus.CanDo;
                return;
            }

            Status = DepTreeNodeStatus.Blocked;

            foreach (var dependancyLink in dependancyLinks)
            {
                if (dependancyLink.Prerequisites.Count == 1)
                {
                    Children.Add(new DepTreeItemNode(dependancyLink.Prerequisites.First(), this));
                }
                else
                {
                    var multiNode = new DepTreeMultiNode<DepTreeItemNode>(this)
                    {
                        RequiredCount = dependancyLink.RequiredCount
                    };
                    multiNode.Children = dependancyLink.Prerequisites
                            .Select(i => new DepTreeItemNode(i, multiNode))
                            .AsEnumerable<DepTreeNode>()
                            .ToList();
                    Children.Add(multiNode);
                }
            }
        }

        public ArchLocation Location { get; set; }

        public List<DependancyLink> DepLink { get; set; }

        public override void CalcStatusAndTrim()
        {
            base.CalcStatusAndTrim();
            if (Status != DepTreeNodeStatus.Done && Children.Count > 0)
            {
                Status = Children.All(c => c.Status == DepTreeNodeStatus.Done) ? DepTreeNodeStatus.CanDo : DepTreeNodeStatus.Blocked;
            }
        }

        public override string Text => $"{Location.Name}";
        public override string SubText => $"({Location.Player.Name})";

    }

    public class DepTreeMultiNode<T> : DepTreeNode
    {
        public DepTreeMultiNode(DepTreeNode parent) : base(parent) { }

        public int RequiredCount { get; set; }

        public override void CalcStatusAndTrim()
        {
            base.CalcStatusAndTrim();
            if (Status != DepTreeNodeStatus.Done)
            {
                var doneCount = Children.Count(c => c.Status == DepTreeNodeStatus.Done);

                // calc status for children
                if (doneCount >= RequiredCount)
                {
                    Status = DepTreeNodeStatus.Done;
                    Children.Clear();
                    return;
                }
                else
                {
                    Status = DepTreeNodeStatus.Blocked;
                }
            }
        }

        public override string Text => $"{RequiredCount} of";
        public override string SubText => null;
    }
}
