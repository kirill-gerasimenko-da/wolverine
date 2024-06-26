// <auto-generated/>
#pragma warning disable
using Microsoft.AspNetCore.Routing;
using System;
using System.Linq;
using Wolverine.Http;

namespace Internal.Generated.WolverineHandlers
{
    // START: POST_auditable_empty
    public class POST_auditable_empty : Wolverine.Http.HttpHandler
    {
        private readonly Wolverine.Http.WolverineHttpOptions _wolverineHttpOptions;

        public POST_auditable_empty(Wolverine.Http.WolverineHttpOptions wolverineHttpOptions) : base(wolverineHttpOptions)
        {
            _wolverineHttpOptions = wolverineHttpOptions;
        }

        public override async System.Threading.Tasks.Task Handle(Microsoft.AspNetCore.Http.HttpContext httpContext)
        {
            var auditableEndpoint = new WolverineWebApi.AuditableEndpoint();
            // Reading the request body via JSON deserialization
            var (command, jsonContinue) = await ReadJsonAsync<WolverineWebApi.AuditablePostBody>(httpContext);
            if (jsonContinue == Wolverine.HandlerContinuation.Stop) return;
            // Application-specific Open Telemetry auditing
            System.Diagnostics.Activity.Current?.SetTag("id", command.Id);
            
            // The actual HTTP request handler execution
            auditableEndpoint.EmptyPost(command);

            // Wolverine automatically sets the status code to 204 for empty responses
            if (!httpContext.Response.HasStarted) httpContext.Response.StatusCode = 204;
        }
    }

    // END: POST_auditable_empty
    
    
}