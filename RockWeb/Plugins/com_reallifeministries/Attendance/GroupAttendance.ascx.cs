﻿using System;
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
    [Category( "GroupAttendance" )]
    [Description( "Group Attendance Entry" )]
    [GroupField( "Group", "Either pick a specific group or choose <none> to have group be determined by the groupId page parameter" )]
    public partial class GroupAttendance : RockBlock
    {
        private RockContext ctx;
        private Group _group = null;
        private DefinedValueCache _inactiveStatus = null;
        private bool _canView = false;
        private bool _takesAttendance = false;
        private List<Rock.Model.Attendance> _attendances = new List<Rock.Model.Attendance>();

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

        protected bool attendanceTaken
        {
            get
            {
                return (_attendances != null) &&_attendances.Count > 0;
            }
        }

        protected void Page_Load( object sender, EventArgs e )
        {
            HandleNotification();
            pnlForm.Visible = (_canView && _takesAttendance && !attendanceTaken);
            pnlResults.Visible = attendanceTaken;

            if (attendanceTaken)
            {
                rptAttendees.DataSource = _attendances;
                rptAttendees.DataBind();

            } else if (_canView && _takesAttendance && !IsPostBack)
            {
                BindPeopleList();
                dpAttendanceDate.SelectedDate = DateTime.Now;
            } 
            
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

                        foreach (Person person in people) {
                            var attendance = ctx.Attendances.Create();
                            attendance.PersonAlias = person.PrimaryAlias;
                            attendance.Group = _group;
                            // ADD GROUP LOCATION ?
                            
                            attendance.StartDateTime = (DateTime)dpAttendanceDate.SelectedDate;
                            attendanceService.Add( attendance );
                            _attendances.Add( attendance );
                        }

                        ctx.SaveChanges();
                        FlashMessage( string.Format(
                            "Attendance Recorded for {1} people: {0}", 
                            dpAttendanceDate.SelectedDate.ToString(),
                            attendendPeopleIds.Count
                        ), NotificationBoxType.Success);
                    
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
    }

}