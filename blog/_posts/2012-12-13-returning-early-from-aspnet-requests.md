---
layout: post
title: "Returning Early from ASP.NET Requests"
---

<div class="alert alert-info" markdown="1">
<i class="fa fa-hand-o-right fa-2x pull-left"></i>

**Update, 2021-02-22:** There is more information in [a whole series of posts on Asynchronous Messaging]({% post_url 2021-01-07-asynchronous-messaging-1-basic-distributed-architecture %}){:.alert-link}, which is the *proper* solution for request-extrinsic code.
</div>

I have great reservations about writing this blog post. Pretty much everything I'm going to describe here is a bad idea and you should strongly avoid putting it into production, but there _are_ just a few situations where this technique can be really helpful.

As I described in [Async Doesn't Change the HTTP Protocol]({% post_url 2012-08-09-async-doesnt-change-http-protocol %}), in the ASP.NET worldview you only get one "response" for each "request". You can't return early just by using an `await`. However, in some situations you have enough information to generate the response but the actual request processing may take some more time. That's were today's solution comes in.

## Not Recommended

The solution in this blog post is not recommended. Before putting it into production, you **need** to understand **why** it's not recommended.

ASP.NET executes your web site (or web application) in an AppDomain, separated from other web sites (or web applications) on the same server. There are [many](https://docs.microsoft.com/en-us/archive/blogs/tess/asp-net-case-study-lost-session-variables-and-appdomain-recycles) [reasons](https://docs.microsoft.com/en-us/archive/blogs/johan/common-reasons-why-your-application-pool-may-unexpectedly-recycle) why this AppDomain may be shut down; modern versions of IIS [recycle the entire process](http://www.iis.net/configreference/system.applicationhost/applicationpools/add/recycling) every 29 hours by default just to keep things clean. Also, you have to take into consideration unmanaged shutdowns: hard drive failures, hurricanes, etc.

Consider what happens if you generate (and return) the response but you're still working on the request. If you lose your AppDomain for any reason, that in-progress work is _lost_. The client thinks it was completed, but it really wasn't. As long as the request is incomplete, the responsibility is on the client. When you complete the request (by sending a response), you have accepted the full responsibility of that request. If you haven't already committed the changes, you need to be absolutely sure that they _will_ be committed.

## Proper Solutions

The correct solutions are all complicated: you need to put the additional work in a safe place, like an Azure queue, database, or persistent messaging system (Azure message bus, MSMQ, WebSphere MQ, etc). And each of those solutions brings a whole scope of additional work: setup and configuration, dead-letter queues, poison messages, etc.

But that's the correct way to do it, because _you can't drop the ball!_ You store the additional work in the safe place and then return a response after the work is safely stored. Personally, I like distributed systems (like Azure queues) because it's not just safely stored on the hard drive - it's safely stored on _six_ hard drives, three of which are in a _different geographic location._ This gives you more protection from more problems (like hard drive failures and hurricanes).

<div class="alert alert-info" markdown="1">
<i class="fa fa-hand-o-right fa-2x pull-left"></i>

**Update, 2021-02-22:** I have [a whole series of posts on asynchronous messaging]({% post_url 2021-01-07-asynchronous-messaging-1-basic-distributed-architecture %}){:.alert-link}, which is the proper solution for request-extrinsic code.
</div>

## The Improper "Solution"

The _unsafe_ way to do it is to keep the work in memory. The simple way to do this is to just toss the work into `Task.Run`. Unfortunately, ASP.NET has no idea if you have queued work like this, and it will feel free to take down your AppDomain when it thinks it's idle.

The _slightly safer but still unsafe_ way to do it is to keep the work in memory but register it with ASP.NET so that it will notify you when your AppDomain is going away. The code in this blog post uses [the technique described by Phil Haack](http://haacked.com/archive/2011/10/16/the-dangers-of-implementing-recurring-background-tasks-in-asp-net.aspx) to register work with the ASP.NET runtime. It's important to note the **limitations** of this approach:

- <s>By default, you only have 30 seconds total from the time the notification goes out to the time the AppDomain is yanked out from under you.</s> As noted in the comments, the ASP.NET runtime will wait an arbitrary amount of time for your background tasks to complete. Still, it's probably best not to keep them waiting...
- You _may_ not get notification at all. In an unmanaged shutdown (e.g., power loss), all bets are off.

Still, this approach _can_ be useful in a limited set of scenarios, so with great reservation let's take a look at the code.

<div class="alert alert-info" markdown="1">
<i class="fa fa-hand-o-right fa-2x pull-left"></i>

**Update 2014-04:** A newer version of this code is now [on GitHub](https://github.com/StephenCleary/AspNetBackgroundTasks){:.alert-link} and available [via NuGet](https://www.nuget.org/packages/Nito.AspNetBackgroundTasks/){:.alert-link}.
</div>

{% highlight csharp %}
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Hosting;
using Nito.AsyncEx;

/// <summary>
/// A type that tracks background operations and notifies ASP.NET that they are still in progress.
/// </summary>
public sealed class BackgroundTaskManager : IRegisteredObject
{
    /// <summary>
    /// A cancellation token that is set when ASP.NET is shutting down the app domain.
    /// </summary>
    private readonly CancellationTokenSource shutdown;

    /// <summary>
    /// A countdown event that is incremented each time a task is registered and decremented each time it completes. When it reaches zero, we are ready to shut down the app domain. 
    /// </summary>
    private readonly AsyncCountdownEvent count;

    /// <summary>
    /// A task that completes after <see cref="count"/> reaches zero and the object has been unregistered.
    /// </summary>
    private readonly Task done;

    private BackgroundTaskManager()
    {
        // Start the count at 1 and decrement it when ASP.NET notifies us we're shutting down.
        shutdown = new CancellationTokenSource();
        count = new AsyncCountdownEvent(1);
        shutdown.Token.Register(() => count.Signal(), useSynchronizationContext: false);

        // Register the object and unregister it when the count reaches zero.
        HostingEnvironment.RegisterObject(this);
        done = count.WaitAsync().ContinueWith(_ => HostingEnvironment.UnregisterObject(this), TaskContinuationOptions.ExecuteSynchronously);
    }

    void IRegisteredObject.Stop(bool immediate)
    {
        shutdown.Cancel();
        if (immediate)
            done.Wait();
    }

    /// <summary>
    /// Registers a task with the ASP.NET runtime.
    /// </summary>
    /// <param name="task">The task to register.</param>
    private void Register(Task task)
    {
        count.AddCount();
        task.ContinueWith(_ => count.Signal(), TaskContinuationOptions.ExecuteSynchronously);
    }

    /// <summary>
    /// The background task manager for this app domain.
    /// </summary>
    private static readonly BackgroundTaskManager instance = new BackgroundTaskManager();

    /// <summary>
    /// Gets a cancellation token that is set when ASP.NET is shutting down the app domain.
    /// </summary>
    public static CancellationToken Shutdown { get { return instance.shutdown.Token; } }
    
    /// <summary>
    /// Executes an <c>async</c> background operation, registering it with ASP.NET.
    /// </summary>
    /// <param name="operation">The background operation.</param>
    public static void Run(Func<Task> operation)
    {
        instance.Register(Task.Run(operation));
    }

    /// <summary>
    /// Executes a background operation, registering it with ASP.NET.
    /// </summary>
    /// <param name="operation">The background operation.</param>
    public static void Run(Action operation)
    {
        instance.Register(Task.Run(operation));
    }
}
{% endhighlight %}

`BackgroundTaskManager` is a singleton that keeps track of background operations. It uses an [AsyncCountdownEvent](http://nitoasyncex.codeplex.com/wikipage?title=AsyncCountdownEvent) from [AsyncEx](http://nitoasyncex.codeplex.com/) as a counter of background operations (plus an extra count that is decremented when ASP.NET notifies us that the AppDomain is going down).

You can queue synchronous or asynchronous work by calling `Run`:

{% highlight csharp %}
BackgroundTaskManager.Run(() =>
{
    Thread.Sleep(20000);
});
BackgroundTaskManager.Run(async () =>
{
    await Task.Delay(20000);
});
{% endhighlight %}

`BackgroundTaskManager` also publishes a `CancellationToken` that is canceled when ASP.NET notifies us that our AppDomain is shutting down. `async` code can use this to abort processing (when it is safe to do so):

{% highlight csharp %}
BackgroundTaskManager.Run(async () =>
{
    await Task.Delay(20000, BackgroundTaskManager.Shutdown);
});
{% endhighlight %}

One important note about background operations: **exceptions are ignored!** So if you want to catch errors and toss a "hail Mary" to ETW or the Event Log, you'll need to do so with a `try ... catch` inside each operation. In the example above, if the AppDomain is recycled while the operation is doing the delay, the cancellation exception will be raised from the operation and then it will be ignored.

As a final reminder, do **not** put critical processing in a background operation like this. It works fine for the "easy" case (ASP.NET gets a gentle request to shut down the AppDomain and nicely notifies the background operations, which all complete or cancel well within the timeout window), but it can fall down if anything goes wrong (IIS is killed, or the background operations continue too long due to another process hogging the CPU, or there's a power outage, etc).

