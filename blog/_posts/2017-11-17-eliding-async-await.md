---
layout: post
title: "Eliding Async and Await"
description: "A discussion about returning tasks directly rather than using async/await."
---

Once one has learned the basics of `async` and `await` and has gotten fairly comfortable with it, a common design question often comes up: If I can remove `async` and `await`, should I? There are a number of situations where you can elide the `async`/`await` keywords and just return the task directly.

This is a surprisingly nuanced question, and in fact I now hold to a different answer than the position I originally took on this issue.

First, let's check out the argument in favor of eliding the keywords.

## Efficiency

It's more efficient to elide `async` and `await`. By not including these keywords, the compiler can skip generating the `async` state machine. This means that there are fewer compiler-generated types in your assembly, less pressure on the garbage collector, and fewer CPU instructions to execute.

However, it's important to point out that each of these gains are absolutely minimal. There's one fewer type, a handful of small objects saved from GC, and only a few CPU instructions skipped. The vast majority of the time, `async` is dealing with I/O, which completely dwarfs any performance gains. In almost every scenario, eliding `async` and `await` doesn't make any difference to the running time of your application.

For a thorough overview with timed examples, see Stephen Toub's classic video [The Zen of Async](https://channel9.msdn.com/Events/Build/BUILD2011/TOOL-829T).

When I started writing about `async`, I would always recommend eliding `async` and `await`, but I've modified that stand in recent years. There are just too many pitfalls to recommend eliding as a default decision. These days I recommend keeping `async` and `await` around except for a few scenarios, because of the drawbacks described in this blog post.

## Pitfalls

By eliding `async` and `await`, you can avoid the compiler modifications to your method. Unfortunately, this also means that all the compiler modifications to your method must now be done by hand if you desire the same semantics.

### Using

One of the most common mistakes in eliding `async` and `await` is that developers forget that there is code at the end of their method that needs to run at the appropriate time. In particular, when using a `using` statement:

{% highlight csharp %}
public async Task<string> GetWithKeywordsAsync(string url)
{
    using (var client = new HttpClient())
        return await client.GetStringAsync(url);
}

public Task<string> GetElidingKeywordsAsync(string url)
{
    using (var client = new HttpClient())
        return client.GetStringAsync(url);
}
{% endhighlight %}

In this example, eliding the keywords will abort the download.

It's easier to understand if you walk through how the code progresses (if you need a review, my [intro to `async` and `await` post]({% post_url 2012-02-02-async-and-await %}) is still perfectly relevant today). For simplicity, I'll assume that `HttpClient.GetStringAsync` never completes synchronously.

With `GetWithKeywordsAsync`, the code does this:

1) Create the `HttpClient` object.
2) Invoke `GetStringAsync`, which returns an incomplete task.
3) Pauses the method until the task returned from `GetStringAsync` completes, returning an incomplete task.
4) When the task returned from `GetStringAsync` completes, resumes executing the method.
5) Disposes the `HttpClient` object.
6) Completes the task previously returned from `GetWithKeywordsAsync`.

With `GetElidingKeywordsAsync`, the code does this:

1) Create the `HttpClient` object.
2) Invoke `GetStringAsync`, which returns an incomplete task.
3) Disposes the `HttpClient` object.
4) Returns the task that that was returned from `GetStringAsync`.

Clearly, the `HttpClient` is disposed before the `GET` task completes, and this causes that request to be cancelled. The appropriate fix is to (asynchronously) wait until the `GET` operation is complete, and only then dispose the `HttpClient`, which is exactly what happens if you use `async` and `await`.

### Exceptions

Another easily-overlooked pitfall is that of exceptions. The state machine for `async` methods will capture exceptions from your code and place them on the returned task. Without the `async` keyword, the exception is raised directly rather than going on the task:

{% highlight csharp %}
string CreateUrlForId(int id);
Task<string> DownloadStringAsync(string url);

public async Task<string> GetByIdWithKeywordsAsync(int id)
{
    string url = CreateUrlForId(id);
    return await DownloadStringAsync(url);
}

public Task<string> GetByIdElidingKeywordsAsync(int id)
{
    string url = CreateUrlForId(id);
    return DownloadStringAsync(url);
}
{% endhighlight %}

Now, let's say that our implementation of `CreateUrlForId` looks like this:

{% highlight csharp %}
string CreateUrlForId(int id)
{
    return $"http://example.com/api/letters/{id}";
}
{% endhighlight %}

That's good. Both `GetByIdWithKeywordsAsync` and `GetByIdElidingKeywordsAsync` are working just fine.

Well, except for one thing. It's possible for invalid (negative) `id`s to reach this method, and we want to avoid a network call for obviously invalid `id`s. So, the developer responsible for implementing this decides it's logical to put the check into `CreateUrlForId`:

{% highlight csharp %}
string CreateUrlForId(int id)
{
    if (id < 0)
        throw new Exception("Naughty id!");
    return $"http://example.com/api/letters/{id}";
}
{% endhighlight %}



### AsyncLocal

## Recommended Guideline

## Synchronous Implementations of Asynchronous APIs

  - exceptions
  - https://github.com/dotnet/roslyn/issues/10449
  - DO NOT elide by default.
  - DO consider eliding when the mthod is just a passthrough or overload.