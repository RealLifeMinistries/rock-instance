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
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Linq;
using Rock;
using Rock.Constants;
using Rock.Data;
using Rock.Model;
using Rock.Web.UI;
using Rock.Web;
using Rock.Web.Cache;
using System.IO;
using System.ComponentModel;
using Rock.Security;

namespace RockWeb.Blocks.Crm
{
    /// <summary>
    /// 
    /// </summary>
    [DisplayName("Layout Detail")]
    [Category("CMS")]
    [Description("Displays the details for a specific layout.")]
    public partial class LayoutDetail : RockBlock, IDetailBlock
    {

        #region Fields

        // used for private variables

        #endregion

        #region Properties

        // used for public / protected properties

        #endregion

        #region Base Control Methods

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Load" /> event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnLoad( EventArgs e )
        {
            base.OnLoad( e );

            if ( !Page.IsPostBack )
            {
                string siteId = PageParameter( "siteId" );
                string layoutId = PageParameter( "layoutId" );
                if ( !string.IsNullOrWhiteSpace( layoutId ) )
                {
                    if ( string.IsNullOrWhiteSpace( siteId ) )
                    {
                        ShowDetail( "layoutId", int.Parse( layoutId ) );
                    }
                    else
                    {
                        ShowDetail( "layoutId", int.Parse( layoutId ), int.Parse( siteId ) );
                    }
                }
                else
                {
                    upDetail.Visible = false;
                }
            }
        }

        /// <summary>
        /// Returns breadcrumbs specific to the block that should be added to navigation
        /// based on the current page reference.  This function is called during the page's
        /// oninit to load any initial breadcrumbs
        /// </summary>
        /// <param name="pageReference">The page reference.</param>
        /// <returns></returns>
        public override List<BreadCrumb> GetBreadCrumbs( PageReference pageReference )
        {
            var breadCrumbs = new List<BreadCrumb>();

            int? layoutId = PageParameter(pageReference, "layoutId" ).AsInteger();
            if ( layoutId != null )
            {
                Layout layout = new LayoutService().Get( layoutId.Value );
                if ( layout != null )
                {
                    breadCrumbs.Add( new BreadCrumb( layout.Name, pageReference ) );
                }
                else
                {
                    breadCrumbs.Add( new BreadCrumb( "New Layout", pageReference ) );
                }
            }
            else
            {
                // don't show a breadcrumb if we don't have a pageparam to work with
            }

            return breadCrumbs;
        }

        #endregion

        #region Events

        /// <summary>
        /// Handles the Click event of the btnEdit control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        protected void btnEdit_Click( object sender, EventArgs e )
        {
            var layout = new LayoutService().Get( int.Parse( hfLayoutId.Value ) );
            ShowEditDetails( layout );
        }

        /// <summary>
        /// Handles the Click event of the btnSave control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        protected void btnSave_Click( object sender, EventArgs e )
        {
            if ( Page.IsValid )
            {
                LayoutService layoutService = new LayoutService();
                Layout layout;

                int layoutId = int.Parse( hfLayoutId.Value );

                // if adding a new layout 
                if ( layoutId.Equals( 0 ) )
                {
                    layout = new Layout { Id = 0 };
                    layout.SiteId = hfSiteId.ValueAsInt();
                }
                else
                {
                    //load existing group member
                    layout = layoutService.Get( layoutId );
                }

                layout.Name = tbLayoutName.Text;
                layout.Description = tbDescription.Text;
                layout.FileName = ddlLayout.SelectedValue;

                if ( !layout.IsValid )
                {
                    return;
                }

                RockTransactionScope.WrapTransaction( () =>
                {
                    if ( layout.Id.Equals( 0 ) )
                    {
                        layoutService.Add( layout, CurrentPersonAlias );
                    }

                    layoutService.Save( layout, CurrentPersonAlias );
                } );

                LayoutCache.Flush( layout.Id );

                Dictionary<string, string> qryParams = new Dictionary<string, string>();
                qryParams["layoutId"] = layout.Id.ToString();
                NavigateToPage( RockPage.Guid, qryParams );
            }
        }

        /// <summary>
        /// Handles the Click event of the btnCancel control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        protected void btnCancel_Click( object sender, EventArgs e )
        {
            if ( hfLayoutId.Value.Equals( "0" ) )
            {
                // Cancelling on Add
                Dictionary<string, string> qryString = new Dictionary<string, string>();
                qryString["siteId"] = hfSiteId.Value;
                NavigateToParentPage( qryString );
            }
            else
            {
                // Cancelling on Edit
                Layout layout = new LayoutService().Get( int.Parse( hfLayoutId.Value ) );
                ShowReadonlyDetails( layout );
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Shows the detail.
        /// </summary>
        /// <param name="itemKey">The item key.</param>
        /// <param name="itemKeyValue">The item key value.</param>
        public void ShowDetail( string itemKey, int itemKeyValue )
        {
            ShowDetail( itemKey, itemKeyValue, null );
        }

        /// <summary>
        /// Shows the detail.
        /// </summary>
        /// <param name="itemKey">The item key.</param>
        /// <param name="itemKeyValue">The item key value.</param>
        /// <param name="siteId">The group id.</param>
        public void ShowDetail( string itemKey, int itemKeyValue, int? siteId )
        {
            if ( !itemKey.Equals( "layoutId" ) )
            {
                return;
            }

            Layout layout = null;

            if ( !itemKeyValue.Equals( 0 ) )
            {
                layout = new LayoutService().Get( itemKeyValue );
            }
            else
            {
                // only create a new one if parent was specified
                if ( siteId.HasValue )
                {
                    layout = new Layout { Id = 0 };
                    layout.SiteId = siteId.Value;
                }
            }

            if ( layout == null )
            {
                return;
            }

            hfSiteId.Value = layout.SiteId.ToString();
            hfLayoutId.Value = layout.Id.ToString();

            bool readOnly = false;

            nbEditModeMessage.Text = string.Empty;
            if ( !IsUserAuthorized( Authorization.EDIT ) )
            {
                readOnly = true;
                nbEditModeMessage.Text = EditModeMessage.ReadOnlyEditActionNotAllowed( Rock.Model.Layout.FriendlyTypeName );
            }

            if ( layout.IsSystem )
            {
                nbEditModeMessage.Text = EditModeMessage.System( Rock.Model.Layout.FriendlyTypeName );
            }

            if ( readOnly )
            {
                btnEdit.Visible = false;
                //btnDelete.Visible = false;
                ShowReadonlyDetails( layout );
            }
            else
            {
                btnEdit.Visible = true;
                //btnDelete.Visible = !layout.IsSystem;
                if ( layout.Id > 0 )
                {
                    ShowReadonlyDetails( layout );
                }
                else
                {
                    ShowEditDetails( layout );
                }
            }
        }

        /// <summary>
        /// Shows the edit details.
        /// </summary>
        /// <param name="layout">The layout.</param>
        private void ShowEditDetails( Rock.Model.Layout layout )
        {
            if ( layout.Id.Equals( 0 ) )
            {
                lReadOnlyTitle.Text = ActionTitle.Add( Rock.Model.Layout.FriendlyTypeName ).FormatAsHtmlTitle();
            }
            else
            {
                lReadOnlyTitle.Text = layout.Name.FormatAsHtmlTitle();
            }

            SetEditMode( true );

            LoadDropDowns();

            tbLayoutName.Text = layout.Name;
            tbDescription.Text = layout.Description;
            ddlLayout.SetValue( layout.FileName );
        }

        /// <summary>
        /// Shows the readonly details.
        /// </summary>
        /// <param name="layout">The layout.</param>
        private void ShowReadonlyDetails( Rock.Model.Layout layout )
        {
            SetEditMode( false );

            hfLayoutId.SetValue( layout.Id );
            lReadOnlyTitle.Text = layout.Name.FormatAsHtmlTitle();

            lLayoutDescription.Text = layout.Description;

            DescriptionList descriptionList = new DescriptionList();
            descriptionList.Add( "Layout File", layout.FileName );
            lblMainDetails.Text = descriptionList.Html;
        }

        /// <summary>
        /// Sets the edit mode.
        /// </summary>
        /// <param name="editable">if set to <c>true</c> [editable].</param>
        private void SetEditMode( bool editable )
        {
            pnlEditDetails.Visible = editable;
            fieldsetViewDetails.Visible = !editable;

            this.HideSecondaryBlocks( editable );
        }

        /// <summary>
        /// Loads the drop downs.
        /// </summary>
        private void LoadDropDowns()
        {
            ddlLayout.Items.Clear();
            ddlLayout.Items.Add( new ListItem( string.Empty, None.IdValue ) );

            var site = SiteCache.Read( hfSiteId.ValueAsInt() );
            string virtualFolder = string.Format( "~/Themes/{0}/Layouts", site.Theme );
            string physicalFolder = Request.MapPath( virtualFolder );

            // search for all layouts (aspx files) under the physical path 
            var layoutFiles = new List<string>();
            DirectoryInfo di = new DirectoryInfo( physicalFolder );
            if ( di.Exists )
            {
                foreach ( var file in di.GetFiles( "*.aspx", SearchOption.AllDirectories ) )
                {
                    ddlLayout.Items.Add( new ListItem( file.FullName.Replace( physicalFolder, virtualFolder ).Replace( @"\", "/" ), Path.GetFileNameWithoutExtension( file.Name ) ) );
                }
            }

            ddlLayout.Required = true;
        }

        #endregion
    }
}