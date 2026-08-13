using MediatR;
using Microsoft.Extensions.Logging;
using NSubstitute;
using RescuePC.Software.Logging.Behaviors;

namespace RescuePC.Software.Logging.UnitTests.Behaviors;

public class LoggingBehaviorTests
{
    private readonly ILogger<LoggingBehavior<TestRequest, TestResponse>> _logger;
    private readonly IRequestHandler<TestRequest, TestResponse> _handler;
    private readonly LoggingBehavior<TestRequest, TestResponse> _sut;

    public LoggingBehaviorTests()
    {
        _logger = Substitute.For<ILogger<LoggingBehavior<TestRequest, TestResponse>>>();
        _handler = Substitute.For<IRequestHandler<TestRequest, TestResponse>>();
        _sut = new LoggingBehavior<TestRequest, TestResponse>(_logger, _handler);
    }

    [Fact]
    public async Task Handle_WhenSucceeds_LogsHandlingAndCompleted()
    {
        var request = new TestRequest();
        var expected = new TestResponse();
        RequestHandlerDelegate<TestResponse> next = _ => Task.FromResult(expected);

        var result = await _sut.Handle(request, next, CancellationToken.None);

        Assert.Equal(expected, result);

        _logger.Received(1).Log(
            LogLevel.Information,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains("Handling")),
            null,
            Arg.Any<Func<object, Exception?, string>>());

        _logger.Received(1).Log(
            LogLevel.Information,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains("Completed")),
            null,
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public async Task Handle_WhenNextThrows_LogsHandlingAndError_AndRethrows()
    {
        var request = new TestRequest();
        var exception = new InvalidOperationException("boom");
        RequestHandlerDelegate<TestResponse> next = _ => Task.FromException<TestResponse>(exception);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _sut.Handle(request, next, CancellationToken.None));

        _logger.Received(1).Log(
            LogLevel.Information,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains("Handling")),
            null,
            Arg.Any<Func<object, Exception?, string>>());

        _logger.Received(1).Log(
            LogLevel.Error,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains("Failed")),
            exception,
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public async Task Handle_WhenCancelled_PropagatesCancellation()
    {
        var request = new TestRequest();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        RequestHandlerDelegate<TestResponse> next = ct =>
        {
            ct.ThrowIfCancellationRequested();
            return Task.FromResult(new TestResponse());
        };

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            _sut.Handle(request, next, cts.Token));
    }

    [Fact]
    public async Task Handle_UsesHandlerNameInLogs()
    {
        var request = new TestRequest();
        RequestHandlerDelegate<TestResponse> next = _ => Task.FromResult(new TestResponse());

        await _sut.Handle(request, next, CancellationToken.None);

        var handlerName = _handler.GetType().Name;

        _logger.Received().Log(
            LogLevel.Information,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains(handlerName)),
            null,
            Arg.Any<Func<object, Exception?, string>>());
    }

    public record TestRequest : IRequest<TestResponse>;
    public record TestResponse;
}
