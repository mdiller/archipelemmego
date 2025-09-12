using ArchipeLemmeGo.Datamodel.Arch;
using Discord;
using Microsoft.Msagl.Drawing;
using Microsoft.Msagl.Layout.Layered;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchipeLemmeGo.Bot
{
    /// <summary>
    /// A dependancy tree for a given item
    /// </summary>
    public class DependancyTree
    {
        /// <summary>
        /// The starting node of the tree
        /// </summary>
        public DepTreeItemNode RootNode { get; set; }

        public DependancyTree(ArchItem rootItem)
        {
            RootNode = new DepTreeItemNode(rootItem, null);
            RootNode.CalcStatusAndTrim();
        }
    }
}
