---
layout: post
title: "BackgroundService Gotcha: Application Lifetime"
series: "BackgroundService Gotchas"
seriesTitle: "Application Lifetime"
description: "BackgroundService (and IHostedService) do not shut down the application when they complete."
---

## BackgroundService Gotcha: Service Lifetime is Independent from Application Lifetime

OK, I guess I *do* have a series on my hands.

This one isn't so much a "gotcha" as it is a common mistake. Hosted services (including `BackgroundService`s) have a lifetime that is *independent* from their host. This means that if a `BackgroundService` exits, its host will not exit. And a host will happily continue running even if *all* its services have exited.

This mistake is most common when writing a kind of "subscriber" or "backend processor" application - something that listens to a queue and processes messages on that queue. For these kinds of applications, it's common to have a .NET Core host with only a single hosted service, and the service has a loop which processes messages from the queue. This is essentially the "ASP.NET Core Worker Process" template project in Visual Studio. When writing this kind of application, it's easy to forget about the host, and treat the hosted service *as* the application. But when there are no more messages (or some other shutdown signal is delivered to the hosted service) and the service exits, then the host application stays around.

## Solution

This behavior is by design. If a `BackgroundService` implementation wants the application to exit when it stops (or [has a fatal error]({% post_url 2020-05-28-backgroundservice-gotcha-silent-failure %})), it needs to do that itself by injecting an `IHostApplicationLifetime` and calling `StopApplication`:

{% highlight csharp %}
public class MyBackgroundService : BackgroundService
{
    private readonly IHostApplicationLifetime _hostApplicationLifetime;
    public MyBackgroundService(IHostApplicationLifetime hostApplicationLifetime) =>
        _hostApplicationLifetime = hostApplicationLifetime;

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            // Implementation
        }
        finally
        {
            _hostApplicationLifetime.StopApplication();
        }
    }
}
{% endhighlight %}

I recommend also [using `Task.Run` to avoid startup problems]({% post_url 2020-05-21-backgroundservice-gotcha-startup %}) and [logging all exceptions in this top-level loop]({% post_url 2020-05-28-backgroundservice-gotcha-silent-failure %}):

{% highlight csharp %}
public class MyBackgroundService : BackgroundService
{
    private readonly IHostApplicationLifetime _hostApplicationLifetime;
    private readonly ILogger<MyBackgroundService> _logger;
    public MyBackgroundService(IHostApplicationLifetime hostApplicationLifetime, ILogger<MyBackgroundService> logger)
    {
        _hostApplicationLifetime = hostApplicationLifetime;
        _logger = logger;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken) => Task.Run(async () =>
    {
        try
        {
            // Implementation
        }
        catch (Exception ex) when False(() => _logger.LogCritical(ex, "Fatal error"))
        {
            throw;
        }
        finally
        {
            _hostApplicationLifetime.StopApplication();
        }
    });

    private static bool False(Action action) { action(); return false; }
}
{% endhighlight %}

At this point, this is a fair amount of boilerplate code to go into each background service. Production code bases should probably define a "critical background service" base type that handles all the wrapper code so background services only need to define the `// Implementation` part.