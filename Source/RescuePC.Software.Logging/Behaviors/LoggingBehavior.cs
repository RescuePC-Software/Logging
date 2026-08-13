using System.Diagnostics;
using Microsoft.Extensions.Logging;
using MediatR;

namespace RescuePC.Software.Logging.Behaviors;

public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;
    private readonly IRequestHandler<TRequest, TResponse> _handler;

    public LoggingBehavior(
        ILogger<LoggingBehavior<TRequest, TResponse>> logger,
        IRequestHandler<TRequest, TResponse> handler)
    {
        _logger = logger;
        _handler = handler;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var handlerName = _handler.GetType().Name;

        _logger.LogInformation("[{HandlerName}]: Handling", handlerName);

        var stopwatch = Stopwatch.StartNew();
        try
        {
            var response = await next(cancellationToken);
            stopwatch.Stop();

            _logger.LogInformation("[{HandlerName}]: Completed in {ElapsedMs}ms", handlerName, stopwatch.ElapsedMilliseconds);

            return response;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "[{HandlerName}]: Failed after {ElapsedMs}ms", handlerName, stopwatch.ElapsedMilliseconds);

            throw;
        }
    }
}
