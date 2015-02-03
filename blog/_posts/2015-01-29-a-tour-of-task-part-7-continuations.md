---
layout: post
title: "A Tour of Task, Part 7: Continuations"
series: "A Tour of Task"
seriesTitle: "Continuations"
description: "An analysis of Task.ContinueWith, TaskFactory.ContinueWhenAny, TaskFactory.ContinueWhenAll, Task.WhenAll, and Task.WhenAny; and discussion of whether they should be used for asynchronous and/or parallel code."
---

Recent posts have considered several members that wait for tasks to complete ([`Wait`, `WaitAll`, `WaitAny`]({% post_url 2014-10-10-a-tour-of-task-part-5-wait %}), [`Result`, and `GetAwaiter().GetResult()`]({% post_url 2014-12-05-a-tour-of-task-part-6-results %})). One common disadvantage that all of these have is that they synchronously *block* the calling thread while waiting for the task to complete.

Today's post talks about *continuations*. A continuation is a delegate that you can attach to a task and tell the task "run this when you're done." When the task completes, it will then schedule its continuations. The task that a continuation attaches to is called the "antecedent" task.

Continuations are important because they don't block any threads. Instead of (synchronously) waiting for a task to complete, a thread may just attach a continuation for the task to run whenever it does complete. This is the essence of asynchrony, and the `async`/`await` system uses continuations whenever it deals with tasks.

## ContinueWith

The most low-level way to attach continuations to a task is to use its `ContinueWith` method. There are quite a number of overloads, but the general idea is to attach a delegate as a continuation for the task:

{% highlight csharp %}
Task ContinueWith(Action<Task>);
Task ContinueWith(Action<Task>, CancellationToken);
Task ContinueWith(Action<Task>, TaskContinuationOptions);
Task ContinueWith(Action<Task>, TaskScheduler);
Task ContinueWith(Action<Task>, CancellationToken, TaskContinuationOptions, TaskScheduler);
Task ContinueWith(Action<Task, object>, object);
Task ContinueWith(Action<Task, object>, object, CancellationToken);
Task ContinueWith(Action<Task, object>, object, TaskContinuationOptions);
Task ContinueWith(Action<Task, object>, object, TaskScheduler);
Task ContinueWith(Action<Task, object>, object, CancellationToken, TaskContinuationOptions, TaskScheduler);

Task<TResult> ContinueWith<TResult>(Func<Task, TResult>);
Task<TResult> ContinueWith<TResult>(Func<Task, TResult>, CancellationToken);
Task<TResult> ContinueWith<TResult>(Func<Task, TResult>, TaskContinuationOptions);
Task<TResult> ContinueWith<TResult>(Func<Task, TResult>, TaskScheduler);
Task<TResult> ContinueWith<TResult>(Func<Task, TResult>, CancellationToken, TaskContinuationOptions, TaskScheduler);
Task<TResult> ContinueWith<TResult>(Func<Task, object, TResult>, object);
Task<TResult> ContinueWith<TResult>(Func<Task, object, TResult>, object, CancellationToken);
Task<TResult> ContinueWith<TResult>(Func<Task, object, TResult>, object, TaskContinuationOptions);
Task<TResult> ContinueWith<TResult>(Func<Task, object, TResult>, object, TaskScheduler);
Task<TResult> ContinueWith<TResult>(Func<Task, object, TResult>, object, CancellationToken, TaskContinuationOptions, TaskScheduler);
{% endhighlight %}

Whew, that's a lot of overloads! Let's break it down a little. First, the overloads containing an `object` parameter just pass that value through to the continuation delegate; this is just an optimization to avoid an extra allocation in some cases, so we can ignore those overloads for now:

{% highlight csharp %}
Task ContinueWith(Action<Task>);
Task ContinueWith(Action<Task>, CancellationToken);
Task ContinueWith(Action<Task>, TaskContinuationOptions);
Task ContinueWith(Action<Task>, TaskScheduler);
Task ContinueWith(Action<Task>, CancellationToken, TaskContinuationOptions, TaskScheduler);

Task<TResult> ContinueWith<TResult>(Func<Task, TResult>);
Task<TResult> ContinueWith<TResult>(Func<Task, TResult>, CancellationToken);
Task<TResult> ContinueWith<TResult>(Func<Task, TResult>, TaskContinuationOptions);
Task<TResult> ContinueWith<TResult>(Func<Task, TResult>, TaskScheduler);
Task<TResult> ContinueWith<TResult>(Func<Task, TResult>, CancellationToken, TaskContinuationOptions, TaskScheduler);
{% endhighlight %}

There's also three optional parameters: a `CancellationToken` (defaulting to `CancellationToken.None`), a set of `TaskContinuationOptions` (defaulting to `TaskContinuationOptions.None`), and a `TaskScheduler` (defaulting to `TaskScheduler.Current`). So this list of overloads can be further simplified to:

{% highlight csharp %}
Task ContinueWith(Action<Task>, CancellationToken, TaskContinuationOptions, TaskScheduler);
Task<TResult> ContinueWith<TResult>(Func<Task, TResult>, CancellationToken, TaskContinuationOptions, TaskScheduler);
{% endhighlight %}

The `Task<T>` type has its own matching set of overloads. I won't bore you with the details - there are another 20 method signatures, which simplify in the same manner down to:

{% highlight csharp %}
Task ContinueWith(Action<Task<TResult>>, CancellationToken, TaskContinuationOptions, TaskScheduler);
Task<TContinuationResult> ContinueWith<TContinuationResult>(Func<Task<TResult>, TContinuationResult>, CancellationToken, TaskContinuationOptions, TaskScheduler);
{% endhighlight %}

At this point, it should be clear that there are two primary types of continuation delegates that can be passed to `ContinueWith`: one has a result value (`Func<...>`) and the other does not (`Action<...>`). The continuation delegate always receives a task as a parameter. This is the task that the continuation is attaching to, so if you were to call `task.ContinueWith(t => ...)`, then `task` and `t` refer to the same antecedent task instance.

`ContinueWith` also *returns* a task. This task represents the continuation itself. So, each continuation is itself a task, and may have its own continuations, and so on.

Let's talk a bit more about the optional parameters.

First, the `CancellationToken`. If you cancel the token *before* the continuation is scheduled, then the continuation delegate never actually runs - it's cancelled. However, note that the token does not cancel the continuation *once it has started*. In other words, the `CancellationToken` cancels the *scheduling* of the continuation, not the continuation itself. For this reason, I think the `CancellationToken` parameter is misleading, and I never use it myself.

The next parameter is `TaskContinuationOptions`, a collection of options for the continuation. Most options either have to do with *conditions*, *scheduling*, or *parenting* for the continuation. The `None` option means to use the default behavior; however, in modern applications, these defaults are only appropriate for [dynamic task parallelism](https://msdn.microsoft.com/en-us/library/ff963551.aspx), which is extremely rare.

The "condition options" will only schedule the continuation if the antecedent task completes in a [matching state]({% post_url 2014-06-05-a-tour-of-task-part-3-status %}). `OnlyOnRanToCompletion`, `OnlyOnFaulted`, and `OnlyOnCanceled` will only schedule the continuation if the antecedent task completes in a specific state. `NotOnRanToCompletion`, `NotOnFaulted`, and `NotOnCanceled` will only schedule the continuation if the antecedent task completes in *another* state. All of these "condition options" are roughly equivalent to just checking the task's `Status` from within the continuation.

**Update, 2015-01-30 [(suggested by Bar Arnon)](https://twitter.com/I3arnon/status/561150440960581637):** If the condition option is met by the antecedent task (e.g., the task completes in a `RanToCompletion` state and the continuation specified the `OnlyOnRanToCompletion` option), then the continuation is scheduled normally. However, if the condition option is *not* met (e.g., the task completes in a faulted state but the continuation specified the `OnlyOnRanToCompletion` option), then the continuation is cancelled. The continuation delegate is never executed and the continuation task immediately moves to the canceled state.

Several "scheduling options" are passed along to the `TaskScheduler` that is responsible for scheduling the continuation. `PreferFairness` is a hint asking for FIFO behavior. `LongRunning` is a hint that the continuation will execute for a long time. `ExecuteSynchronously` is a request that the continuation be scheduled on the same thread that completes the antecedent task. Note that all of these are just hints; it is entirely appropriate for the `TaskScheduler` to ignore them all; in particular, [`ExecuteSynchronously` does not guarantee that the continuation will execute synchronously](http://blogs.msdn.com/b/pfxteam/archive/2012/02/07/10265067.aspx).

<div class="alert alert-info" markdown="1">
<i class="fa fa-hand-o-right fa-2x pull-left"></i>

As of this writing, in the .NET 4.6 preview, there is another option called `RunContinuationsAsynchronously`, which seems to force continuations to execute asynchronously. Currently, there is no way to absolutely force continuations to be synchronous or asynchronous; forcing asynchronous continuations would certainly be useful in some situations.
</div>

There are a few more "scheduling options" that are not passed to the `TaskScheduler`. The `HideScheduler` option (introduced in .NET 4.5) will use the given task scheulder to schedule the continuation, but then will pretend that there is no current task scheduler while the continuation is executing; this can be used as a workaround for the unexpected default task scheduler (described below). `LazyCancellation` (introduced in .NET 4.5) is an option that ensures the continuation is completed (canceled) only after its antecedent completes. Without `LazyCancellation`, if the cancellation token passed to `ContinueWith` is cancelled, it could cancel the continuation before the original task even completed.

The "parenting options" control how the continuation task is attached to the antecedent task. [Attached child tasks](https://msdn.microsoft.com/en-us/library/vstudio/dd997417(v=vs.110).aspx) change the [behavior of their parent task]({% post_url 2014-06-05-a-tour-of-task-part-3-status %}) in ways that are convenient in some [dynamic task parallelism](https://msdn.microsoft.com/en-us/library/ff963551.aspx) scenarios, but are unexpected and awkward anywhere outside that (extremely small) use case. `AttachedToParent` will attach the continuation as a child task of the antecedent task. In modern code, you almost never want this option; more importantly, you almost never want *other* code to attach child tasks to your tasks. For this reason, the `DenyChildAttach` option was introduced in .NET 4.5, which prevents any continuations from using `AttachedToParent`.

The final optional parameter is a `TaskScheduler` that is used to schedule the continuation. Unfortunately, the default value for this parameter is *not* `TaskScheduler.Default`, but rather `TaskScheduler.Current`. This fact has caused quite a bit of confusion over the years, because the vast majority of the time, developers expect (and desire) `TaskScheduler.Default`. `Task.Factory.StartNew` has a [similar problem that I have described earlier]({% post_url 2013-08-29-startnew-is-dangerous %}). Since this default value is unexpected (and almost always undesirable), I recommend that you *always* pass a `TaskScheduler` value to `ContinueWith`. Many companies have run into this issue and enforce similar rules on their codebase.

**In conclusion**, I do not recommend using `ContinueWith` at all, unless you are doing [dynamic task parallelism](https://msdn.microsoft.com/en-us/library/ff963551.aspx) (which is extremely rare). In modern code, you should almost always use `await` instead of `ContinueWith`. There are several benefits to `await`.

One benefit is when working with other asynchronous code. As mentioned above, `ContinueWith` can take only a limited number of delegates, none of which are [`async`-aware delegates]({% post_url 2014-02-20-synchronous-and-asynchronous-delegate %}). When dealing with asynchronous continuations, `ContinueWith` will treat them as though they were synchronous. This can cause some manner of confusion when working with continuations of those continuations. Also, this means the scheduling options (e.g., `LongRunning`) do not work as most developers expect; they are only applied to the initial synchronous portion of an asynchronous delegate. In contrast, `await` works naturally with asynchronous continuations.

Another benefit is a better default task scheduler. Code using `ContinueWith` should always explicitly specify a task scheduler to reduce confusion, but `await` has [much more reasonable default behavior]({% post_url 2012-02-02-async-and-await %}). Modern code almost never uses task schedulers; it either uses `SynchronizationContext.Current` or the thread pool scheduler.

The last benefit is that `await` uses the most appropriate options by default. When you `await` an incomplete task, under the hood `await` does use `ContinueWith` to schedule a continuation for you. However, it will automatically use the appropriate options (`DenyChildAttach` and `ExecuteSynchronously`), and doesn't allow you to specify options that will not work correctly (e.g., `AttachedToParent` or `LongRunning`).

In short, **prefer `await` over `ContinueWith`**. `ContinueWith` is useful when doing [dynamic task parallelism](https://msdn.microsoft.com/en-us/library/ff963551.aspx), but in every other scenario, `await` is preferred.

## TaskFactory.ContinueWhenAny

`ContinueWhenAny` is a way of executing a *single* continuation when any of a *set* of tasks completes. So, it's a way to attach a single continuation to multiple tasks, and only have that continuation run when the first task completes.

The `TaskFactory` type has a set of `ContinueWhenAny` overloads that are somewhat similar to `ContinueWith`:

{% highlight csharp %}
Task ContinueWhenAny(Task[], Action<Task>);
Task ContinueWhenAny(Task[], Action<Task>, CancellationToken);
Task ContinueWhenAny(Task[], Action<Task>, TaskContinuationOptions);
Task ContinueWhenAny(Task[], Action<Task>, CancellationToken, TaskContinuationOptions, TaskScheduler);

Task ContinueWhenAny<TAntecedentResult>(Task<TAntecedentResult>[], Action<Task<TAntecedentResult>>);
Task ContinueWhenAny<TAntecedentResult>(Task<TAntecedentResult>[], Action<Task<TAntecedentResult>>, CancellationToken);
Task ContinueWhenAny<TAntecedentResult>(Task<TAntecedentResult>[], Action<Task<TAntecedentResult>>, TaskContinuationOptions);
Task ContinueWhenAny<TAntecedentResult>(Task<TAntecedentResult>[], Action<Task<TAntecedentResult>>, CancellationToken, TaskContinuationOptions, TaskScheduler);

Task<TResult> ContinueWhenAny<TResult>(Task[], Func<Task, TResult>);
Task<TResult> ContinueWhenAny<TResult>(Task[], Func<Task, TResult>, CancellationToken);
Task<TResult> ContinueWhenAny<TResult>(Task[], Func<Task, TResult>, TaskContinuationOptions);
Task<TResult> ContinueWhenAny<TResult>(Task[], Func<Task, TResult>, CancellationToken, TaskContinuationOptions, TaskScheduler);

Task<TResult> ContinueWhenAny<TAntecedentResult, TResult>(Task<TAntecedentResult>[], Func<Task<TAntecedentResult>, TResult>);
Task<TResult> ContinueWhenAny<TAntecedentResult, TResult>(Task<TAntecedentResult>[], Func<Task<TAntecedentResult>, TResult>, CancellationToken);
Task<TResult> ContinueWhenAny<TAntecedentResult, TResult>(Task<TAntecedentResult>[], Func<Task<TAntecedentResult>, TResult>, TaskContinuationOptions);
Task<TResult> ContinueWhenAny<TAntecedentResult, TResult>(Task<TAntecedentResult>[], Func<Task<TAntecedentResult>, TResult>, CancellationToken, TaskContinuationOptions, TaskScheduler);
{% endhighlight %}

Each of those groups of four overloads simplify down to a central method:

{% highlight csharp %}
Task ContinueWhenAny(Task[], Action<Task>, CancellationToken, TaskContinuationOptions, TaskScheduler);
Task ContinueWhenAny<TAntecedentResult>(Task<TAntecedentResult>[], Action<Task<TAntecedentResult>>, CancellationToken, TaskContinuationOptions, TaskScheduler);
Task<TResult> ContinueWhenAny<TResult>(Task[], Func<Task, TResult>, CancellationToken, TaskContinuationOptions, TaskScheduler);
Task<TResult> ContinueWhenAny<TAntecedentResult, TResult>(Task<TAntecedentResult>[], Func<Task<TAntecedentResult>, TResult>, CancellationToken, TaskContinuationOptions, TaskScheduler);
{% endhighlight %}

The overloads with a `TAntecedentResult` generic parameter are for when the antecedent tasks all have the same result type. The overloads with a `TResult` are for when the continuation returns a result of its own. The `TaskFactory<TResult>` type only has overloads supporting continuations that return a result, so it has half the overloads that `TaskFactory` does.

The default parameter values work similarly to `ContinueWith`, except that they are specified by the `TaskFactory` properties. So, the default `CancellationToken` is `TaskFactory.CancellationToken`, the default `ContinuationOptions` value is `TaskFactory.ContinuationOptions`, and the default `TaskScheduler` is `TaskFactory.Scheduler`, all of which may be set by passing the desired values into the `TaskFactory` constructor.

Note that the default `TaskScheduler` is still dangerous: anytime a `TaskFactory` is constructed without an explicit `TaskScheduler`, it will default to the value of `TaskScheduler.Current` at the time `ContinueWhenAny` is called. This causes the same surprising behavior as it does for `ContinueWith`. Note that the static `TaskFactory` instance `Task.Factory` does have this problematic default task scheduler.

I recommend not using these overloads at all; instead, use `await Task.WhenAny(...)` (see below) to asynchronously wait for one of a set of tasks to complete.

## TaskFactory.ContinueWhenAll

`ContinueWhenAll` is just like `ContinueWhenAny`, except the logic is that the continuation is executed once after *all* the antecedent tasks have completed. There are sixteen overloads on `TaskFactory` and eight on `TaskFactory<TResult>`, exactly like `ContinueWhenAny`. The same default parameter logic applies.

And the same default `TaskScheduler` is still dangerous.

And I recommend not using these overloads at all, either; instead, use `await Task.WhenAll(...)` (see below).

## Task.WhenAll

`Task.WhenAll` returns a task that completes when all of the antecedent tasks have completed. This is conceptually similar to `TaskFactory.ContinueWhenAll`, but works much more nicely with `await`:

{% highlight csharp %}
Task WhenAll(IEnumerable<Task>);
Task WhenAll(params Task[]);
Task<TResult[]> WhenAll<TResult>(IEnumerable<Task<TResult>>);
Task<TResult[]> WhenAll<TResult>(params Task<TResult>[]);
{% endhighlight %}

The `IEnumerable<>` overloads allow you to pass in a sequence of tasks, such as a LINQ expression. The sequence is immediately reified (i.e., copied to an array). For example, this allows you to pass the results of a `Select` expression directly to `WhenAll`. Personally, I usually prefer to explicitly reify the sequence by calling `ToArray()` so that it's obvious that's what's happening, but some folks like the ability to pass the sequence directly in.

The overloads with the `TResult` generic parameter will retrieve all the results of those tasks, as an array. This is very convenient when you have multiple operations of a similar nature. For example, you can do two concurrent downloads as such:

{% highlight csharp %}
var client = new HttpClient();
string[] results = await Task.WhenAll(
    client.GetStringAsync("http://example.com"),
    client.GetStringAsync("http://microsoft.com"));
// results[0] has the HTML of example.com
// results[1] has the HTML of microsoft.com
{% endhighlight %}

This is also powerful when combined with LINQ. The code below will simultaneously download whatever urls are in the source sequence:

{% highlight csharp %}
IEnumerable<string> urls = ...;
var client = new HttpClient();
string[] results = await Task.WhenAll(urls.Select(url => client.GetStringAsync(url)));
{% endhighlight %}

## Task.WhenAny

`Task.WhenAny` is similar to `Task.WhenAll`, but instead of asynchronously waiting for all antecedent tasks to complete, it asynchronously waits for only one. It has a similar set of overloads:

{% highlight csharp %}
Task<Task> WhenAny(IEnumerable<Task>);
Task<Task> WhenAny(params Task[]);
Task<Task<TResult>> WhenAny<TResult>(IEnumerable<Task<TResult>>);
Task<Task<TResult>> WhenAny<TResult>(params Task<TResult>[]);
{% endhighlight %}

The `IEnumerable<>` and `TResult` overloads serve the same purposes as they do for `WhenAll`. However, the *return* type of `WhenAny` is interesting. `WhenAny` returns a task that is completed when any of the antecedent tasks complete. The *result* of that task is the antecedent task that completed.

This means that applying a single `await` to a call to `WhenAny` will give you the task that completed. This allows you to do things like do two operations at the same time and see which finishes first:

{% highlight csharp %}
var client = new HttpClient();
Task<string> downloadExampleTask = client.GetStringAsync("http://example.com");
Task<string> downloadMicrosoftTask = client.GetStringAsync("http://microsoft.com");
Task completedTask = await Task.WhenAny(downloadExampleTask, downloadMicrosoftTask);
if (completedTask == downloadExampleTask)
  ; // example.com downloaded faster.
{% endhighlight %}

Usually, when you use `WhenAny`, you actually don't care about the tasks that *don't* complete first. That is, only the results of the first task are important. In this scenario, you can make use of the rare but legal "double await":

{% highlight csharp %}
var client = new HttpClient();
string results = await await Task.WhenAny(
    client.GetStringAsync("http://example.com"),
    client.GetStringAsync("http://microsoft.com"));
// results contains the HTML for whichever website responded first.
{% endhighlight %}

If you find the "double await" confusing, just break it out and specify the types. The code above is the same as:

{% highlight csharp %}
var client = new HttpClient();
Task<string> firstDownloadToComplete = await Task.WhenAny(
    client.GetStringAsync("http://example.com"),
    client.GetStringAsync("http://microsoft.com"));
string results = await firstDownloadToComplete; 
// results contains the HTML for whichever website responded first.
{% endhighlight %}

<div class="alert alert-info" markdown="1">
<i class="fa fa-hand-o-right fa-2x pull-left"></i>

I do recommend using `await` to retrieve the results of the completed task. In this case, it might seem that `await` is supurfluous, since we *know* that the task is already completed. However, `await` is still better than `Task.Result` because `await` will not wrap exceptions inside an `AggregateException`.
</div>