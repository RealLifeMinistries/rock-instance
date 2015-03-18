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
    [Description( "Sets up the activity entry form page (to be uses with the corresponding block" )]
    [Export( typeof( ActionComponent ) )]
    [ExportMetadata( "ComponentName", "SetActivityEntryPage" )]

    [LinkedPage("EntryFormPage","A page to send the user to to continue the workflow",true)]
    [BooleanField("Redirect", "Redirect user to entry page now", true)]
    public class SetActivityEntryPage : ActionComponent
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
            
            var AttrKey = "EntryFormPage";

            var entryPage = GetAttributeValue( action, AttrKey );

            if (!action.Activity.Attributes.ContainsKey( AttrKey ))
            {
                // If activity attribute doesn't exist, create it 
                // ( should only happen on first workflow using this action for the current activity)
                var attribute = new Rock.Model.Attribute();
                attribute.EntityTypeId = action.Activity.TypeId;
                attribute.EntityTypeQualifierColumn = "ActivityTypeId";
                attribute.EntityTypeQualifierValue = action.Activity.ActivityTypeId.ToString();
                attribute.Name = AttrKey;
                attribute.Key = AttrKey;
                attribute.FieldTypeId = FieldTypeCache.Read( Rock.SystemGuid.FieldType.PAGE_REFERENCE.AsGuid() ).Id;

                // Set the value for this action's instance to the current time
                var attributeValue = new Rock.Model.AttributeValue();
                attributeValue.Attribute = attribute;
                attributeValue.EntityId = action.Activity.Id;
                attributeValue.Value = entryPage;
                new AttributeValueService( rockContext ).Add( attributeValue );

                action.AddLogEntry( string.Format( "Attribute ({0}) added to Activity with value: {1}", AttrKey, entryPage), true );
            }
            else
            {
                action.Activity.SetAttributeValue( AttrKey, entryPage );
                action.AddLogEntry( string.Format( "Attribute ({0}) set on Activity with value: {1}", AttrKey, entryPage ), true );
            }

            var doRedirect = GetAttributeValue( action, "Redirect" ).AsBoolean();

            if (doRedirect)
            {
                if (HttpContext.Current != null)
                {
                    var queryParams = new Dictionary<string, string>();
                    queryParams.Add( "WorkflowTypeId", action.Activity.Workflow.WorkflowTypeId.ToString() );
                    queryParams.Add( "WorkflowId", action.Activity.WorkflowId.ToString() );

                    var pageReference = new Rock.Web.PageReference( entryPage, queryParams );

                    HttpContext.Current.Response.Redirect(pageReference.BuildUrl(),false);
                }
            }

            return true;
        }
    }
}
