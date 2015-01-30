using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

using Rock;
using Rock.Data;
using Rock.Model;
using Rock.Attribute;
using Rock.Constants;
using Rock.Web.UI;
using Rock.Web.UI.Controls;
using Rock.Web.Cache;
using Rock.Security;

namespace com.reallifeministries.Attendance
{
    /// <summary>
    /// Group Attendence Entry
    /// </summary>
    [Category( "Attendance" )]
    [Description( "Group Attendance Entry; Should be placed on a group detail page." )]
    [GroupField( "Group", "Either pick a specific group or choose <none> to have group be determined by the groupId page parameter" )]
    public partial class GroupAttendance : RockBlock
    {
        private RockContext ctx;
        private Group _group = null;
        private DefinedValueCache _inactiveStatus = null;
        private bool _canView = false;
        private bool _takesAttendance = false;
        private List<Rock.Model.Attendance> _attendances = null;

        protected bool wasAttendanceTaken
        {
            get
            {
                if (attendanceList != null)
                {
                    return attendanceList.Count > 0;
                }
                else
                {
                    return false;
                }
            }
        }

        private List<Rock.Model.Attendance> attendanceList
        {
            get
            {
                if (_attendances != null)
                {
                    return _attendances;
                }
                else
                {
                    if (ViewState["attendanceIds"] != null)
                    {
                        var attendanceService = new AttendanceService( ctx );
                        var attendanceIds = new List<int>( ((Array) ViewState["attendanceIds"]).OfType<int>().ToList() );
                        _attendances = attendanceService.GetByIds( attendanceIds ).ToList();
                        return _attendances;
                    }
                    else
                    {
                        return null;
                    }
                }
            }
            set
            {
                _attendances = value;
                if (value != null)
                    ViewState["attendanceIds"] = (from a in value select a.Id).ToArray();
                else
                {
                    ViewState["attendanceIds"] = null;
                }
            }
        }

        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );

            ctx = new RockContext();

            // if this block has a specific GroupId set, use that, otherwise, determine it from the PageParameters
            Guid groupGuid = GetAttributeValue( "Group" ).AsGuid();
            int groupId = 0;

            if (groupGuid == Guid.Empty)
            {
                groupId = PageParameter( "GroupId" ).AsInteger();
            }

            if (!(groupId == 0 && groupGuid == Guid.Empty))
            {
                string key = string.Format( "Group:{0}", groupId );
                _group = RockPage.GetSharedItem( key ) as Group;
                if (_group == null)
                {
                    _group = new GroupService( ctx ).Queryable( "GroupType" )
                        .Where( g => g.Id == groupId || g.Guid == groupGuid )
                        .FirstOrDefault();
                    RockPage.SaveSharedItem( key, _group );
                }

                if (_group != null && _group.IsAuthorized( Authorization.VIEW, CurrentPerson ))
                {
                    _canView = true;
                    if (_group.GroupType.TakesAttendance)
                    {
                        _takesAttendance = true;
                    }
                }
            }

            
        }

        protected void Page_Load( object sender, EventArgs e )
        {
            HandleNotification();

            if (_canView && _takesAttendance && !IsPostBack)
            {
                BindPeopleList();
                dpAttendanceDate.SelectedDate = DateTime.Now;
            }
        }

        protected void Page_PreRender( object sender, EventArgs e )
        {
            BindVisibility();
        }

        protected void BindVisibility()
        {
            pnlResults.Visible = wasAttendanceTaken;
            pnlForm.Visible = (_canView && _takesAttendance && !wasAttendanceTaken);
        }

        protected void BindAttendanceList()
        {
            rptAttendees.DataSource = attendanceList;
            rptAttendees.DataBind();
        }

        protected void BindPeopleList()
        {
            if (_group != null)
            {
                _inactiveStatus = DefinedValueCache.Read( Rock.SystemGuid.DefinedValue.PERSON_RECORD_STATUS_INACTIVE );



                var query = (from gm in ctx.GroupMembers
                             where gm.GroupId == _group.Id && gm.Person.RecordStatusValueId != _inactiveStatus.Id
                             orderby gm.GroupRole.Order, gm.Person.LastName
                             select new
                             {
                                 PersonId = gm.PersonId,
                                 Person = gm.Person,
                                 Role = gm.GroupRole
                             });
                var groupMembers = (from d in query.ToList()
                                    select new
                                    {
                                        PersonId = d.PersonId,
                                        PersonDisplay = d.Person.FullName/* + " ( " + d.Role.Name + " )"*/
                                    });
                cblMembers.DataSource = groupMembers.ToList();
                cblMembers.DataValueField = "PersonId";
                cblMembers.DataTextField = "PersonDisplay";
                cblMembers.DataBind();
            }
        }

        protected void FlashMessage( String message )
        {
            FlashMessage( message, NotificationBoxType.Info );
        }

        protected void FlashMessage( String message, NotificationBoxType type )
        {
            nbMessage.Visible = true;
            nbMessage.Text = message;
            nbMessage.NotificationBoxType = type;
        }

        protected void HandleNotification()
        {
            nbMessage.Visible = false;
            if (_canView)
            {
                if (!_takesAttendance)
                {
                    FlashMessage("Does not take attendance");                
                }
            }
            else
            {
                FlashMessage( "Your security level does not allow this action" , NotificationBoxType.Warning);
            }
        }
        protected void btnReset_Click( object sender, EventArgs e )
        {
            resetCheckBoxes();
            attendanceList = null;
            BindAttendanceList();
        }
       
        protected void btnRecordAttendance_Click( object sender, EventArgs e )
        {
            if (dpAttendanceDate.SelectedDate != null)
            {
                var attendendPeopleIds = new List<int>();
                foreach (ListItem item in cblMembers.Items)
                {
                    if (item.Selected)
                    {
                        attendendPeopleIds.Add( Int32.Parse(item.Value) );
                    }
                }

                if (attendendPeopleIds.Count > 0)
                {
                  
                    var attendanceService = new AttendanceService( ctx );
                    var peopleService = new PersonService( ctx );
                    var people = peopleService.GetByIds( attendendPeopleIds );
                    var attendances = new List<Rock.Model.Attendance>();

                    foreach (Person person in people) {
                        var attendance = new Rock.Model.Attendance();
                        attendance.PersonAlias = person.PrimaryAlias;
                        attendance.Group = _group;
                        // ADD GROUP LOCATION ?
                        
                        attendance.StartDateTime = (DateTime)dpAttendanceDate.SelectedDate;
                        if (attendance.IsValid)
                        {
                            attendanceService.Add( attendance );
                            attendances.Add( attendance );
                        }
                    }

                    ctx.SaveChanges();
                    FlashMessage( string.Format(
                        "Attendance Recorded for {1} people on {0}", 
                        dpAttendanceDate.SelectedDate.Value.ToLongDateString(),
                        attendendPeopleIds.Count
                    ), NotificationBoxType.Success);


                    attendanceList = attendances;

                    BindAttendanceList();

                    resetCheckBoxes();
                }
                else
                {
                    FlashMessage( "Please select at least one Attendee", NotificationBoxType.Danger );
                }
            }
            else
            {
                FlashMessage( "Attended Date is required" , NotificationBoxType.Danger );
            }
        }

        protected void resetCheckBoxes()
        {
            foreach (ListItem item in cblMembers.Items)
            {
                item.Selected = false;
            }
        }
        protected void rptAttendees_ItemCommand( object source, RepeaterCommandEventArgs e )
        {
            var attendId = Int32.Parse( e.CommandArgument.ToString() );
            var attendanceService = new Rock.Model.AttendanceService( ctx );
            var attendance = attendanceService.Get( attendId );
            var newList = new List<Rock.Model.Attendance>();

            attendanceService.Delete(attendance);
            ctx.SaveChanges();
            
            foreach(var item in attendanceList) {
                if (item.Id != attendId)
                {
                    newList.Add( item );
                }
            }

            attendanceList = newList;
            BindAttendanceList();           
        }
}

}