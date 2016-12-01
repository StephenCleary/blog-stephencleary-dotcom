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

For a thorough overview of the efficiency benefits of eliding `async` and `await`, see Stephen Toub's classic video [The Zen of Async](https://channel9.msdn.com/Events/Build/BUILD2011/TOOL-829T) or [his MSDN article on the subject](https://msdn.microsoft.com/en-us/magazine/hh456402.aspx).

When I started writing about `async`, I would always recommend eliding `async` and `await`, but I've modified that stand in recent years. There are just too many pitfalls to recommend eliding as a default decision. These days I recommend keeping the `async` and `await` keywords around except for a few scenarios, because of the drawbacks described in the rest of this blog post.

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

1. Create the `HttpClient` object.
2. Invoke `GetStringAsync`, which returns an incomplete task.
3. Pauses the method until the task returned from `GetStringAsync` completes, returning an incomplete task.
4. When the task returned from `GetStringAsync` completes, it resumes executing the method.
5. Disposes the `HttpClient` object.
6. Completes the task previously returned from `GetWithKeywordsAsync`.

With `GetElidingKeywordsAsync`, the code does this:

1. Create the `HttpClient` object.
2. Invoke `GetStringAsync`, which returns an incomplete task.
3. Disposes the `HttpClient` object.
4. Returns the task that that was returned from `GetStringAsync`.

Clearly, the `HttpClient` is disposed before the `GET` task completes, and this causes that request to be cancelled. The appropriate fix is to (asynchronously) wait until the `GET` operation is complete, and only then dispose the `HttpClient`, which is exactly what happens if you use `async` and `await`.

### Exceptions

Another easily-overlooked pitfall is that of exceptions. The state machine for `async` methods will capture exceptions from your code and place them on the returned task. Without the `async` keyword, the exception is raised directly rather than going on the task:

{% highlight csharp %}
public async Task<string> GetWithKeywordsAsync()
{
    string url = /* Something that can throw an exception */;
    return await DownloadStringAsync(url);
}

public Task<string> GetElidingKeywordsAsync()
{
    string url = /* Something that can throw an exception */;
    return DownloadStringAsync(url);
}
{% endhighlight %}

These methods work exactly the same as long as the calling method does something like this:

{% highlight csharp %}
var result = await GetWithKeywordsAsync(); // works fine

var result = await GetElidingKeywordsAsync(); // works fine
{% endhighlight %}

However, if the *method call* is separated from the `await`, then the semantics are different:

{% highlight csharp %}
var task = GetWithKeywordsAsync();
var result = await task; // Exception thrown here

var task = GetElidingKeywordsAsync(); // Exception thrown here
var result = await task;
{% endhighlight %}

<div class="alert alert-info" markdown="1">
<i class="fa fa-hand-o-right fa-2x pull-left"></i>

The invocation of the method can be separated from the `await` in a variety of cases. For example, the calling method may have other work to do concurrently with the asynchronous work done by our method. This is most common in code that uses `Task.WhenAll`.
</div>

The expected asynchronous semantics are that exceptions are placed on the returned task. Since the returned task represents the execution of the method, if that execution of that method is terminated by an exception, then the natural representation of that scenario is a faulted task.

So, eliding the keywords in this case causes different (and unexpected) exception behavior.

<!--
This pitfall is especially notable when writing synchronous implementations of asynchronous APIs; for proper exception handling, catch any exceptions from the synchronous implementation and return a faulted task:

{% highlight csharp %}
Task<string> INetwork.GetElidingKeywordsAsync()
{
    try
    {
        string result = /* Synchronous implementation */
        return Task.FromResult(result);
    }
    catch (Exception ex)
    {
        return Task.FromException<string>(ex);
    }
}
{% endhighlight %}
-->

### AsyncLocal

This pitfall is a bit harder to reason about.

`AsyncLocal<T>` (and the lower-level `LogicalCallContext`) allow asynchronous code to use a kind of `async`-compatible almost-equivalent of thread local storage. The [way that this actually works]({% post_url 2013-04-04-implicit-async-context-asynclocal %}) is that as part of the `async` compiler transformation, the compiler-generated code will notify the logical call context that it needs to establish a copy-on-write scope.

This provides a way for contextual information to flow "down" asynchronous calls. Note that the value does *not* flow "up".

{% highlight csharp %}
static AsyncLocal<int> context = new AsyncLocal<int>();

static async Task MainAsync()
{
    context.Value = 1;
    Console.WriteLine("Should be 1: " + context.Value);
    await Async();
    Console.WriteLine("Should be 1: " + context.Value);
}

static async Task Async()
{
    Console.WriteLine("Should be 1: " + context.Value);
    context.Value = 2;
    Console.WriteLine("Should be 2: " + context.Value);
    await Task.Yield();
    Console.WriteLine("Should be 2: " + context.Value);
}
{% endhighlight %}

In the example above, the context value is set in the "child" `Async` method, but when `Async` completes and the control flow moves back to `MainAsync`, the code executes in the "parent" context. So the "parent" value flows to the "child", but the "child" value does not flow to the "parent".

The pitfall is that synchronous methods do not notify the logical call context that anything is different. For normal (non-task-returning) synchronous methods, this works out fine; from the logical call context's perspective, all synchronous invocations are "collapsed" - they're actually part of the context of the closest `async` method further up the call stack. For example:

{% highlight csharp %}
static AsyncLocal<int> context = new AsyncLocal<int>();

static async Task MainAsync()
{
    context.Value = 1;
    Console.WriteLine("Should be 1: " + context.Value);
    await Async();
    Console.WriteLine("Should be 1: " + context.Value);
}

static async Task Async()
{
    Console.WriteLine("Should be 1: " + context.Value);
    Sync();
    Console.WriteLine("Should be 2: " + context.Value);
    await Task.Yield();
    Console.WriteLine("Should be 2: " + context.Value);
}

static void Sync()
{
    Console.WriteLine("Should be 1: " + context.Value);
    context.Value = 2;
    Console.WriteLine("Should be 2: " + context.Value);
}
{% endhighlight %}

In this example, `Async` does see the modification from its child `Sync` method. As I mentioned above, I prefer to think of this as the *synchronous* methods really being a part of the closest `async` context further up the stack. From the context's perspective, `Sync` is just a part of `Async`.

When eliding `async` and `await`, you do need to be aware that the task-returning non-`async` method is seen by the context as though it were a regular synchronous method. So, if it does any modification of the logical call context, it will actually affect its *parent* context:

{% highlight csharp %}
static AsyncLocal<int> context = new AsyncLocal<int>();

static async Task MainAsync()
{
    context.Value = 1;
    Console.WriteLine("Should be 1: " + context.Value);
    await Async();
    Console.WriteLine("Should be 1: " + context.Value); // Is actually "2" - unexpected!
}

static Task Async()
{
    Console.WriteLine("Should be 1: " + context.Value);
    context.Value = 2;
    Console.WriteLine("Should be 2: " + context.Value);
    return Task.CompletedTask;
}
{% endhighlight %}

This is a rare scenario, but it is one of the pitfalls of eliding `async`/`await`.

## Recommended Guidelines

I suggest following these guidelines:

1. Do **not** elide by default. Use the `async` and `await` for natural, easy-to-read code.
2. Do *consider* eliding when the method is **just** a passthrough or overload.

Examples:

{% highlight csharp %}
// Simple passthrough to next layer: elide.
Task<string> PassthroughAsync(int x) => _service.PassthroughAsync(x);

// Simple overloads for a method: elide.
async Task<string> OverloadsAsync(CancellationToken cancellationToken)
{
    ... // Core implementation, using await.
}
Task<string> OverloadsAsync() => OverloadsAsync(CancellationToken.None);

// Non-trivial passthrough: use keywords.
async Task<string> PassthroughAsync(int x)
{
    // Reasoning: GetFirstArgument can throw.
    //  Even if it doesn't throw today, some yahoo can change it tomorrow, and it's not possible for them to know to change *this* method, too.
    return await _service.PassthroughAsync(GetFirstArgument(), x);
}

// Non-trivial overloads for a method: use keywords.
async Task<string> OverloadsAsync()
{
    // Same reasoning as above; GetDefaultCancellationTokenForThisScope can throw.
    return await OverloadsAsync(GetDefaultCancellationTokenForThisScope());
}
{% endhighlight %}
