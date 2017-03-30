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

## Why No SynchronizationContext?

Stepping back a moment, a good question to ask is *why* the `AspNetSynchronizationContext` was removed in ASP.NET Core. While I'm not privy to the team's internal discussions on the subject, I assume it is for two reasons: performance and simplicity. Consider the performance aspect first.

When an asynchronous handler resumes execution on legacy ASP.NET, the continuation is *queued* to the request context. The continuation must wait for any other continuations that have already been queued (only one may run at a time). When it is ready to run, a thread is taken from the thread pool, *enters* the request context, and then resumes executing the handler. That "re-entering" the request context involves a number of housekeeping tasks, such as setting `HttpContext.Current` and the current thread's identity and culture.

With the contextless ASP.NET Core approach, when an asynchronous handler resumes execution, a thread is taken from the thread pool and executes the continuation. The context queue is avoided, and there is no "entering" of the request context necessary. In addition, the `async`/`await` mechanism is highly optimized for the contextless scenario. There's simply less work to do for asynchronous requests.

Simplicity is another aspect of this decision. `AspNetSynchronizationContext` worked well, but it had some tricky parts, [particularly around identity management](http://www.hanselman.com/blog/SystemThreadingThreadCurrentPrincipalVsSystemWebHttpContextCurrentUserOrWhyFormsAuthenticationCanBeSubtle.aspx).

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

There is one more major concern when moving from a synchronizing context to a thread pool context (i.e., from legacy ASP.NET to ASP.NET Core).

The legacy ASP.NET `SynchronizationContext` is an actual *synchronizing context*, meaning that within a request context, only one thread can actually *execute code* at a time. That is, asynchronous continuations may run on any thread, but *only one at a time*. ASP.NET Core does not have a `SynchronizationContext`, so `await` defaults to the thread pool context. So, in the ASP.NET Core world, asynchronous continuations may run on any thread, and they may all run in *parallel*.

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

