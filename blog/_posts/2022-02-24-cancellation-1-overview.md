---
layout: post
title: "Cancellation, Part 1: Overview"
series: "Cancellation"
seriesTitle: "Overview"
description: "How cancellation works in .NET and the 90% rule."
---

Cancellation is a topic that I haven't written on much yet, because the [Microsoft documentation](https://docs.microsoft.com/en-us/dotnet/standard/threading/cancellation-in-managed-threads) is quite good. But after answering many questions for many years, I thought it would be a good topic to cover once, exhaustively.

## Cooperative Cancellation

Cancellation in .NET is *cooperative*.

What this really means is that one part of the code Requests cancellation, and another part of the code Responds to that request. We often talk about some code "cancelling" other code, but what actually happens is that the requesting code just politely notifies the other code that it would like it to please stop, and the responding code may react to that cancellation request in any way it chooses. The responding code may immediately stop what it is doing, or it may continue until it reaches a valid stopping point, or it may ignore the cancellation request completely.

So, the important takeaway here is that the responding code must respond to the cancellation request in order for cancellation to actually *cancel* anything.

This discussion usually brings up a question: What about code that doesn't cooperate? I.e., how do I cancel uncancelable code? This is actually an advanced scenario, so I'll discuss it (with solutions) at the end of this series.

## Cancellation Tokens and the 90% Case

In .NET, a cancellation token is the "carrier" of a cancellation request. The requesting code will cancel a cancellation token, and the responding code reacts to the token being cancelled. We'll look at the details of how to create and cancel tokens - as well as how to respond to them - in future posts. For now, it's sufficient to know that the cancellation token is how the cancellation request is passed from the requesting code to the responding code.

In fact, I'd say about 90% of the cancellation code you'll need to write is simply that: add a `CancellationToken` parameter to your method and then pass it down to whatever APIs you call:

{% highlight csharp %}
async Task DoSomethingAsync(int data, CancellationToken cancellationToken)
{
    var intermediateValue = await DoFirstStepAsync(data, cancellationToken);
    await DoSecondStepAsync(intermediateValue, cancellationToken);
}
{% endhighlight %}

A `CancellationToken` can be any kind of cancellation: a user pressing a Cancel button; a client disconnecting from a server; an application shutting down; a timeout. It shouldn't matter to your code *why* it's being cancelled; just the fact that it *is* being cancelled.

Each `CancellationToken` may only be cancelled one time; once it is cancelled, it is always cancelled.

## The Cancellation Contract: Method Signature

[By convention](https://docs.microsoft.com/en-us/dotnet/standard/asynchronous-programming-patterns/task-based-asynchronous-pattern-tap#cancellation-optional), the `CancellationToken` parameter is usually the last parameter unless an `IProgress<T>` parameter is present. It is common to provide an overload or default parameter so that callers do not *have* to provide a `CancellationToken` if they do not have one; the `default` value of a `CancellationToken` is the same as `CancellationToken.None`, i.e., a cancellation token that will never be canceled.

{% highlight csharp %}
Task DoSomethingAsync(int data) => DoSomethingAsync(data, CancellationToken.None);
async Task DoSomethingAsync(int data, CancellationToken cancellationToken)
{
    ...
}

// or:

async Task DoSomethingAsync(int data, CancellationToken cancellationToken = default)
{
    ...
}
{% endhighlight %}

Some method signatures take both a `CancellationToken` and a timeout value as separate parameters. I don't recommend this for your own code; this is mainly done in the BCL to enable more efficient p/Invokes of methods that take timeout parameters. Unless you're also p/Invoking APIs that take timeout parameters, I recommend just taking a single `CancellationToken` which can represent *any* kind of cancellation.

By taking a `CancellationToken` parameter, a method is implicitly claiming that it may respond to cancellation. Technically, this is "may respond", not "must respond". In some cases (like interface implementations), a `CancellationToken` argument may be ignored. So the presence of a `CancellationToken` parameter does not necessarily mean the code *must* support cancellation, but it *might*.

## The Cancellation Contract: Response

As noted above, when cancellation is requested, the responding code may cancel whatever it is doing, or it may not. Even if it *attempts* to cancel, there is usually a race condition and the method may actually complete before the cancellation request can be honored. The cancellation contract handles this by having canceled code throw `OperationCanceledException` when the cancellation is observed and has actually canceled some work. If the cancellation request is ignored or if it arrives too late and the work is finished anyway, then the method returns normally without throwing `OperationCanceledException`.

The standard "90% case" code handles this implicitly; if `DoFirstStepAsync` or `DoSecondStepAsync` throw `OperationCanceledException`, then that exception is also propagated out of `DoSomethingAsync`. No change to the "90% case" code is necessary:

{% highlight csharp %}
async Task DoSomethingAsync(int data, CancellationToken cancellationToken)
{
    var intermediateValue = await DoFirstStepAsync(data, cancellationToken);
    await DoSecondStepAsync(intermediateValue, cancellationToken);
}
{% endhighlight %}

<div class="alert alert-danger" markdown="1">
<i class="fa fa-exclamation-triangle fa-2x pull-left"></i>

There are a lot of code examples out there that just silently return early when cancellation is requested. Please do not do this; it's a violation of the cancellation contract! When the responding code just returns early, the calling code cannot know whether its cancellation request was honored or ignored.
</div>

## Exception to the "90% Case"

The "90% case" just takes a `CancellationToken` parameter and passes it down. There's one notable exception to this rule: you shouldn't pass down `CancellationToken`s to `Task.Run`.

The reason is that (IMO) the semantics are confusing. A lot of developers pass a delegate and a `CancellationToken` to `Task.Run` and expect the delegate to be cancelled when the token is cancelled, but that's not what happens. The `CancellationToken` passed to `Task.Run` just cancels the *scheduling* of the delegate to the thread pool; once that delegate starts running (which happens pretty much immediately), that cancellation token is ignored.

To put it in example code, this is what many developers write, incorrectly expecting that `// Do something` will be canceled after it starts:

{% highlight csharp %}
async Task DoSomethingAsync(CancellationToken cancellationToken)
{
    var test = await Task.Run(() =>
    {
        // Do something, ignoring cancellationToken
    }, cancellationToken);
    ...
}
{% endhighlight %}

By never passing the `CancellationToken` to `Task.Run` (which is ignored anyway unless there's serious thread pool contention or the token is *already* cancelled), we make it clearer that the delegate *itself* has to respond to the token:

{% highlight csharp %}
async Task DoSomethingAsync(CancellationToken cancellationToken)
{
    var test = await Task.Run(() =>
    {
        // Do something
        // The IDE is telling me cancellationToken is unused,
        //    so this delegate code better use it.
    });
    ...
}
{% endhighlight %}
