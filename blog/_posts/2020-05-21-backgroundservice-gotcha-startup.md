---
layout: post
title: "BackgroundService Gotcha: Startup"
series: "BackgroundService Gotchas"
seriesTitle: "Synchronous Starts"
description: "BackgroundService (and IHostedService) are started synchronously."
---

## BackgroundService Gotcha: Synchronous Starts

This is some behavior that I've found surprising (and others have as well): ASP.NET Core background services are started with a synchronous call.

Specifically, the host will invoke `IHostedService.StartAsync` for all its hosted services, and `BackgroundService` [directly invokes `ExecuteAsync` before returning from `StartAsync`](https://github.com/dotnet/runtime/blob/e3ffd343ad5bd3a999cb9515f59e6e7a777b2c34/src/libraries/Microsoft.Extensions.Hosting.Abstractions/src/BackgroundService.cs#L37). So `BackgroundService` *assumes* that its derived classes will have an asynchronous `ExecuteAsync`. If the `ExecuteAsync` implementation is synchronous (or starts executing with a blocking call), then problems will ensue.

## Problem Description

The resulting behavior is that the background service will start executing, but the host will be unable to finish starting up. This will block other background services from starting.

Depending on the background service implementation, this may manifest as a delay of startup or a complete block of startup. If `ExecuteAsync` is synchronous, then the host cannot continue starting up until that background service has *completed*. If `ExecuteAsync` is asynchronous but takes a long time before it yields, then the host has its startup delayed.

## Common Scenarios

This problem is common in any of these conditions:

1. The hosted service has a synchronous `ExecuteAsync`. In this case, the host is prevented from starting until `ExecuteAsync` completes.
1. The hosted service reads from a queue to process messages, but the queue reading is blocking. Even if the processing is asynchronous, the host startup is blocked until the first message arrives for this service and is (asynchronously) processed.
1. The hosted service is properly asynchronous, but the asynchrony is completing immediately. E.g., if it is asynchronously reading from a queue but there are many messages immediately received, then the host startup is blocked until the background service actually yields.

## Solution

Since the problem is synchronous `ExecuteAsync` methods (or at least `ExecuteAsync` methods that do non-trivial work before they become asynchronous), the simplest solution is to ensure `ExecuteAsync` is asynchronous.

I'm [not a fan of using `Task.Run` to wrap the body of a method]({% post_url 2013-11-07-taskrun-etiquette-examples-dont-use %}) (i.e., "fake asynchrony"), but since the caller *requires* an asynchronous implementation, I think that's an acceptable approach in this case:

{% highlight csharp %}
public class MyBackgroundService : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken stoppingToken) => Task.Run(async () =>
    {
        // Implementation
    });
}
{% endhighlight %}

That way, any slow or blocking code early in `ExecuteAsync` will not prevent the host from starting up.

## Update (2020-05-22)

<div class="alert alert-info" markdown="1">
**Update (2020-05-22):** I've [been informed](https://twitter.com/klettier/status/1263727450502152194){:.alert-link} that the .NET Core team is [considering changing this behavior](https://github.com/dotnet/runtime/issues/36063#issuecomment-518913079){:.alert-link}.
</div>