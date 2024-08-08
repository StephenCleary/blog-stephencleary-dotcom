---
layout: post
title: "Cancellation, Part 5: Registration"
series: "Cancellation"
seriesTitle: "Registration"
description: "Responding to cancellation requests by using registration."
---

Last time in this series I talked about [how to respond to cancellation requests by polling for them]({% post_url 2022-03-17-cancellation-4-polling %}). That's a common approach for synchronous or CPU-bound code. In this post, I'm covering a pattern more common for asynchronous code: registration.

Registration is a way for your code to get a callback immediately when cancellation is requested. This callback can then perform some operation (often calling a different API) to cancel the asynchronous operation. Due to the multiple ways callbacks may be invoked, it's generally recommended that cancellation callbacks should not throw exceptions.

<div class="alert alert-danger" markdown="1">
<i class="fa fa-exclamation-triangle fa-2x pull-left"></i>

Your callback should not throw exceptions.
</div>

## How to Register

Your code can register a callback with any `CancellationToken` by calling `CancellationToken.Register`. This callback is invoked when (if) the cancellation token is cancelled. The `Register` method returns a cancellation token registration, which is essentially just an `IDisposable`.

Pretty much every asynchronous API _already has_ cancellation support, so the example code below is somewhat contrived. The API in this example code provides a `StartSomething()` method to start some asynchronous operation, a `StopSomething()` method to cancel that operation, and a `SomethingCompletedTask` property to detect when the operation completed. There are very few APIs like this in the .NET ecosystem, but you may run into a design similar to this if you're wrapping code from another language that doesn't have a promise-based `async` system.

{% highlight csharp %}
async Task DoSomethingAsync(CancellationToken cancellationToken)
{
    using var registration = cancellationToken.Register(() => StopSomething());
    StartSomething();
    await SomethingCompletedTask;
}
{% endhighlight %}

Note that callbacks might never be invoked! Tasks always complete (that is, [they always _should_ complete](https://devblogs.microsoft.com/pfxteam/dont-forget-to-complete-your-tasks/?WT.mc_id=DT-MVP-5000058)), but that doesn't hold for cancellation tokens. There are some cancellation tokens that will never be cancelled, so they will never invoke their callbacks.

<div class="alert alert-danger" markdown="1">
<i class="fa fa-exclamation-triangle fa-2x pull-left"></i>

Your callback might never be called.
</div>

### A Race Condition

What happens if a cancellation token is cancelled at approximately the same time the callback is registered? This situation is properly handled by a simple rule: if a callback is ever added to a cancellation token that is already canceled, then it is immediately and synchronously invoked.

<div class="alert alert-danger" markdown="1">
<i class="fa fa-exclamation-triangle fa-2x pull-left"></i>

Your callback might be called immediately on the same thread before `Register` returns.
</div>

## Cleanup Is Important!

The lifetime of cancellation tokens can vary greatly. Some cancellation tokens are used for short, individual operations. Other cancellation tokens are used for application shutdown. When writing your cancelable code, ensure that your code disposes of the registration; this will prevent resource leaks in your application.

The `using var registration` in the example code above (repeated below) is one common way of handling cleanup: the registration is disposed once the asynchronous work completes.

{% highlight csharp %}
async Task DoSomethingAsync(CancellationToken cancellationToken)
{
    using var registration = cancellationToken.Register(() => StopSomething());
    StartSomething();
    await SomethingCompletedTask;
} // The registration is cleaned up here if the operation completed without being cancelled.
{% endhighlight %}

<div class="alert alert-danger" markdown="1">
<i class="fa fa-exclamation-triangle fa-2x pull-left"></i>

Be sure to dispose your cancellation token registrations!
</div>

## Sharp Corner: Synchronous Cancellation Callbacks

As discussed in [requesting cancellation]({% post_url 2022-03-03-cancellation-2-requesting-cancellation %}), token sources may be cancelled by calling the `Cancel` method. It's important to note that any registered callbacks are immediately (and synchronously) run by the `Cancel` method before it returns. This can be a source of deadlocks or other unexpected behavior if your code is written assuming that callbacks are invoked _after_ the `Cancel` method. Specifically, callbacks shouldn't perform any blocking operation.

<div class="alert alert-danger" markdown="1">
<i class="fa fa-exclamation-triangle fa-2x pull-left"></i>

Your callback is invoked synchronously in most cases.
</div>

This is awkward often enough that .NET 8.0 added a [`CancellationTokenSource.CancelAsync` method](https://learn.microsoft.com/en-us/dotnet/api/system.threading.cancellationtokensource.cancelasync?view=net-8.0&WT.mc_id=DT-MVP-5000058) which invokes the cancellation callbacks on a thread pool thread. Technically, it immediately and synchronously transitions the cancellation token source to the canceled state, and _then_ queues the callback invocations on a thread pool thread. The returned task completes when the callbacks have completed.

One more wrinkle, actually: it's possible that `CancelAsync` will return before the callbacks have completed _if_ it's already been called. As soon as the `CancellationTokenSource` transitions to the canceled state, any future calls to `Cancel` or `CancelAsync` will return immediately, even if a previous call to `CancelAsync` hasn't finished running its callbacks yet. But you probably don't need to worry about that; it's just a side note.

## Summary

This post has a bunch of scary warnings, but really, registering callbacks is the _natural_ way to implement cancellation at the lowest levels. [Polling]({% post_url 2022-03-17-cancellation-4-polling %}) is commonly used in sample code because it's simpler, but registration allows your code to react immediately.

Don't let the warnings dissuade you from using cancellation registrations! They're more like guidelines for proper usage:
- Your callback should not throw exceptions.
- Your callback might never be called.
- Your callback might be called immediately on the same thread before `Register` returns.
- Be sure to dispose your cancellation token registrations.
- Your callback is invoked synchronously in most cases.

If you follow these guidelines, you should be able to use cancellation token registrations successfully!
