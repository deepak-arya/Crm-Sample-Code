﻿using System;
using System.Activities;
using System.ServiceModel;
using System.Globalization;
using System.Runtime.Serialization;
using System.IO;
using System.Text;
using System.Net;
using Microsoft.Xrm.Sdk;
using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Workflow;
using Microsoft.Xrm.Sdk.Query;
using System.Text.RegularExpressions;
using System.Linq;

namespace KeyValueManager
{
    public class GetKeyValue : CodeActivity
    {
        [Input("Key name")]
        public InArgument<string> KeyName { get; set; }

        [Output("String value")]
        public OutArgument<string> StringValue { get; set; }

        [Output("Integer value")]
        public OutArgument<Int32> IntegerValue { get; set; }

        [Output("Decimal value")]
        public OutArgument<decimal> DecimalValue { get; set; }

        [Output("Date value")]
        public OutArgument<DateTime> DateValue { get; set; }

        [Output("Date/time value")]
        public OutArgument<DateTime> DateTimeValue { get; set; }

        private string _processName = "GetKeyValue";
        /// <summary>
        /// Executes the workflow activity.
        /// </summary>
        /// <param name="executionContext">The execution context.</param>
        protected override void Execute(CodeActivityContext executionContext)
        {
            // Create the tracing service
            ITracingService tracingService = executionContext.GetExtension<ITracingService>();

            if (tracingService == null)
            {
                throw new InvalidPluginExecutionException("Failed to retrieve tracing service.");
            }

            tracingService.Trace("Entered " + _processName + ".Execute(), Activity Instance Id: {0}, Workflow Instance Id: {1}",
                executionContext.ActivityInstanceId,
                executionContext.WorkflowInstanceId);

            // Create the context
            IWorkflowContext context = executionContext.GetExtension<IWorkflowContext>();

            if (context == null)
            {
                throw new InvalidPluginExecutionException("Failed to retrieve workflow context.");
            }

            tracingService.Trace(_processName + ".Execute(), Correlation Id: {0}, Initiating User: {1}",
                context.CorrelationId,
                context.InitiatingUserId);

            IOrganizationServiceFactory serviceFactory = executionContext.GetExtension<IOrganizationServiceFactory>();
            IOrganizationService service = serviceFactory.CreateOrganizationService(context.UserId);

            try
            {

                string keyName = KeyName.Get(executionContext);

                //look up team template by name
                QueryByAttribute querybyexpression = new QueryByAttribute("new_keyvaluepair");
                querybyexpression.ColumnSet = new ColumnSet("new_name",
                    "new_stringvalue",
                    "new_decimalvalue",
                    "new_integervalue",
                    "new_datevalue",
                    "new_dateandtimevalue",
                    "new_valuetype"
                    );
                querybyexpression.Attributes.AddRange("new_name");
                querybyexpression.Values.AddRange(keyName);
                EntityCollection retrieved = service.RetrieveMultiple(querybyexpression);

                //if we find something, we're set
                if (retrieved.Entities.Count > 0)
                {
                    Entity kvp = retrieved.Entities[0];
                    StringValue.Set(executionContext, (string)kvp["new_stringvalue"]);
                    IntegerValue.Set(executionContext, (int)kvp["new_integervalue"]);
                    DecimalValue.Set(executionContext, (decimal)kvp["new_decimalvalue"]);
                    DateValue.Set(executionContext, (DateTime)kvp["new_datevalue"]);
                    DateTimeValue.Set(executionContext, (DateTime)kvp["new_dateandtimevalue"]);
                }
                else
                {
                    //throw exception if unable to find a matching template
                    throw new Exception("could not find key-value pair for: " + keyName);
                }
            }
            catch (FaultException<OrganizationServiceFault> e)
            {
                tracingService.Trace("Exception: {0}", e.ToString());

                // Handle the exception.
                throw;
            }
            catch (Exception e)
            {
                tracingService.Trace("Exception: {0}", e.ToString());
                throw;
            }

            tracingService.Trace("Exiting " + _processName + ".Execute(), Correlation Id: {0}", context.CorrelationId);
        }
    }

}