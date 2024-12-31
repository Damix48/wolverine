// <auto-generated/>
#pragma warning disable
using Microsoft.AspNetCore.Routing;
using System;
using System.Linq;
using Wolverine.Http;

namespace Internal.Generated.WolverineHandlers
{
    // START: GET_enum_direction
    public class GET_enum_direction : Wolverine.Http.HttpHandler
    {
        private readonly Wolverine.Http.WolverineHttpOptions _wolverineHttpOptions;

        public GET_enum_direction(Wolverine.Http.WolverineHttpOptions wolverineHttpOptions) : base(wolverineHttpOptions)
        {
            _wolverineHttpOptions = wolverineHttpOptions;
        }



        public override async System.Threading.Tasks.Task Handle(Microsoft.AspNetCore.Http.HttpContext httpContext)
        {
            if (!WolverineWebApi.Direction.TryParse<WolverineWebApi.Direction>((string)httpContext.GetRouteValue("direction"), true, out WolverineWebApi.Direction direction))
            {
                httpContext.Response.StatusCode = 404;
                return;
            }


            var fakeEndpoint = new WolverineWebApi.FakeEndpoint();
            
            // The actual HTTP request handler execution
            var result_of_ReadEnumArgument = fakeEndpoint.ReadEnumArgument(direction);

            await WriteString(httpContext, result_of_ReadEnumArgument);
        }

    }

    // END: GET_enum_direction
    
    
}

