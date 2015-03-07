using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Web;

using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;
using Rock.Workflow;

namespace com.reallifeministries.RockExtensions.Workflow.Action
{
    /// <summary>
    /// Sends user to a specific page to continue the workflow 
    /// </summary>
    [Description( "Sends the user to a specific page to continue the workflow. It will send along WorkflowId and WorkflowTypeId as query params, as well as any other attributes you specify." )]
    [Export( typeof( ActionComponent ) )]
    [ExportMetadata( "ComponentName", "Go to Page" )]

    [LinkedPage("NextPage","A page to send the user to to continue the workflow",true)]
    [TextField("AttributesToSend","Attribute(s) that will be passed as URL parameters (Separate by comma)",false)]
    public class GoToPage : ActionComponent
    {
        /// <summary>
        /// Executes the specified workflow.
        /// </summary>
        /// <param name="rockContext">The rock context.</param>
        /// <param name="action">The action.</param>
        /// <param name="entity">The entity.</param>
        /// <param name="errorMessages">The error messages.</param>
        /// <returns></returns>
        public override bool Execute( RockContext rockContext, WorkflowAction action, Object entity, out List<string> errorMessages )
        {
            errorMessages = new List<string>();

            var queryParams = new Dictionary<string, string>();
            var linkedPage = GetAttributeValue( action, "NextPage" );
            
            queryParams.Add( "WorkflowTypeId", action.Activity.Workflow.WorkflowTypeId.ToString() );
            queryParams.Add( "WorkflowId", action.Activity.WorkflowId.ToString() );

            if (action.Activity.Attributes == null)
            {
                action.Activity.LoadAttributes( rockContext );
            }

            if (action.Activity.Workflow.Attributes == null)
            {
                action.Activity.Workflow.LoadAttributes( rockContext );
            }

            var attrsToSend = GetAttributeValue( action, "AttributesToSend" );
            if (!String.IsNullOrWhiteSpace( attrsToSend ))
            {
                foreach (var attr in attrsToSend.Split( ',' ))
                {
                    var attrName = attr.Trim();
                    if (!String.IsNullOrEmpty( action.Activity.GetAttributeValue( attrName ) ))
                    {
                        queryParams.Add( attrName, action.Activity.GetAttributeValue( attrName ) );
                    }
                    else if (!String.IsNullOrEmpty( action.Activity.Workflow.GetAttributeValue( attrName ) ))
                    {
                        queryParams.Add( attrName, action.Activity.Workflow.GetAttributeValue( attrName ) );
                    }
                }
            }

            var pageReference = new Rock.Web.PageReference( linkedPage, queryParams );

            var currentPage = (Rock.Web.UI.RockPage) HttpContext.Current.Handler;
            var currentUrl = currentPage.PageReference.BuildUrl();
            
            if (currentPage.PageId != pageReference.PageId)
            {
                string linkedPageUrl;
                if (pageReference.PageId > 0)
                {
                    linkedPageUrl = pageReference.BuildUrl();
                }
                else
                {
                    linkedPageUrl = string.Empty;
                }

                if (!string.IsNullOrWhiteSpace( linkedPageUrl ))
                {
                    HttpContext.Current.Response.Redirect( linkedPageUrl, true );

                    return true;
                }
                else
                {
                    errorMessages.Add( "Linked Page is not a valid Page" );
                    return false;
                }
            }
            else
            {
                return true;
            }
        }
    }
}
