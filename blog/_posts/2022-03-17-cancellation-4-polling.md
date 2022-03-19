---
layout: post
title: "Cancellation, Part 4: Polling"
series: "Cancellation"
seriesTitle: "Polling"
description: "Responding to cancellation requests by using polling."
---

So far in this series, I've talked about how to request and detect cancellation, but for the next couple of posts I'll be switching perspectives and discussing ways to respond to cancellation.

I know I've probably said this a half dozen times already, but it bears repeating: cancellation is cooperative. Your code will be provided a `CancellationToken`, and it must *do* something with that `CancellationToken` in order to be cancelable. Most of the time, this is just passing the `CancellationToken` down to lower-level APIs, but if you want cancelable code at the lowest level, there are a couple of other options. The one we're looking at today is polling.

## How to Poll

The normal pattern is to periodically call `ThrowIfCancellationRequested`:

{% highlight csharp %}
void DoSomething(CancellationToken cancellationToken)
{
    while (!done)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Thread.Sleep(200); // do synchronous work
    }
}
{% endhighlight %}

The example code above checks the cancellation token *before* it starts work, which is a good general practice. It is possible that the token is already cancelled by the time your operation starts running.

`ThrowIfCancellationRequested` will check to see if cancellation is requested, and if it is, it will throw `OperationCanceledException`. So it handles the proper reporting of cancellation for you; your code should just let that exception propagate out of the method.

One question you'll need to answer is how *often* to poll. There really isn't a good answer for this; ideally you probably want to poll a few times a second, but when you're talking about CPU-bound code running on potentially very different machines, it's pretty much a guess at where in the code the `ThrowIfCancellationRequested` should go. Just put it in the best place(s), run some tests to see if cancellation feels responsive enough, and that's the best you can do.

## How Not to Poll

There's a sadly common antipattern regarding polling for cancellation, particularly in infinite loops: the "while not cancelled" loop, which looks like this:

{% highlight csharp %}
void DoSomethingForever(CancellationToken cancellationToken)
{
    Environment.FailFast("Bad code! Do not use!");
    while (!cancellationToken.IsCancellationRequested)
    {
        Thread.Sleep(200); // do work
    }
}
{% endhighlight %}

When this code runs, it will periodically check the cancellation token; but when cancellation is requested, the method just returns early. This method doesn't satisfy the cancellation contract of throwing an exception on cancellation. This means that the calling code cannot know whether the method ran to completion or whether it was cancelled.

The proper solution is to use `ThrowIfCancellationRequested`, even for infinite loops:

{% highlight csharp %}
void DoSomethingForever(CancellationToken cancellationToken)
{
    while (true)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Thread.Sleep(200); // do work
    }
}
{% endhighlight %}

## When to Poll

Polling is an appropriate option for observing cancellation if your code is synchronous, such as CPU-bound code. Cancellation is often thought of as only a concern for asynchronous code; it was explicitly pointed out in the documentation for `async` when `async` was introduced. But cancellation in .NET predates `async`, and cancellation is just as applicable to synchronous code as to asynchronous code. In fact, `Parallel` loops and PLINQ each have built-in support for cancellation: `ParallelOptions.CancellationToken` for `Parallel`, and `WithCancellation` for PLINQ.

That's not to say you can't use it for asynchronous code. It's also appropriate to inject a `ThrowIfCancellationRequested` in asynchronous code, if you are not sure whether other methods will respect their cancellation tokens. Remember, taking a `CancellationToken` parameter means that the method *may* respect cancellation; it may also ignore the token and just ignore any cancellation requests. So your code may want to inject cancellation checks in-between "steps" of its operation. In this case, `ThrowIfCancellationRequested` isn't so much "polling" as it is an "occasional check":

{% highlight csharp %}
async Task DoComplexWorkAsync(CancellationToken cancellationToken)
{
    // From testing, it appears that DoStep1Async and DoStep2Async do not always cancel when requested.
    cancellationToken.ThrowIfCancellationRequested();
    await DoStep1Async(cancellationToken);

    cancellationToken.ThrowIfCancellationRequested();
    await DoStep2Async(cancellationToken);
}
{% endhighlight %}

While you *can* sprinkle calls to `ThrowIfCancellationRequested` throughout your code like this, I only do this when testing indicates that the code does not respect cancellation. In other words, I assume that `DoStep1Async` and `DoStep2Async` will respect cancellation until proven otherwise by testing.

It is also appropriate to use `ThrowIfCancellationRequested` at certain points where your code is about to do something expensive. Just adding a cancellation check there means your code won't have to do the expensive work if it's cancelled anyway.

## Summary

Polling - implemented by periodic calls to `ThrowIfCancellationRequested` - is one way to respond to cancellation requests. This is the common solution for synchronous, CPU-bound methods, and can also be used in a few other scenarios. Most asynchronous code does not use polling; we'll cover that scenario next time!
