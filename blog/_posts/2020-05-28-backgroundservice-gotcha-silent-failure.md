---
layout: post
title: "BackgroundService Gotcha: Silent Failures"
series: "BackgroundService Gotchas"
seriesTitle: "Silent Failures"
description: "BackgroundService will ignore exceptions thrown from ExecuteAsync."
---

## BackgroundService Gotcha: Silent Failures

I know [last time]({% post_url 2020-05-21-backgroundservice-gotcha-startup %}) I talked about `BackgroundService`... I don't want to make this a series or anything, but there is another common "gotcha" when it comes to `BackgroundService`: exceptions are silently ignored.

If the `ExecuteAsync` implementation throws an exception, that exception is silently swallowed and ignored. This is because `BackgroundService` captures the task from `ExecuteAsync` but never `await`s it - i.e., [`BackgroundService` uses fire-and-forget](https://github.com/dotnet/runtime/blob/e3ffd343ad5bd3a999cb9515f59e6e7a777b2c34/src/libraries/Microsoft.Extensions.Hosting.Abstractions/src/BackgroundService.cs).

## Problem Description

This problem will surface as `BackgroundService` instances just stopping, without any indication of a problem. What actually happens if `ExecuteAsync` throws an exception is that the exception is captured and placed on the `Task` that was returned from `ExecuteAsync`. The problem is that `BackgroundService` doesn't observe that task, so there's no logging and no process crash - the `BackgroundService` has completed executing but it just sits there doing nothing.

This is not *necessarily* a problem with `BackgroundService`; fire-and-forget can be appropriate for "top-level" loops such as a background worker task. However, it would be nice to have logging at least, so this "gotcha" is detectable.

## Solution

All top-level loops should have a `try`/`catch` with some kind of reporting if something goes wrong. `ExecuteAsync` implementations are top-level loops, so they should have a top-level `try` that catches all exceptions:

{% highlight csharp %}
public class MyBackgroundService : BackgroundService
{
    private readonly ILogger<MyBackgroundService> _logger;
    public MyBackgroundService(ILogger<MyBackgroundService> logger) => _logger = logger;

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            // Implementation
        }
        catch (Exception ex) when (False(() => _logger.LogCritical(ex, "Fatal error")))
        {
            throw;
        }
    }

    private static bool False(Action action) { action(); return false; }
}
{% endhighlight %}

I recommend you combine this solution with the solution from last time that [uses `Task.Run` to avoid startup problems]({% post_url 2020-05-21-backgroundservice-gotcha-startup %}):

{% highlight csharp %}
public class MyBackgroundService : BackgroundService
{
    private readonly ILogger<MyBackgroundService> _logger;
    public MyBackgroundService(ILogger<MyBackgroundService> logger) => _logger = logger;

    protected override Task ExecuteAsync(CancellationToken stoppingToken) => Task.Run(async () =>
    {
        try
        {
            // Implementation
        }
        catch (Exception ex) when (False(() => _logger.LogCritical(ex, "Fatal error")))
        {
            throw;
        }
    });

    private static bool False(Action action) { action(); return false; }
}
{% endhighlight %}
