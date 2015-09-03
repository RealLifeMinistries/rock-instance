﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.ComponentModel;

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
    [DisplayName( "SubGroupInfo (RLM Custom)" )]
    [Category( "Groups" )]
    [Description( "Lists all the sub groups of the given group & member counts" )]

    [GroupField( "Group", "Either pick a specific group or choose <none> to have group be determined by the groupId page parameter" )]
    [LinkedPage( "Detail Page" )]
    public partial class SubGroupInfo : Rock.Web.UI.RockBlock, ISecondaryBlock
    {
        #region Private Variables

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
                    _group = new GroupService( new RockContext() ).Queryable( "GroupType.Roles" )
                        .Where( g => g.Id == groupId || g.Guid == groupGuid )
                        .FirstOrDefault();
                    RockPage.SaveSharedItem( key, _group );
                }

                if (_group != null && _group.IsAuthorized( Authorization.VIEW, CurrentPerson ))
                {
                    _canView = true;
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
                    BindSubGroupsGrid();
                }
            }
        }

        #endregion

        #region SubGroupsGrid

         /// <summary>
        /// Handles the GridRebind event of the gSubGroups control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        /// <exception cref="System.NotImplementedException"></exception>
        protected void gSubGroups_GridRebind( object sender, EventArgs e )
        {
            BindSubGroupsGrid();
        }

        #endregion

        #region Internal Methods

        protected void BindSubGroupsGrid()
        {
            if (_group != null)
            {
                gSubGroups.Visible = true;

                var rockContext = new RockContext();

                var subGroups = rockContext.Groups.Where( g => g.ParentGroupId == _group.Id ).Select( g => new
                    {
                        Group = g,
                        InactiveMembers = g.Members.Where(m => m.GroupMemberStatus == GroupMemberStatus.Inactive).Count(),
                        PendingMembers = g.Members.Where( m => m.GroupMemberStatus == GroupMemberStatus.Pending ).Count(),
                        ActiveMembers = g.Members.Where(m => m.GroupMemberStatus == GroupMemberStatus.Active).Count()
                    }
                ).ToList();
                gSubGroups.DataSource = subGroups;
                gSubGroups.DataBind();
            }
            else
            {
                gSubGroups.Visible = false;
            }
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
