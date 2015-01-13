---
layout: post
title: "A Tour of Task, Part 7: Continuations"
series: "A Tour of Task"
seriesTitle: "Continuations"
description: "An analysis of Task.ContinueWith, TaskFactory.ContinueWhenAny, TaskFactory.ContinueWhenAll, Task.WhenAll, and Task.WhenAny; and discussion of whether they should be used for asynchronous and/or parallel code."
---

Recent posts have considered several members that wait for tasks to complete ([`Wait`, `WaitAll`, `WaitAny`]({% post_url 2014-10-10-a-tour-of-task-part-5-wait %}), [`Result`, and `GetAwaiter().GetResult()`]({% post_url 2014-12-05-a-tour-of-task-part-6-results %})). One common disadvantage that all of these have is that they synchronously *block* the calling thread while waiting for the task to complete.

Today's post talks about *continuations*. A continuation is a delegate that you can attach to a task and tell the task "run this when you're done." When the task completes, it will then schedule its continuations.

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

<!--
The overloads taking an `object` parameter just pass that value through to the continuation delegate. This is just an optimization to avoid an extra allocation in some cases. Logically, all these overloads can simplify down to a single method:

{% highlight csharp %}
Task ContinueWith(Action<Task> continuation) { return ContinueWith(continuation, CancellationToken.None, TaskContinuationOptions.None, TaskScheduler.Current); }
Task ContinueWith(Action<Task> continuation, CancellationToken token) { return ContinueWith(continuation, token, TaskContinuationOptions.None, TaskScheduler.Current); }
Task ContinueWith(Action<Task> continuation, TaskContinuationOptions options) { return ContinueWith(continuation, CancellationToken.None, options, TaskScheduler.Current); }
Task ContinueWith(Action<Task> continuation, TaskScheduler scheduler) { return ContinueWith(continuation, CancellationToken.None, TaskContinuationOptions.None, scheduler); }
Task ContinueWith(Action<Task> continuation, CancellationToken token, TaskContinuationOptions options, TaskScheduler scheduler) { return ContinueWith(_ => continuation(), null, token, options, scheduler); }
Task ContinueWith(Action<Task, object> continuation, object state) { return ContinueWith(continuation, state, CancellationToken.None, TaskContinuationOptions.None, TaskScheduler.Current); }
Task ContinueWith(Action<Task, object> continuation, object state, CancellationToken token) { return ContinueWith(continuation, state, token, TaskContinuationOptions.None, TaskScheduler.Current); }
Task ContinueWith(Action<Task, object> continuation, object state, TaskContinuationOptions options) { return ContinueWith(continuation, state, CancellationToken.None, options, TaskScheduler.Current); }
Task ContinueWith(Action<Task, object> continuation, object state, TaskScheduler scheduler) { return ContinueWith(continuation, state, CancellationToken.None, TaskContinuationOptions.None, scheduler; }
Task ContinueWith(Action<Task, object> continuation, object state, CancellationToken token, TaskContinuationOptions options, TaskScheduler scheduler) { return ContinueWith<object>(s => { continuation(s); return null; }, state, token, options, scheduler); }
Task<TResult> ContinueWith<TResult>(Func<Task, TResult> continuation) { return ContinueWith(continuation, CancellationToken.None, TaskContinuationOptions.None, TaskScheduler.Current); }
Task<TResult> ContinueWith<TResult>(Func<Task, TResult> continuation, CancellationToken token) { return ContinueWith(continuation, token, TaskContinuationOptions.None, TaskScheduler.Current); }
Task<TResult> ContinueWith<TResult>(Func<Task, TResult> continuation, TaskContinuationOptions options) { return ContinueWith(continuation, CancellationToken.None, options, TaskScheduler.Current); }
Task<TResult> ContinueWith<TResult>(Func<Task, TResult> continuation, TaskScheduler scheduler) { return ContinueWith(continuation, CancellationToken.None, TaskContinuationOptions.None, scheduler); }
Task<TResult> ContinueWith<TResult>(Func<Task, TResult> continuation, CancellationToken token, TaskContinuationOptions options, TaskScheduler scheduler) { return ContinueWith(_ => continuation(), null, token, options, scheduler); }
Task<TResult> ContinueWith<TResult>(Func<Task, object, TResult> continuation, object state) { return ContinueWith(continuation, state, CancellationToken.None, TaskContinuationOptions.None, TaskScheduler.Current); }
Task<TResult> ContinueWith<TResult>(Func<Task, object, TResult> continuation, object state, CancellationToken token) { return ContinueWith(continuation, state, token, TaskContinuationOptions.None, TaskScheduler.Current); }
Task<TResult> ContinueWith<TResult>(Func<Task, object, TResult> continuation, object state, TaskContinuationOptions options) { return ContinueWith(continuation, state, CancellationToken.None, options, TaskScheduler.Current); }
Task<TResult> ContinueWith<TResult>(Func<Task, object, TResult> continuation, object state, TaskScheduler scheduler) { return ContinueWith(continuation, state, CancellationToken.None, TaskContinuationOptions.None, scheduler); }
Task<TResult> ContinueWith<TResult>(Func<Task, object, TResult>, object, CancellationToken, TaskContinuationOptions, TaskScheduler);
{% endhighlight %}
-->

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

At this point, it should be clear that there are two primary types of continuation delegates that can be passed to `ContinueWith`: one has a result value (`Func<...>`) and the other does not (`Action<...>`). The continuation delegate always receives a task as a parameter. This is the task that the continuation is attaching to, so if you were to call `task.ContinueWith(t => ..., ...)`, then `task` and `t` refer to the same instance.

`ContinueWith` also *returns* a task. This is a Promise Task that represents the continuation delegate itself.

Let's talk a bit more about the optional parameters.

First, the `CancellationToken`. If you cancel the token *before* the continuation is scheduled, then the continuation delegate never actually runs - it's cancelled. However, note that the token is not 

- Use await instead

## TaskFactory.ContinueWhenAny

`Task.WaitAny` is similar to `WaitAll` except it only waits for the first task to complete (and returns the index of that task). Again, we have the similar overloads:

{% highlight csharp %}
static int WaitAny(params Task[]);
static int WaitAny(Task[], CancellationToken);
static int WaitAny(Task[], int);
static int WaitAny(Task[], TimeSpan);
static int WaitAny(Task[], int, CancellationToken);
{% endhighlight %}

Which simplify down to a single logical method:

{% highlight csharp %}
static int WaitAny(params Task[] tasks) { return WaitAny(tasks, -1); }
static int WaitAny(Task[] tasks, CancellationToken token) { return WaitAny(tasks, -1, token); }
static int WaitAny(Task[] tasks, int timeout) { return WaitAny(tasks, timeout, CancellationToken.None); }
static int WaitAny(Task[] tasks, TimeSpan timeout) { return WaitAny(tasks, timeout.TotalMilliseconds); }
static int WaitAny(Task[], int, CancellationToken);
{% endhighlight %}

## TaskFactory.ContinueWhenAll

`Task.WaitAny` is similar to `WaitAll` except it only waits for the first task to complete (and returns the index of that task). Again, we have the similar overloads:

{% highlight csharp %}
static int WaitAny(params Task[]);
static int WaitAny(Task[], CancellationToken);
static int WaitAny(Task[], int);
static int WaitAny(Task[], TimeSpan);
static int WaitAny(Task[], int, CancellationToken);
{% endhighlight %}

Which simplify down to a single logical method:

{% highlight csharp %}
static int WaitAny(params Task[] tasks) { return WaitAny(tasks, -1); }
static int WaitAny(Task[] tasks, CancellationToken token) { return WaitAny(tasks, -1, token); }
static int WaitAny(Task[] tasks, int timeout) { return WaitAny(tasks, timeout, CancellationToken.None); }
static int WaitAny(Task[] tasks, TimeSpan timeout) { return WaitAny(tasks, timeout.TotalMilliseconds); }
static int WaitAny(Task[], int, CancellationToken);
{% endhighlight %}

## Task.WhenAll

## Task.WhenAny