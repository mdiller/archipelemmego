using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchipeLemmeGo
{
    /// <summary>
    /// An exception to be bubbled up to the user to indicate that there was a failure at some point in the logic
    /// </summary>
    public class UserError : Exception
    {
        public UserError(string message) : base(message) { }
    }
}
