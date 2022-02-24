---
layout: post
title: "Cancellation, Part 3: Detecting Cancellation"
series: "Cancellation"
seriesTitle: "Detecting Cancellation"
description: "After issuing a cancellation request, detect whether the operation completed normally or was cancelled."
---

It's not uncommon to want to detect cancellation. Cancellation is cooperative, and sometimes the code requesting cancellation needs to know whether that cancellation actually took place, or whether the operation just completed normally.

As a reminder, the cancellation contract has a way to communicate that: methods that take `CancellationToken`, by convention, will throw `OperationCanceledException` when they are cancelled. This is true for all BCL methods, and should be true for your code as well. Later in this series we'll cover the best ways for code to respond to cancellation requests, all of which satisfy this contract, i.e., throwing `OperationCanceledException` when they cancel.

## Responding to Cancellation

The most common scenario for detecting cancelltion is to avoid taking the normal error path if the code has been cancelled. Usually, the `OperationCanceledException` is just ignored:

{% highlight csharp %}
async Task TryDoSomethingAsync()
{
    using CancellationTokenSource cts = new();

    .. // Wire up something that may cancel the CTS.

    try
    {
        await DoThingAsync(cts.Token);
    }
    catch (Exception ex) when (ex is not OperationCanceledException)
    {
        .. // Normal error handling; log, etc.
    }
}
{% endhighlight %}

If your code must do something *different* when a cancellation happens, then you can handle that in a `catch` block. Well, first, I'd recommend taking a step back and asking yourself if you *really have* to do that, because it's unusual and raises concerns about the code design, and it can be difficult to test as well. But if you must:

{% highlight csharp %}
async Task DoSomethingAsync()
{
    using CancellationTokenSource cts = new();

    .. // Wire up something that may cancel the CTS.

    try
    {
        await DoThingAsync(cts.Token);
    }
    catch (OperationCanceledException)
    {
        .. // Special cancellation handling
    }
    catch (Exception ex)
    {
        .. // Normal error handling; log, etc.
    }
}
{% endhighlight %}

## TaskCanceledException

You may notice that there is another cancellation exception type: `TaskCanceledException`. This is raised by some APIs instead of `OperationCanceledException`.

As a general rule, I recommend you completely ignore `TaskCanceledException`. Some APIs just raise `OperationCanceledException`, even if they deal with cancelled tasks. And since `TaskCanceledException` derives from `OperationCanceledException`, your cancellation exception handler code can just use `OperationCanceledException`, ignore `TaskCanceledException` completely, and it will work everywhere.

<div class="alert alert-danger" markdown="1">
<i class="fa fa-exclamation-triangle fa-2x pull-left"></i>

Do not catch `TaskCanceledException`. Catch `OperationCanceledException` instead.
</div>

## OperationCanceledException.CancellationToken

You may also notice that `OperationCanceledException` has a `CancellationToken` property. This is the token that caused the cancellation. That is, if it's set; not all APIs set this value on the exceptions they throw.

If your code needs to determine whether *it* cancels the operation or whether something *else* cancels the operation, then you might be tempted to use this property. But I recommend that your code ignore this property. When linked cancel tokens are used (a topic I'll cover in a future post), it's possible that the token in this property is not actually the root cause of the cancellation.

More specifically:

{% highlight csharp %}
async Task DoSomethingAsync()
{
    Environment.FailFast("Bad code; do not use!");

    using CancellationTokenSource cts = new();

    .. // Wire up something that may cancel the CTS.

    try
    {
        await DoThingAsync(cts.Token);
    }
    catch (OperationCanceledException ex) when (ex.CancellationToken == cts.Token)
    {
        .. // Special cancellation handling for "our" cancellation only.
    }
}
{% endhighlight %}

The code above has a problem: depending on the implementation of `DoThingAsync`, it's possible that `cts` will be cancelled, and that cancellation will cause `DoThingAsync` to throw `OperationCanceledException`, and for the token referenced by that exception to be *different* than the `cts`'s token.

If you do need to do special processing for when this specific cancellation happens, I recommend something like this:

{% highlight csharp %}
async Task DoSomethingAsync()
{
    using CancellationTokenSource cts = new();

    .. // Wire up something that may cancel the CTS.

    try
    {
        await DoThingAsync(cts.Token);
    }
    catch (OperationCanceledException ex) when (cts.IsCancellationRequested)
    {
        .. // Special cancellation handling for "our" cancellation only.
    }
}
{% endhighlight %}

*Technically,* the semantics of this are not "did my token cause the cancellation", but rather "did cancellation happen and is my token requesting cancellation". But in every case I've seen in the real world, the alternative semantics have been sufficient.

<div class="alert alert-danger" markdown="1">
<i class="fa fa-exclamation-triangle fa-2x pull-left"></i>

Do not use `OperationCanceledException.CancellationToken`. I'll explain why in more detail in a future post, but the TL;DR is that it doesn't work as expected when linked cancellation tokens enter the picture.
</div>
