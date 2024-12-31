// <auto-generated/>
#pragma warning disable
using Microsoft.Extensions.Logging;

namespace Internal.Generated.WolverineHandlers
{
    // START: NumberMessageHandler722393759
    public class NumberMessageHandler722393759 : Wolverine.Runtime.Handlers.MessageHandler
    {
        private readonly Microsoft.Extensions.Logging.ILogger<WolverineWebApi.NumberMessage> _loggerForMessage;

        public NumberMessageHandler722393759(Microsoft.Extensions.Logging.ILogger<WolverineWebApi.NumberMessage> loggerForMessage)
        {
            _loggerForMessage = loggerForMessage;
        }



        public override async System.Threading.Tasks.Task HandleAsync(Wolverine.Runtime.MessageContext context, System.Threading.CancellationToken cancellation)
        {
            // The actual message body
            var numberMessage = (WolverineWebApi.NumberMessage)context.Envelope.Message;

            var problemDetails = WolverineWebApi.NumberMessageHandler.Validate(numberMessage);
            // Evaluate whether the processing should stop if there are any problems
            if (!(ReferenceEquals(problemDetails, Wolverine.Http.WolverineContinue.NoProblems)))
            {
                Wolverine.Http.CodeGen.ProblemDetailsContinuationPolicy.WriteProblems(((Microsoft.Extensions.Logging.ILogger)_loggerForMessage), problemDetails);
                return;
            }


            
            // The actual message execution
            WolverineWebApi.NumberMessageHandler.Handle(numberMessage);

        }

    }

    // END: NumberMessageHandler722393759
    
    
}

