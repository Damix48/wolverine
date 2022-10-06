using System;
using System.Threading;
using System.Threading.Tasks;
using Baseline.Dates;
using Wolverine.ErrorHandling;
using Wolverine.Logging;

namespace Wolverine.Runtime.Handlers;

internal enum InvokeResult
{
    Success,
    TryAgain
}

internal interface IExecutor
{
    Task<IContinuation> ExecuteAsync(MessageContext context, CancellationToken cancellation);
    Task<InvokeResult> InvokeAsync(MessageContext context, CancellationToken cancellation);
}

internal class Executor : IExecutor
{
    private readonly IMessageHandler _handler;
    private readonly IMessageLogger _logger;
    private readonly FailureRuleCollection _rules;
    private readonly TimeSpan _timeout;

    public Executor(IWolverineRuntime runtime, IMessageHandler handler, FailureRuleCollection rules, TimeSpan timeout)
    {
        _handler = handler;
        _timeout = timeout;
        _rules = rules;
        _logger = runtime.MessageLogger;
    }

    public Executor(IMessageHandler handler, IMessageLogger logger, FailureRuleCollection rules, TimeSpan timeout)
    {
        _handler = handler;
        _logger = logger;
        _rules = rules;
        _timeout = timeout;
    }

    internal Executor WrapWithMessageTracking(IMessageSuccessTracker tracker)
    {
        return new Executor(new CircuitBreakerWrappedMessageHandler(_handler, tracker), _logger, _rules, _timeout);
    }

    public async Task<IContinuation> ExecuteAsync(MessageContext context, CancellationToken cancellation)
    {
        context.Envelope!.Attempts++;

        using var timeout = new CancellationTokenSource(_timeout);
        using var combined = CancellationTokenSource.CreateLinkedTokenSource(timeout.Token, cancellation);

        try
        {
            await _handler.HandleAsync(context, combined.Token);
            return MessageSucceededContinuation.Instance;
        }
        catch (Exception e)
        {
            _logger.LogException(e, context.Envelope!.Id, "Failure during message processing execution");
            _logger
                .ExecutionFinished(context.Envelope); // Need to do this to make the MessageHistory complete

            await context.ClearAllAsync();

            return _rules.DetermineExecutionContinuation(e, context.Envelope);
        }
    }


    public async Task<InvokeResult> InvokeAsync(MessageContext context, CancellationToken cancellation)
    {
        if (context.Envelope == null)
        {
            throw new ArgumentOutOfRangeException(nameof(context.Envelope));
        }

        try
        {
            await _handler.HandleAsync(context, cancellation);
            return InvokeResult.Success;
        }
        catch (Exception e)
        {
            _logger.LogException(e, message: $"Invocation of {context.Envelope} failed!");

            var retry = _rules.TryFindInlineContinuation(e, context.Envelope);
            if (retry == null)
            {
                throw;
            }

            if (retry.Delay.HasValue)
            {
                await Task.Delay(retry.Delay.Value, cancellation).ConfigureAwait(false);
            }

            return InvokeResult.TryAgain;
        }
    }

    public static IExecutor Build(IWolverineRuntime runtime, HandlerGraph handlerGraph, Type messageType)
    {
        var handler = handlerGraph.HandlerFor(messageType);
        if (handler == null)
        {
            return new NoHandlerExecutor(messageType, (WolverineRuntime)runtime);
        }

        var timeoutSpan = handler.Chain?.DetermineMessageTimeout(runtime.Options) ?? 5.Seconds();
        var rules = handler.Chain?.Failures.CombineRules(handlerGraph.Failures) ?? handlerGraph.Failures;
        return new Executor(runtime, handler, rules, timeoutSpan);
    }
}
