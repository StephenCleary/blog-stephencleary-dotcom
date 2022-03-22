---
layout: post
title: "A Tour of Task, Part 5: Waiting"
series: "A Tour of Task"
seriesTitle: "Waiting"
description: "An analysis of Task.Wait, Task.WaitAll, Task.WaitAny, and Task.AsyncWaitHandle; and discussion of whether they should be used for asynchronous and/or parallel code."
---

Today, we'll look at a variety of ways that code can block on a task. All of these options block the calling thread until the task completes, so they're almost never used with [Promise Tasks]({% post_url 2014-06-05-a-tour-of-task-part-3-status %}). Note that [blocking on a Promise Task is a common cause of deadlocks]({% post_url 2012-07-12-dont-block-on-async-code %}); blocking is almost exclusively used with Delegate Tasks (i.e., a task returned from `Task.Run`).

## Wait

There are five overloads of `Wait`:

{% highlight csharp %}
void Wait();
void Wait(CancellationToken);
bool Wait(int);
bool Wait(TimeSpan);
bool Wait(int, CancellationToken);
{% endhighlight %}

These nicely simplify down to a single _logical_ method:

{% highlight csharp %}
void Wait() { Wait(-1); }
void Wait(CancellationToken token) { Wait(-1, token); }
bool Wait(int timeout) { return Wait(timeout, CancellationToken.None); }
bool Wait(TimeSpan timeout) { return Wait(timeout.TotalMilliseconds); }
bool Wait(int, CancellationToken);
{% endhighlight %}

`Wait` is rather simple: it will block the calling thread until the task completes, a timeout occurs, or the wait is cancelled. If the wait is cancelled, then `Wait` raises an `OperationCanceledException`. If a timeout occurs, then `Wait` returns `false`. If the task completes in a failed or canceled state, then `Wait` wraps any exceptions into an `AggregateException`. Note that a canceled _task_ will raise an `OperationCanceledException` wrapped in an `AggregateException`, whereas a canceled _wait_ will raise an unwrapped `OperationCanceledException`.

`Task.Wait` is occasionally useful, if it's done in the correct context. For example, the `Main` method of a Console application can use `Wait` if it has asynchronous work to do, but wants the main thread to synchronously block until that work is done. However, most of the time, `Task.Wait` is dangerous because of its [deadlock potential]({% post_url 2012-07-12-dont-block-on-async-code %}).

<div class="alert alert-info" markdown="1">
<i class="fa fa-hand-o-right fa-2x pull-left"></i>

For asynchronous code, use `await` instead of `Task.Wait`.
</div>

## WaitAll

The overloads for `WaitAll` are very similar to the overloads of `Wait`:

{% highlight csharp %}
static void WaitAll(params Task[]);
static void WaitAll(Task[], CancellationToken);
static bool WaitAll(Task[], int);
static bool WaitAll(Task[], TimeSpan);
static bool WaitAll(Task[], int, CancellationToken);
{% endhighlight %}

Again, these nicely simplify down to a single logical method:

{% highlight csharp %}
static void WaitAll(params Task[] tasks) { WaitAll(tasks, -1); }
static void WaitAll(Task[] tasks, CancellationToken token) { WaitAll(tasks, -1, token); }
static bool WaitAll(Task[] tasks, int timeout) { return WaitAll(tasks, timeout, CancellationToken.None); }
static bool WaitAll(Task[] tasks, TimeSpan timeout) { return WaitAll(tasks, timeout.TotalMilliseconds); }
static bool WaitAll(Task[], int, CancellationToken);
{% endhighlight %}

These are practically identical to `Task.Wait`, except they wait for multiple tasks to all complete. Similarly to `Task.Wait`, `Task.WaitAll` will throw `OperationCanceledException` if the wait is cancelled, or an `AggregateException` if any of the tasks fail or are cancelled. `WaitAll` will return `false` if a timeout occurs.

`Task.WaitAll` should be very rarely used. It is occasionally useful when working with [Delegate Tasks]({% post_url 2014-06-05-a-tour-of-task-part-3-status %}), but even this usage is rare. Developers writing parallel code should first attempt data parallelism; and even if task parallism is necessary, then parent/child tasks may result in cleaner code than defining ad-hoc dependencies with `Task.WaitAll`.

<div class="alert alert-info" markdown="1">
<i class="fa fa-hand-o-right fa-2x pull-left"></i>

Note that `Task.WaitAll` (for synchronous code) is rare, but `Task.WhenAll` (for asynchronous code) is common.
</div>

## WaitAny

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

The semantics of `WaitAny` are a bit different than `WaitAll` and `Wait`: `WaitAny` merely waits for the first task to complete. It will _not_ propagate that task's exception in an `AggregateException`. Rather, any task failures will need to be checked after `WaitAny` returns. `WaitAny` will return `-1` on timeout, and will throw `OperationCanceledException` if the wait is cancelled.

If `Task.WaitAll` is rarely used, `Task.WaitAny` should hardly ever be used at all.

## AsyncWaitHandle

The `Task` type actually implements `IAsyncResult` for [easy interoperation with the (unfortunately named) Asynchronous Programming Model (APM)](http://msdn.microsoft.com/en-us/library/hh873178(v=vs.110).aspx#TapToApm?WT.mc_id=DT-MVP-5000058). This means `Task` has a wait handle as one of its properties:

{% highlight csharp %}
WaitHandle IAsyncResult.AsyncWaitHandle { get; }
{% endhighlight %}

Note that this member is explicitly implemented, so consuming code _must_ cast the `Task` as `IAsyncResult` before reading it. The actual underlying wait handle is lazy-allocated.

Code using `AsyncWaitHandle` should be extremely, _extremely_ rare. It only makes sense if you have tons of existing code that is built around `WaitHandle`. If you do read the `AsyncWaitHandle` property, strongly consider [disposing the task instance](https://devblogs.microsoft.com/pfxteam/do-i-need-to-dispose-of-tasks/?WT.mc_id=DT-MVP-5000058).

## Conclusion

There are a few corner cases where a single `Task.Wait` could be useful; but in general, code should not synchronously block on a task.