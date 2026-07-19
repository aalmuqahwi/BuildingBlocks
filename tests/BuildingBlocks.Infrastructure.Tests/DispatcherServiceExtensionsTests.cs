using BuildingBlocks.Application;
using BuildingBlocks.Domain;

using Microsoft.Extensions.DependencyInjection;

namespace BuildingBlocks.Infrastructure.Tests;

#pragma warning disable CS1591
public record CommandA : ICommand;
public record CommandB : ICommand;
public record CommandC : ICommand;
public record CommandWithNoHandler : ICommand;

public class CommandAHandler : ICommandHandler<CommandA>
{
    public Task<Result> Handle(CommandA command, CancellationToken ct)
        => Task.FromResult(Result.Failure(new Error("A", "Handled by A")));
}

public class CommandBHandler : ICommandHandler<CommandB>
{
    public Task<Result> Handle(CommandB command, CancellationToken ct)
        => Task.FromResult(Result.Failure(new Error("B", "Handled by B")));
}

public class CommandCHandler : ICommandHandler<CommandC>
{
    private readonly List<string> _log;

    public CommandCHandler(List<string> log) => _log = log;

    public Task<Result> Handle(CommandC command, CancellationToken ct)
    {
        _log.Add("Handler - invoked");
        return Task.FromResult(Result.Success());
    }
}

public class TestBehavior : IPipelineBehavior<ICommand<Result>, Result>
{
    private readonly List<string> _log;
    private readonly string _name;

    public TestBehavior(List<string> log, string name)
    {
        _log = log;
        _name = name;
    }

    public async Task<Result> Handle(ICommand<Result> request, RequestHandlerDelegate<Result> next, CancellationToken ct)
    {
        _log.Add($"{_name} - before");
        var result = await next();
        _log.Add($"{_name} - after");
        return result;
    }
}

public class ShortCircuitingBehavior : IPipelineBehavior<ICommand<Result>, Result>
{
    private readonly List<string> _log;

    public ShortCircuitingBehavior(List<string> log) => _log = log;

    public Task<Result> Handle(ICommand<Result> request, RequestHandlerDelegate<Result> next, CancellationToken ct)
    {
        _log.Add("Behavior - short circuited");
        return Task.FromResult(Result.Failure(new Error("Blocked", "Stopped by behavior")));
    }
}

public class DispatcherServiceExtensionsTests
{
    [Fact]
    public async Task AddDispatcher_should_key_wrappers_by_command_type_not_result_type()
    {
        var services = new ServiceCollection();
        services.AddDispatcher(typeof(DispatcherServiceExtensionsTests).Assembly);
        var provider = services.BuildServiceProvider();

        var dispatcher = provider.GetRequiredService<IDispatcher>();

        var resultA = await dispatcher.SendAsync(new CommandA());
        var resultB = await dispatcher.SendAsync(new CommandB());

        Assert.Equal("A", resultA.Error.Code);
        Assert.Equal("B", resultB.Error.Code);
    }

    [Fact]
    public async Task AddDispatcher_should_invoke_handler_directly_when_no_behaviors_registered()
    {
        var services = new ServiceCollection();
        services.AddDispatcher(typeof(DispatcherServiceExtensionsTests).Assembly);
        var provider = services.BuildServiceProvider();

        var dispatcher = provider.GetRequiredService<IDispatcher>();

        var result = await dispatcher.SendAsync(new CommandA());

        Assert.Equal("A", result.Error.Code);
    }

    [Fact]
    public async Task Dispatcher_should_run_behaviors_in_registration_order()
    {
        var log = new List<string>();
        var services = new ServiceCollection();
        services.AddDispatcher(typeof(DispatcherServiceExtensionsTests).Assembly);
        services.AddSingleton<IPipelineBehavior<ICommand<Result>, Result>>(new TestBehavior(log, "First"));
        services.AddSingleton<IPipelineBehavior<ICommand<Result>, Result>>(new TestBehavior(log, "Second"));
        var provider = services.BuildServiceProvider();
        var dispatcher = provider.GetRequiredService<IDispatcher>();

        await dispatcher.SendAsync(new CommandA());

        Assert.Equal(
            new[] { "First - before", "Second - before", "Second - after", "First - after" },
            log);
    }

    [Fact]
    public async Task Dispatcher_should_not_invoke_handler_when_behavior_short_circuits()
    {
        var log = new List<string>();

        var services = new ServiceCollection();
        services.AddSingleton(log);
        services.AddDispatcher(typeof(DispatcherServiceExtensionsTests).Assembly);
        services.AddSingleton<IPipelineBehavior<ICommand<Result>, Result>>(new ShortCircuitingBehavior(log));

        var provider = services.BuildServiceProvider();
        var dispatcher = provider.GetRequiredService<IDispatcher>();

        var result = await dispatcher.SendAsync(new CommandC());

        Assert.Equal("Blocked", result.Error.Code);
        Assert.DoesNotContain("Handler - invoked", log);
    }

    [Fact]
    public async Task Dispatcher_should_throw_when_no_handler_is_registered()
    {
        var services = new ServiceCollection();
        services.AddDispatcher(typeof(DispatcherServiceExtensionsTests).Assembly);
        var provider = services.BuildServiceProvider();

        var dispatcher = provider.GetRequiredService<IDispatcher>();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => dispatcher.SendAsync(new CommandWithNoHandler()));
    }
}


#pragma warning restore CS1591