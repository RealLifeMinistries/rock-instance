using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rock.Model;
using Rock.Data;

namespace com.reallifeministries.RockExtensions
{
    public class GroupMatcher
    {
        private double metersInMile = 1609.344;

        public int acceptableMileRadius = 5;
        public int sizeWeight = 10;
        public int locationWeight = 10;
        public int dayOfWeekWeight = 10;
        public int numMatches = 3;

        public List<int> daysOfWeek;
        public Person person;
        public Location personLocation;
        public GroupType groupType;

        public GroupMatcher(Person pers, GroupType gt, List<int> days)
        {
            person = pers;
            personLocation = pers.GetHomeLocation();
            daysOfWeek = days;
            groupType = gt;
        }
        
        public List<GroupMatch> GetMatches()
        {
            var matches = new List<GroupMatch>();
            using (var ctx = new RockContext()) 
            {
               matches = (
                    from gl in ctx.GroupLocations
                    let distance = gl.Location.GeoPoint.Distance(personLocation.GeoPoint)
                    let memberCount = gl.Group.Members.Select(m => m.PersonId).Distinct().Count()
                    where distance <= (metersInMile * acceptableMileRadius)
                    where gl.Group.GroupTypeId == groupType.Id
                    select new GroupMatch {
                        Group = gl.Group,
                        Distance = distance / metersInMile,
                        MemberCount = memberCount
                    }
                   ).Take(numMatches).ToList();

            }
            
            return matches;
        }
    }
}
