---
layout: post
title: "ASP.NET Core SynchronizationContext"
description: "ASP.NET Core doesn't have a SynchronizationContext. But what does this mean for developers?"
---

[David Pine](http://davidpine.net/) pinged me on Slack the other day, suggesting that I write a blog post about the `SynchronizationContext` in ASP.NET Core. So, here it is!

## There Isn't One

My inital reply was just "well, that would be a short post!" Because there *isn't* a `SynchronizationContext` in ASP.NET Core.

{:.center}
[![]({{ site_url }}/assets/NoAspNetCoreSyncCtx.png)]({{ site_url }}/assets/NoAspNetCoreSyncCtx.png)

OK, so there's no `SynchronizationContext`. What does that mean for developers?

## You Can Block on Async Code - But You Shouldn't

The first and most obvious consequence is that there's no context captured by `await`. This means that [blocking on asynchronous code won't cause a deadlock.]({% post_url 2012-07-12-dont-block-on-async-code %}) You can use `Task.GetAwaiter().GetResult()` (or `Task.Wait` or `Task<T>.Result`) without fear of deadlock.

However, you *shouldn't*. Because the moment you block on asynchronous code, you're giving up every benefit of asynchronous code in the first place. The enhanced scalability of asynchronous handlers is nullified as soon as you block a thread.

There were a couple of scenarios in (legacy) ASP.NET where blocking was unfortunately necessary: ASP.NET MVC filters and child actions. However, in ASP.NET Core, the entire pipeline is fully asynchronous; both filters and view components execute asynchronously.

In conclusion, *ideally* you should strive to use `async` all the way; but if your code needs to, it *can* block without danger.

## You Don't Need ConfigureAwait(false), But Still Use It in Libraries

Since there is no context anymore, there's no need for `ConfigureAwait(false)`. Any code that *knows* it's running under ASP.NET Core does not need to explicitly avoid its context. In fact, the ASP.NET Core team themselves have dropped the use of `ConfigureAwait(false)`.

However, I still recommend that you use it in your core libraries - anything that may be reused in other applications. If you have code in a library that may also run in a UI app, or legacy ASP.NET app, or anywhere else there may be a context, then you should still use `ConfigureAwait(false)` in that library.

## Beware Implicit Parallelism

There's one more major concern when moving from a synchronizing context to a thread pool context (i.e., from legacy ASP.NET to ASP.NET Core). The legacy ASP.NET `SynchronizationContext` is an actual *synchronizing context*, meaning that within a request context, only one thread can actually *execute code* at a time. That is, asynchronous continuations may run on any thread, but *only one at a time*. ASP.NET Core does not have a `SynchronizationContext`, so `await` defaults to the thread pool context. So, in the ASP.NET Core world, asynchronous continuations may run on any thread, and they may all run in *parallel*.

As a contrived example, consider this code, which downloads two strings and places them into a list. This code works fine in legacy ASP.NET because the request context only permits one continuation at a time:

{% highlight csharp %}
private HttpClient _client = new HttpClient();

async Task<List<string>> GetBothAsync(string url1, string url2)
{
    var result = new List<string>();
    var task1 = GetOneAsync(result, url1);
    var task2 = GetOneAsync(result, url2);
    return result;
}

async Task GetOneAsync(List<string> result, string url)
{
    var data = await _client.GetStringAsync(url);
    result.Add(data);
}
{% endhighlight %}

The `result.Add(data)` line can only be executed by one thread at a time because it executes in the request context.

However, this same code is unsafe on ASP.NET Core; specifically, the `result.Add(data)` line may be executed by two threads *at the same time,* without protecting the shared `List<string>`.

Code such as this is rare; asynchronous code is by its nature functional, so it's far more *natural* to return results from asynchronous methods rather than modifying shared state. However, the quality of asynchronous code does vary, and there is doubtless some code out there that is not adequately shielded against parallel execution.
