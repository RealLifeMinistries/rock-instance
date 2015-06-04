using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rock.Model;

namespace com.reallifeministries.RockExtensions
{
    public class GroupMatch
    {
        public Group Group { get; set; }
        public Location Location { get; set; }
        public double? Distance { get; set; }
        public int MemberCount { get; set; }
        public Schedule Schedule { get; set; }
    }
}
