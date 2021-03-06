﻿// <copyright>
// Copyright 2013 by the Spark Development Network
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>
//
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Web.UI.WebControls;
using Newtonsoft.Json;
using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Security;
using Rock.Web.Cache;
using Rock.Web.UI;
using Rock.Web.UI.Controls;

namespace com.reallifeministries
{
    [DisplayName( "Advanced Group Member List (RLM Custom)" )]
    [Category( "Groups" )]
    [Description( "Lists all the members of the given group & sub-groups." )]

    [GroupField( "Group", "Either pick a specific group or choose <none> to have group be determined by the groupId page parameter" )]
    [LinkedPage( "Detail Page" )]
    [LinkedPage( "Person Profile Page", "Page used for viewing a person's profile. If set a view profile button will show for each group member.", false, "", "", 2, "PersonProfilePage" )]
    public partial class RLMGroupMemberList : RockBlock, ISecondaryBlock
    {
        #region Private Variables

        private DefinedValueCache _inactiveStatus = null;
        private Group _group = null;
        private bool _canView = false;

        #endregion

        

        #region Control Methods

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Init" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );

            // if this block has a specific GroupId set, use that, otherwise, determine it from the PageParameters
            Guid groupGuid = GetAttributeValue( "Group" ).AsGuid();
            int groupId = 0;
            
            if ( groupGuid == Guid.Empty )
            {
                groupId = PageParameter( "GroupId" ).AsInteger();
            }

            if ( !(groupId == 0 && groupGuid == Guid.Empty ))
            {
                string key = string.Format( "Group:{0}", groupId );
                _group = RockPage.GetSharedItem( key ) as Group;
                if ( _group == null )
                {
                    _group = new GroupService( new RockContext() ).Queryable( "GroupType.Roles" )
                        .Where( g => g.Id == groupId || g.Guid == groupGuid )
                        .FirstOrDefault();
                    RockPage.SaveSharedItem( key, _group );
                }

                if ( _group != null && _group.IsAuthorized( Authorization.VIEW, CurrentPerson ) )
                {
                    _canView = true;

                    rFilter.ApplyFilterClick += rFilter_ApplyFilterClick;
                    gGroupMembers.DataKeyNames = new string[] { "Id" };
                    gGroupMembers.CommunicateMergeFields = new List<string> { "GroupRole" };
                    gGroupMembers.PersonIdField = "PersonId";
                    gGroupMembers.RowDataBound += gGroupMembers_RowDataBound;
                    gGroupMembers.Actions.AddClick += gGroupMembers_AddClick;
                    gGroupMembers.GridRebind += gGroupMembers_GridRebind;
                    gGroupMembers.RowItemText = _group.GroupType.GroupTerm + " " + _group.GroupType.GroupMemberTerm;
                    gGroupMembers.ExportFilename = _group.Name;

                    // make sure they have Auth to edit the block OR edit to the Group
                    bool canEditBlock = IsUserAuthorized( Authorization.EDIT ) || _group.IsAuthorized( Authorization.EDIT, this.CurrentPerson );
                    gGroupMembers.Actions.ShowAdd = canEditBlock;
                    gGroupMembers.IsDeleteEnabled = canEditBlock;
                    if (canEditBlock)
                    {
                        // Add delete column
                        var deleteField = new DeleteField();
                        gGroupMembers.Columns.Add( deleteField );
                        deleteField.Click += DeleteGroupMember_Click;
                    }
                }
            }
        }

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Load" /> event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnLoad( EventArgs e )
        {
            base.OnLoad( e );

            if ( !Page.IsPostBack )
            {
                pnlContent.Visible = _canView;
                if ( _canView )
                {
                    SetFilter();
                    BindGroupMembersGrid();
                }
            }
        }

        #endregion

        #region GroupMembers Grid

        /// <summary>
        /// Handles the RowDataBound event of the gGroupMembers control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Web.UI.WebControls.GridViewRowEventArgs"/> instance containing the event data.</param>
        protected void gGroupMembers_RowDataBound( object sender, System.Web.UI.WebControls.GridViewRowEventArgs e )
        {
            if ( e.Row.RowType == DataControlRowType.DataRow )
            {
                var groupMember = e.Row.DataItem as GroupMember;
                if ( groupMember != null && groupMember.Person != null )
                {
                    if ( _inactiveStatus != null &&
                        groupMember.Person.RecordStatusValueId.HasValue &&
                        groupMember.Person.RecordStatusValueId == _inactiveStatus.Id )
                    {
                        e.Row.AddCssClass( "inactive" );
                        e.Row.ToolTip = "Inactive member";
                    }

                    if ( groupMember.Person.IsDeceased ?? false )
                    {
                        e.Row.AddCssClass( "deceased" );
                        e.Row.ToolTip = "Deceased";
                    }
                    
                    var linkControl = (HyperLink)e.Row.FindControl( "lnkProfile" );
                    linkControl.NavigateUrl = LinkedPageUrl( "PersonProfilePage", new Dictionary<string, string>() { { "PersonId", groupMember.PersonId.ToString() } } );
                    var navi = linkControl.NavigateUrl; 
                }
            }
        }

        /// <summary>
        /// Handles the ApplyFilterClick event of the rFilter control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        protected void rFilter_ApplyFilterClick( object sender, EventArgs e )
        {
            rFilter.SaveUserPreference( MakeKeyUniqueToGroup( "First Name" ), "First Name", tbFirstName.Text );
            rFilter.SaveUserPreference( MakeKeyUniqueToGroup( "Last Name" ), "Last Name", tbLastName.Text );
            rFilter.SaveUserPreference( MakeKeyUniqueToGroup( "Show Sub Groups" ), "Show Sub Groups", tglSubGroups.Checked.ToString() );
            rFilter.SaveUserPreference( MakeKeyUniqueToGroup( "Role" ), "Role", cblRole.SelectedValues.AsDelimited( ";" ) );
            rFilter.SaveUserPreference( MakeKeyUniqueToGroup( "Status" ), "Status", cblStatus.SelectedValues.AsDelimited( ";" ) );

            BindGroupMembersGrid();
        }

        /// <summary>
        /// Rs the filter_ display filter value.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The e.</param>
        protected void rFilter_DisplayFilterValue( object sender, GridFilter.DisplayFilterValueArgs e )
        {


            if ( e.Key == MakeKeyUniqueToGroup( "First Name" ) )
            {
                return;
            }
            else if ( e.Key == MakeKeyUniqueToGroup( "Last Name" ) )
            {
                return;
            }
            else if ( e.Key == MakeKeyUniqueToGroup( "Role" ) )
            {
                e.Value = ResolveValues( e.Value, cblRole );
            }
            else if ( e.Key == MakeKeyUniqueToGroup( "Status" ) )
            {
                e.Value = ResolveValues( e.Value, cblStatus );
            }
            else
            {
                e.Value = string.Empty;
            }

        }

        /// <summary>
        /// Handles the Click event of the DeleteGroupMember control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="Rock.Web.UI.Controls.RowEventArgs" /> instance containing the event data.</param>
        protected void DeleteGroupMember_Click( object sender, Rock.Web.UI.Controls.RowEventArgs e )
        {
            RockContext rockContext = new RockContext();
            GroupMemberService groupMemberService = new GroupMemberService( rockContext );
            GroupMember groupMember = groupMemberService.Get( e.RowKeyId );
            if ( groupMember != null )
            {
                string errorMessage;
                if (!groupMember.Group.IsAuthorized( Authorization.EDIT, this.CurrentPerson ))
                {
                    errorMessage = "You're not authorized to remove this Person";
                    mdGridWarning.Show( errorMessage, ModalAlertType.Warning );
                    return;
                }

                if ( !groupMemberService.CanDelete( groupMember, out errorMessage ) )
                {
                    mdGridWarning.Show( errorMessage, ModalAlertType.Information );
                    return;
                }

                int groupId = groupMember.GroupId;

                groupMemberService.Delete( groupMember );
                rockContext.SaveChanges();

                Group group = new GroupService( rockContext ).Get( groupId );
                if ( group.IsSecurityRole || group.GroupType.Guid.Equals( Rock.SystemGuid.GroupType.GROUPTYPE_SECURITY_ROLE.AsGuid() ) )
                {
                    // person removed from SecurityRole, Flush
                    Rock.Security.Role.Flush( group.Id );
                    Rock.Security.Authorization.Flush();
                }
            }

            BindGroupMembersGrid();
        }

        /// <summary>
        /// Handles the AddClick event of the gGroupMembers control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        /// <exception cref="System.NotImplementedException"></exception>
        protected void gGroupMembers_AddClick( object sender, EventArgs e )
        {
            NavigateToLinkedPage( "DetailPage", "GroupMemberId", 0, "GroupId", _group.Id );
        }

        /// <summary>
        /// Handles the Edit event of the gGroupMembers control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RowEventArgs" /> instance containing the event data.</param>
        protected void gGroupMembers_Edit( object sender, RowEventArgs e )
        {
            NavigateToLinkedPage( "DetailPage", "GroupMemberId", e.RowKeyId );
        }

        /// <summary>
        /// Handles the GridRebind event of the gGroupMembers control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        /// <exception cref="System.NotImplementedException"></exception>
        protected void gGroupMembers_GridRebind( object sender, EventArgs e )
        {
            BindGroupMembersGrid();
        }

        #endregion

        #region Internal Methods

        /// <summary>
        /// Binds the filter.
        /// </summary>
        private void SetFilter()
        {

            string showSubGroups = rFilter.GetUserPreference( MakeKeyUniqueToGroup( "Show Sub Groups" ) );
            if (string.IsNullOrWhiteSpace( showSubGroups ))
            {
                tglSubGroups.Checked = false;
            }
            else
            {
                tglSubGroups.Checked = showSubGroups.AsBoolean();
            }

            

            if ( _group != null )
            {
                var rockContext = new RockContext();
                GroupService groupService = new GroupService( rockContext );

                var groups = new List<Group>();
                groups.Add( _group );

                if (tglSubGroups.Checked)
                {
                    var descendedGroups = groupService.GetAllDescendents( _group.Id ).ToList();
                    groups.AddRange(descendedGroups);
                }

                GroupMemberService gmserv = new GroupMemberService( rockContext );
                var groupIds = groups.Select( a => a.Id ).ToList();
                var roleNames = rockContext.GroupMembers
                                    .Where( gm => groupIds.Contains( gm.GroupId ) )
                                    .Select( a => a.GroupRole.Name )
                                    .Distinct().ToList();
                roleNames.Sort();
                cblRole.DataSource = roleNames;
                cblRole.DataBind();
            }

            cblStatus.BindToEnum<GroupMemberStatus>();

            tbFirstName.Text = rFilter.GetUserPreference( MakeKeyUniqueToGroup( "First Name" ) );
            tbLastName.Text = rFilter.GetUserPreference( MakeKeyUniqueToGroup( "Last Name" ) );

            string roleValue = rFilter.GetUserPreference( MakeKeyUniqueToGroup( "Role" ) );
            if ( !string.IsNullOrWhiteSpace( roleValue ) )
            {
                cblRole.SetValues( roleValue.Split( ';' ).ToList() );
            }
           
            string statusValue = rFilter.GetUserPreference( MakeKeyUniqueToGroup( "Status" ) );
            if ( !string.IsNullOrWhiteSpace( statusValue ) )
            {
                cblStatus.SetValues( statusValue.Split( ';' ).ToList() );
            }

        }

        /// <summary>
        /// Binds the group members grid.
        /// </summary>
        protected void BindGroupMembersGrid()
        {
            if ( _group != null )
            {
                pnlGroupMembers.Visible = true;

                lHeading.Text = string.Format( "{0} {1}", _group.GroupType.GroupTerm, _group.GroupType.GroupMemberTerm.Pluralize() );

                var rockContext = new RockContext();
                
                var groups = new List<Group>();
                groups.Add( _group );

                if (tglSubGroups.Checked)
                {
                    GroupService groupService = new GroupService( rockContext );
                    var descendedGroups = groupService.GetAllDescendents( _group.Id ).ToList();
                    groups.AddRange( descendedGroups );
                }
  
                nbRoleWarning.Visible = false;
                rFilter.Visible = true;
                gGroupMembers.Visible = true;

                var groupIds = groups.Select( g => g.Id ).ToList();

                GroupMemberService groupMemberService = new GroupMemberService( rockContext );
                var qry = groupMemberService.Queryable( "Person,GroupRole,Group", true )
                    .Where( m => groupIds.Contains( m.GroupId ) );

                // Filter by First Name
                string firstName = tbFirstName.Text;
                if ( !string.IsNullOrWhiteSpace( firstName ) )
                {
                    qry = qry.Where( m => m.Person.FirstName.StartsWith( firstName ) );
                }

                // Filter by Last Name
                string lastName = tbLastName.Text;
                if ( !string.IsNullOrWhiteSpace( lastName ) )
                {
                    qry = qry.Where( m => m.Person.LastName.StartsWith( lastName ) );
                }

                var roles = cblRole.SelectedValues;
                    
                if ( roles.Any() )
                {
                    qry = qry.Where( m => roles.Contains( m.GroupRole.Name ) );
                }

                // Filter by Status
                var statuses = new List<GroupMemberStatus>();
                foreach ( string status in cblStatus.SelectedValues )
                {
                    if ( !string.IsNullOrWhiteSpace( status ) )
                    {
                        statuses.Add( status.ConvertToEnum<GroupMemberStatus>() );
                    }
                }
                if ( statuses.Any() )
                {
                    qry = qry.Where( m => statuses.Contains( m.GroupMemberStatus ) );
                }

                _inactiveStatus = DefinedValueCache.Read( Rock.SystemGuid.DefinedValue.PERSON_RECORD_STATUS_INACTIVE );

                SortProperty sortProperty = gGroupMembers.SortProperty;

                List<GroupMember> groupMembers = null;

                if ( sortProperty != null )
                {
                    groupMembers = qry.Sort( sortProperty ).ToList();
                }
                else
                {
                    groupMembers = qry.OrderBy( a => a.Person.LastName ).ThenBy( a => a.Person.FirstName ).ToList();
                }
                                                          
                gGroupMembers.DataSource = groupMembers/*.Select( m => new
                {
                    m.Id,
                    m.Guid,
                    m.PersonId,
                    m.Person,
                    Group = m.Group.Name,
                    Name = m.Person.NickName + " " + m.Person.LastName,
                    GroupRole = m.GroupRole.Name,
                    m.GroupMemberStatus
                } )*/.ToList();

                gGroupMembers.DataBind();
            }
            else
            {
                pnlGroupMembers.Visible = false;
            }
        }

        /// <summary>
        /// Resolves the values.
        /// </summary>
        /// <param name="values">The values.</param>
        /// <param name="listControl">The list control.</param>
        /// <returns></returns>
        private string ResolveValues( string values, System.Web.UI.WebControls.CheckBoxList checkBoxList )
        {
            var resolvedValues = new List<string>();

            foreach ( string value in values.Split( ';' ) )
            {
                var item = checkBoxList.Items.FindByValue( value );
                if ( item != null )
                {
                    resolvedValues.Add( item.Text );
                }
            }

            return resolvedValues.AsDelimited( ", " );
        }

        /// <summary>
        /// Makes the key unique to group.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns></returns>
        private string MakeKeyUniqueToGroup( string key )
        {
            if ( _group != null )
            {
                return string.Format( "{0}-{1}", _group.Id, key );
            }
            return key;
        }

        #endregion

        #region ISecondaryBlock

        /// <summary>
        /// Sets the visible.
        /// </summary>
        /// <param name="visible">if set to <c>true</c> [visible].</param>
        public void SetVisible( bool visible )
        {
            pnlContent.Visible = visible;
        }

        #endregion
    }
}