using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;

using Rock;
using Rock.Data;
using Rock.Model;
using Rock.Attribute;
using Rock.Constants;
using Rock.Web.UI;
using Rock.Web.UI.Controls;
using Rock.Web.Cache;

namespace com.reallifeministries.Attendance
{
    /// <summary>
    /// Attendence Entry
    /// </summary>
    [Category( "Attendance" )]
    [Description( "Attendance Entry" )]
    //[LinkedPage( "Admin Page" )]
    //[BooleanField( "Show Key Pad", "Show the number key pad on the search screen", false )]
    //[IntegerField( "Minimum Text Length", "Minimum length for text searches (defaults to 4).", false, 4 )]
    //[IntegerField( "Maximum Text Length", "Maximum length for text searches (defaults to 20).", false, 20 )]
    public partial class AttendanceEntry : RockBlock
    {
        protected RockContext ctx;

        protected void Page_Load(object sender, EventArgs e)
        {
            ctx = new RockContext();

            if (!IsPostBack)
            {
                pnlResults.Visible = false;
                BindCampusPicker();

                // @TODO: set campus to default campus for person
            }
        }

       
        protected void BindCampusPicker()
        {
            ddlCampus.Campuses = CampusCache.All();
        }


        protected void btnSearch_Click( object sender, EventArgs e )
        {
            lblMessage.Text = null;

            var personService = new PersonService( ctx );
            pnlResults.Visible = true;

            gResults.Caption = "Search Results";

            if (!String.IsNullOrEmpty(tbPhoneNumber.Text))
            {
                gResults.DataSource = personService.GetByPhonePartial( tbPhoneNumber.Text ).ToList();
            }
            else if (!String.IsNullOrEmpty( tbName.Text ))
            {
                gResults.DataSource = personService.GetByFullName( tbName.Text, true ).ToList();
            }
            else
            {
                gResults.DataSource = ctx.People.ToList();
            }
            gResults.DataBind();
        }
      
        protected void btnFamily_Click( object sender, EventArgs e )
        {
            LinkButton btn = (LinkButton)sender;
            int id = Convert.ToInt32( btn.CommandArgument );

            var personService = new PersonService( ctx );
            var person = personService.Get( id );

            person.LoadAttributes();

            gResults.Caption = "Family of " + person.FullName;
            gResults.DataSource = person.GetFamilyMembers(true).Select( gm => gm.Person ).ToList();
            gResults.DataBind();
        }
        protected void btnClear_Click( object sender, EventArgs e )
        {
            clearResults();
            clearForm();
            lblMessage.Text = null;
        }

        protected void clearForm()
        {
            tbName.Text = null;
            tbPhoneNumber.Text = null;
        }

        protected void clearResults()
        {
            gResults.DataSource = null;
            gResults.Caption = null;
            gResults.DataBind();
            pnlResults.Visible = false;
        }
        protected void btnRecord_Click( object sender, EventArgs e )
        {
            var peopleIds = new List<int>();

            foreach (GridViewRow row in gResults.Rows)
            {
                CheckBox cb = (CheckBox)row.FindControl( "chkSelect" );
                if (cb.Checked)
                {
                    string dataKey = gResults.DataKeys[row.RowIndex].Value.ToString();
                    if (!String.IsNullOrEmpty(dataKey))
                    {
                        peopleIds.Add( Convert.ToInt32(dataKey) );
                    } 
                }
            }

            /*if (!String.IsNullOrEmpty(campusPicker.SelectedValue))
            {
                recorder.CampusId = Convert.ToInt32(campusPicker.SelectedValue);
            }*/

            var personService = new PersonService( ctx );
            var attendanceService = new AttendanceService( ctx );
            var people = personService.GetByIds( peopleIds );

            foreach (Person person in people)
            {
                Rock.Model.Attendance attendance = ctx.Attendances.Create();
                attendance.PersonAlias = person.PrimaryAlias;
                attendance.StartDateTime = (DateTime)dpAttendanceDate.SelectedDate;
                var campus_id = ddlCampus.SelectedValue;
                if (!String.IsNullOrEmpty( campus_id ))
                {
                    attendance.CampusId = Convert.ToInt32(campus_id);
                }
                
                attendanceService.Add( attendance );
            }

            ctx.SaveChanges();

            clearForm();
            clearResults();

            lblMessage.Text = "Attendance Recorded FOR: " + String.Join( ", ", people.Select(p => p.FullName ).ToArray() );
        }
}
}
