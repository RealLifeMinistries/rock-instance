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
        public int numMatches = 3;

        public List<DayOfWeek> daysOfWeek;
        public Person person;
        public Location personLocation;
        public GroupType groupType;

        public GroupMatcher(Person pers, GroupType gt, List<DayOfWeek> days)
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
                    where gl.Group.Schedule.WeeklyDayOfWeek != null
                    where  daysOfWeek.Contains( (DayOfWeek)gl.Group.Schedule.WeeklyDayOfWeek )
                    where gl.Group.GroupTypeId == groupType.Id
                    orderby distance
                    select new GroupMatch {
                        Group = gl.Group,
                        Distance = distance / metersInMile,
                        MemberCount = memberCount,
                        Location = gl.Location,
                        Schedule = gl.Group.Schedule
                    }
                   ).Take(numMatches).ToList();
            }   
            return matches;
        }
    }
}
