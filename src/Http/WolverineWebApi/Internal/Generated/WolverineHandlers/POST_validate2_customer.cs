// <auto-generated/>
#pragma warning disable
using FluentValidation;
using Microsoft.AspNetCore.Routing;
using System;
using System.Linq;
using Wolverine.Http;
using Wolverine.Http.FluentValidation;

namespace Internal.Generated.WolverineHandlers
{
    // START: POST_validate2_customer
    public class POST_validate2_customer : Wolverine.Http.HttpHandler
    {
        private readonly Wolverine.Http.WolverineHttpOptions _wolverineHttpOptions;
        private readonly FluentValidation.IValidator<Wolverine.Http.Tests.DifferentAssembly.Validation.CreateCustomer2> _validator;
        private readonly Wolverine.Http.FluentValidation.IProblemDetailSource<Wolverine.Http.Tests.DifferentAssembly.Validation.CreateCustomer2> _problemDetailSource;

        public POST_validate2_customer(Wolverine.Http.WolverineHttpOptions wolverineHttpOptions, FluentValidation.IValidator<Wolverine.Http.Tests.DifferentAssembly.Validation.CreateCustomer2> validator, Wolverine.Http.FluentValidation.IProblemDetailSource<Wolverine.Http.Tests.DifferentAssembly.Validation.CreateCustomer2> problemDetailSource) : base(wolverineHttpOptions)
        {
            _wolverineHttpOptions = wolverineHttpOptions;
            _validator = validator;
            _problemDetailSource = problemDetailSource;
        }



        public override async System.Threading.Tasks.Task Handle(Microsoft.AspNetCore.Http.HttpContext httpContext)
        {
            // Reading the request body via JSON deserialization
            var (customer, jsonContinue) = await ReadJsonAsync<Wolverine.Http.Tests.DifferentAssembly.Validation.CreateCustomer2>(httpContext);
            if (jsonContinue == Wolverine.HandlerContinuation.Stop) return;
            
            // Execute FluentValidation validators
            var result1 = await Wolverine.Http.FluentValidation.Internals.FluentValidationHttpExecutor.ExecuteOne<Wolverine.Http.Tests.DifferentAssembly.Validation.CreateCustomer2>(_validator, _problemDetailSource, customer).ConfigureAwait(false);

            // Evaluate whether or not the execution should be stopped based on the IResult value
            if (result1 != null && !(result1 is Wolverine.Http.WolverineContinue))
            {
                await result1.ExecuteAsync(httpContext).ConfigureAwait(false);
                return;
            }


            
            // The actual HTTP request handler execution
            var result_of_Post = Wolverine.Http.Tests.DifferentAssembly.Validation.Validated2Endpoint.Post(customer);

            await WriteString(httpContext, result_of_Post);
        }

    }

    // END: POST_validate2_customer
    
    
}

