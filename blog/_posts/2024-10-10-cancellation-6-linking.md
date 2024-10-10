---
layout: post
title: "Cancellation, Part 6: Linking"
series: "Cancellation"
seriesTitle: "Linking"
description: "Providing complemetary cancellation by linking cancellation tokens."
---

So far we've covered how [cancellation is requested by one piece of code, and responded to by another piece of code]({% post_url 2022-02-24-cancellation-1-overview %}). The requesting code has a [standard way of requesting cancellation]({% post_url 2022-03-03-cancellation-2-requesting-cancellation %}), as well as a standard way of [detecting whether the code was canceled or not]({% post_url 2022-03-10-cancellation-3-detecting-cancellation %}). Meanwhile, the responding code can observe cancellation either by [polling]({% post_url 2022-03-17-cancellation-4-polling %}) or (more commonly) by [registering a cancellation callback]({% post_url 2024-08-08-cancellation-5-registration %}). So far, so good; and we're ready for the next step!

In this article, we'll look at how _linked_ cancellation tokens work. 

## Linked Cancellation Tokens

Linked cancellation tokens allow your code to create a `CancellationTokenSource` that cancels whenever any other cancellation tokens are canceled, in addition to a manual cancellation request.

The following code creates a linked cancellation token source:

{% highlight csharp %}
async Task DoSomethingAsync(CancellationToken cancellationToken)
{
    using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
    var task = DoSomethingElseAsync(cts.Token);
    ... // Do something while operation is in progress, possibly calling `cts.Cancel()`
    await task;
}
{% endhighlight %}

The `DoSomethingAsync` method above takes a `cancellationToken` - I'll call this the "outer" cancellation token. It then creates a CTS that is linked to that outer token. Then, when it calls `DoSomethingElseAsync`, it passes the token from that linked CTS, which I'll call the "inner" cancellation token.

If the outer cancellation token (`cancellationToken`) is ever canceled, then the linked CTS (`cts`) and its inner cancellation token (`cts.Token`) are also canceled. Furthermore, the `DoSomethingAsync` method has the option of explicitly cancelling the linked CTS - in this case, only the inner cancellation token would be canceled, leaving the outer cancellation token unchanged.

Sharp observers may have noticed that the same thing can be done using registrations:

{% highlight csharp %}
async Task DoSomethingAsync(CancellationToken cancellationToken)
{
    using var cts = new CancellationTokenSource();
    using var registration = cancellationToken.Register(cts.Cancel);
    var task = DoSomethingElseAsync(cts.Token);
    ... // Do something while operation is in progress, possibly calling `cts.Cancel()`
    await task;
}
{% endhighlight %}

Indeed, logically this is pretty much what is happening: you can think of a linked cancellation token source as a perfectly ordinary cancellation token source along with a registration that cancels it when some other token is canceled.

### Multiple Links

The inner cancellation token above is canceled when the outer token is canceled _or_ when its source is explicitly canceled. Similarly, we can pass any number of cancellation tokens into `CreateLinkedTokenSource`, and the cancellation token source it returns will be canceled when _any_ of the outer tokens are canceled.

## Use Cases

The outer token and the inner cancellation source can really represent anything; linked cancellation tokens are useful whenever you need code to be canceled if "A or B".

But I suspect the most common use case is when the outer token represents an end-user cancellation request, and the inner token represents a timeout. E.g., this can happen when the business logic includes a timeout-and-retry kind of code pattern, while also allowing the end-user to cancel all the retries with a single button click.

One natural place where this kind of code is used is Polly. Polly will allow you to pass in a cancellation token - an outer token that is under your control. Then it passes a _potentially different_ cancellation token to your execution delegate; this inner token is controlled by Polly. Polly's pipelines (e.g., timeout) may trigger the inner token to cancel your delegate. Naturally, if your code cancels the outer token passed to Polly, that would flow to the inner token as well. I.e., they are linked.

Taking a simplified code example right from the Polly homepage:

{% highlight csharp %}
async Task ExecuteWithTimeoutAsync(CancellationToken cancellationToken)
{
    ResiliencePipeline pipeline = new ResiliencePipelineBuilder()
        .AddTimeout(TimeSpan.FromSeconds(10))
        .Build();

    await pipeline.ExecuteAsync(async token =>
    {
        /* Your custom logic goes here */
    }, cancellationToken);
}
{% endhighlight %}

The `ExecuteWithRetryAndTimeoutAsync` takes an outer token `cancellationToken` and passes it to Polly. Polly then creates a linked inner token (which includes pipeline behaviors such as the timeout), and passes the inner token (`token`) to your delegate.

<div class="alert alert-danger" markdown="1">
<i class="fa fa-exclamation-triangle fa-2x pull-left"></i>

Delegates you pass to Polly should observe the token they get from Polly, not any other tokens!
</div>

This is particularly a pitfall when you're adding Polly pipelines to existing code, e.g., when adding timeouts to this code:

{% highlight csharp %}
async Task ExecuteAsync(CancellationToken cancellationToken)
{
    for (int i = 0; i != 10; ++i)
        await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
}
{% endhighlight %}

A common mistake is to forget to update the token usage:

{% highlight csharp %}
// BAD CODE!!! DO NOT USE!!!
async Task ExecuteWithTimeoutAsync(CancellationToken cancellationToken)
{
    ResiliencePipeline pipeline = new ResiliencePipelineBuilder()
        .AddTimeout(TimeSpan.FromSeconds(10))
        .Build();

    await pipeline.ExecuteAsync(async token =>
    {
        // BAD CODE!!! DO NOT USE!!!
        for (int i = 0; i != 10; ++i)
            await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
    }, cancellationToken);
}
{% endhighlight %}

In this case, the delegate is still observing `cancellationToken`, when it should be observing `token` instead:

{% highlight csharp %}
async Task ExecuteWithTimeoutAsync(CancellationToken cancellationToken)
{
    ResiliencePipeline pipeline = new ResiliencePipelineBuilder()
        .AddTimeout(TimeSpan.FromSeconds(10))
        .Build();

    await pipeline.ExecuteAsync(async token =>
    {
        for (int i = 0; i != 10; ++i)
            await Task.Delay(TimeSpan.FromSeconds(1), token);
    }, cancellationToken);
}
{% endhighlight %}

## Sharp Corner: Don't use OperationCanceledException.CancellationToken

Consider the original example code again:

{% highlight csharp %}
async Task DoSomethingAsync(CancellationToken cancellationToken)
{
    using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
    var task = DoSomethingElseAsync(cts.Token);
    ... // Do something while operation is in progress, possibly calling `cts.Cancel()`
    await task;
}
{% endhighlight %}

Now consider some code that may call `DoSomethingAsync` and respond to cancellation:

{% highlight csharp %}
// BAD CODE!!! DO NOT USE!!!
async Task MainAsync()
{
    using var cts = new CancellationTokenSource();
    cts.CancelAfter(TimeSpan.FromSeconds(2000));

    try
    {
        await DoSomethingAsync(cts.Token);
    }
    catch (OperationCanceledException ex) when (ex.CancellationToken == cts.Token) // BAD CODE!!! DO NOT USE!!!
    {
        Console.WriteLine("Timeout!");
    }
    catch (Exception ex)
    {
        Console.WriteLine(ex);
    }
}
{% endhighlight %}

The intent of the handling code is to do something different if the code is canceled _due to this particular cancellation source_. Unfortunately, this code is problematic in the real world; `DoSomethingAsync` may be using a linked cancellation token source, in which case the `OperationCanceledException.CancellationToken` would not match `cts.Token`, even if that was the source of the cancellation!

This is why I always recommend [not using `OperationCanceledException.CancellationToken`]({% post_url 2022-03-10-cancellation-3-detecting-cancellation %}). A proper solution is to check whether that source has been triggered:

{% highlight csharp %}
async Task MainAsync()
{
    using var cts = new CancellationTokenSource();
    cts.CancelAfter(TimeSpan.FromSeconds(2000));

    try
    {
        await DoSomethingAsync(cts.Token);
    }
    catch (OperationCanceledException) when (cts.IsCancellationRequested)
    {
        Console.WriteLine("Timeout!");
    }
    catch (Exception ex)
    {
        Console.WriteLine(ex);
    }
}
{% endhighlight %}

### Checking Inner Tokens: Still don't use OperationCanceledException.CancellationToken

You might be tempted to do this kind of test when using linked cancellation tokens, again to determine what the cancellation source is:

{% highlight csharp %}
// BAD CODE!!! DO NOT USE!!!
async Task DoSomethingAsync(CancellationToken cancellationToken)
{
    using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
    cts.CancelAfter(TimeSpan.FromSeconds(10));

    try
    {
        await DoSomethingElseAsync(cts.Token);
    }
    catch (OperationCanceledException ex) when (ex.CancellationToken == cts.Token) // BAD CODE!!! DO NOT USE!!!
    {
        ... // Do some recovery specific to the timeout.
        throw;
    }
}
{% endhighlight %}

However, this code has the same issue! It's possible that `DoSomethingElseAsync` may _itself_ use a linked cancellation token (or may be changed to use one in the future)!

The solution - again - is to not use `OperationCanceledException.CancellationToken`:

{% highlight csharp %}
async Task DoSomethingAsync(CancellationToken cancellationToken)
{
    using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
    cts.CancelAfter(TimeSpan.FromSeconds(10));

    try
    {
        await DoSomethingElseAsync(cts.Token);
    }
    catch (OperationCanceledException ex) when (cts.IsCancellationRequested)
    {
        ... // Do some recovery specific to the timeout.
        throw;
    }
}
{% endhighlight %}

## Summary

Most of the time you won't need to use linked cancellation tokens in your code, but linked cancellation tokens are useful when you need them! Some points to remember:

- Dispose your cancellation token sources - including linked cancellation token sources.
- Don't use `OperationCanceledException.CancellationToken`; use `IsCancellationRequested` instead.
- For any code that has multiple tokens in scope, be mindful about which one you are observing.
