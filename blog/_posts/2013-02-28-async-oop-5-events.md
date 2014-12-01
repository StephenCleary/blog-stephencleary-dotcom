---
layout: post
title: "Async OOP 5: Events"
series: "Async OOP"
seriesTitle: "Events"
---
This post is going to deal with asynchronous event handlers. More specifically, how to design objects that will allow asynchronous event handlers.

For the vast majority of events, asynchronous event handlers work normally and we don't have to do anything special. Consider the classic `Button.Click` as a reasonable example: the producer of the event (`Button`) just raises the event normally, which starts calling the event subscribers one at a time. If there is an asynchronous handler, it will return early and the producer continues with the next event subscriber. Eventually, the asynchronous handler continues, usually returning to its original context, so it won't overlap the execution of any other event subscriber. In effect, asynchronous event handlers (usually) just get divided into a few actual events that each move the handler along a bit. This works perfectly well for the vast majority of events.

However, there's another class of events where this won't work. If the producer takes some special action when the event is completed, then the event is not really an _event_ - it's a _command_. For example, the `Application.Suspending` event in Windows Store applications is an event that is raised when the application is being suspended; when the event handler returns, Windows assumes that the application is done preparing to be suspended. An asynchronous handler for this kind of "command-style event" isn't going to work well: the handler will return early and Windows will suspend the application, preventing the rest of the handler from executing.

Other examples exist, too: ASP.NET has page lifecycle "events", and when those handlers return, the page moves to the next step in the lifecycle. Class designs that implement the visitor pattern can also suffer from early returns - and this is true whether the visitor type is implemented using actual events or virtual methods that act like events. Some types just use (misuse?) events as implementations, e.g., `BackgroundWorker.DoWork`. In each of these situations, asynchronous event handlers are not going to behave as expected.

Normal events are _notifications_ - the producer doesn't care whether there's a handler and there's no special semantic meaning to the completion of the event handlers. Normal events work fine if they're broadcasted, with no result returning to the producer. Other events are _commands_ - the producer takes some action when the event handlers have completed. Command events with asynchronous handlers need to report some kind of completion or result back to the producer so that it knows the event handler has completed.

## The WinRT Solution: Deferrals

With that background in mind, let's look at how WinRT solves this problem. For command-style events, the event producer must wait for a signal before considering the event handled. In the case of multiple asynchronous handlers, the producer must wait for _all_ handlers to signal before considering the event handled. Also, it would be nice to solve this problem in a way that adds no overhead for synchronous event handlers.

WinRT introduces the concept of a "deferral" for command-style events. An asynchronous handler acquires a deferral before its first `await`, and notifies that deferral when the handler is complete. Synchronous handlers do not need to acquire deferrals. WinRT does not provide a "deferral manager" type for implementing our own command-style events, so we'll have to make our own.

There's already a type in my [AsyncEx](http://nitoasyncex.codeplex.com) library with much of the functionality we need: [AsyncCountdownEvent](http://nitoasyncex.codeplex.com/wikipage?title=AsyncCountdownEvent). We increment the count whenever a handler acquires a deferral and signal the count whenever a deferral is completed. To prevent race conditions, we also need to increment the count before invoking the handlers and signal the count when all the handlers have returned.

There is one difference in the design of WinRT deferrals and my deferral manager: WinRT provides different interfaces and implementations for each different deferral scenario (i.e., `SuspendingDeferral` has no relation to `BackgroundTaskDeferral`). Since a "deferral" only has one possible action (`Complete`), and since this action should _always_ be done regardless of exceptions, I chose to represent deferrals as `IDisposable`. This allows asynchronous event handlers to use a `using` block in their implementation.

Compared to the design, the code is actually quite simple:

{% highlight csharp %}
public sealed class DeferralManager
{
  private readonly AsyncCountdownEvent _count = new AsyncCountdownEvent(1);

  public IDisposable GetDeferral()
  {
    return new Deferral(_count);
  }

  public Task SignalAndWaitAsync()
  {
    _count.Signal();
    return _count.WaitAsync();
  }

  private sealed class Deferral : IDisposable
  {
    private AsyncCountdownEvent _count;

    public Deferral(AsyncCountdownEvent count)
    {
      _count = count;
      _count.AddCount();
    }

    void IDisposable.Dispose()
    {
      if (_count == null)
        return;
      _count.Signal();
      _count = null;
    }
  }
}
{% endhighlight %}

<div class="alert alert-info" markdown="1">
<i class="fa fa-hand-o-right fa-2x pull-left"></i>

The [DeferralManager in the AsyncEx library](http://nitoasyncex.codeplex.com/wikipage?title=DeferralManager){:.alert-link} is almost identical to this code, except that it lazy-creates the asynchronous countdown event. This minimizes overhead if all the handlers are synchronous.
</div>

Once you have a `DeferralManager`, you can extend your "command-style" event arguments type as such:

{% highlight csharp %}
public class MyEventArgs : EventArgs
{
  private readonly DeferralManager deferrals = new DeferralManager();

  ... // Your own constructors and properties.

  public IDisposable GetDeferral()
  {
    return deferrals.GetDeferral();
  }

  internal Task WaitForDeferralsAsync()
  {
    return deferrals.SignalAndWaitAsync();
  }
}
{% endhighlight %}

Then, each time you raise the event, use code similar to this:

{% highlight csharp %}
private Task RaiseMyEventAsync()
{
  var handler = MyEvent;
  if (handler == null)
    return Task.FromResult<object>(null); // or TaskConstants.Completed

  var args = new MyEventArgs(...);
  handler(args);
  return args.WaitForDeferralsAsync();
}
{% endhighlight %}

One final note: ensure that your event arguments type (`MyEventArgs`) supports concurrent access. If there are multiple asynchronous handlers, they all will share a reference to the same event arguments instance. Ideally, all the properties on that type should be immutable.

## The Task-Returning Delegate Solution

I think deferrals are a perfectly acceptable solution. It's the solution that was chosen for WinRT, so it's one that programmers will be familiar with.

However, there's another solution that should work as well: make your event handler delegate type return `Task` instead of `void`. This is not as evil as it first appears; it's true that event handlers are supposed to return `void`, but `Task` is the `async` equivalent of `void`, so the event handler delegate type still _logically_ returns `void`, even though it doesn't _literally_ return `void`.

If you take this approach, you could use `Delegate.GetInvocationList` to invoke each handler individually and `Task.WhenAll` to detect when they have all completed.

The advantage of this approach is that there's no need for deferrals (or any explicit code) in the event handlers. The disadvantage is that _all_ event handlers are forced to have an asynchronous signature; this means synchronous event handlers would have to return `Task.FromResult<object>(null)` or some such. This is both awkward and inefficient.

## The SynchronizationContext Solution

OK, I suppose there is a third solution, if you _really_ want to go there.

Consider the example of ASP.NET page lifecycle events. With this example, it would have been a lot of work to add deferrals to the existing events, and that solution would probably bring up backwards-compatibility issues. So ASP.NET took a different approach: they integrated their events with the ASP.NET `SynchronizationContext`.

My [Feb 2011 MSDN article](http://msdn.microsoft.com/en-us/magazine/gg598924.aspx) describes how `async` methods interact with `SynchronizationContext`; at that time `async` was in a CTP stage, but that interaction did not change when `async` was released in VS2012. To summarize: `async void` methods call `SynchronizationContext.OperationStarted` at the beginning of the method and `SynchronizationContext.OperationCompleted` at the end of the method. The ASP.NET `SynchronizationContext` uses these methods to keep track of how many operations are currently in progress.

So, the ASP.NET page lifecycle events use this information: after raising each event, the ASP.NET runtime (asynchronously) waits until the operation count returns to its previous level. At that point, it knows that any `async` event handlers have run to completion.

But don't do that. Yes, it's cool, and very clever. But having your component override the `SynchronizationContext` for your event handlers is almost certainly a bad idea. It makes sense for ASP.NET because ASP.NET is a platform, not a library.

## You Should Use Deferrals

I recommend using deferrals. It's what programmers are expecting these days. I just mentioned these other solutions for the sake of completeness.

<div class="alert alert-info" markdown="1">
<i class="fa fa-hand-o-right fa-2x pull-left"></i>

Update (2014-12-01): For more details, see Recipe 10.5 in my [Concurrency Cookbook](http://tinyurl.com/ConcurrencyCookbook){:.alert-link}.
</div>
