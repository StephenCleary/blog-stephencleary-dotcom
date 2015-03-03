---
layout: post
title: "A Tour of Task, Part 9: Delegate Tasks"
series: "A Tour of Task"
seriesTitle: "Delegate Tasks"
description: "An analysis of TaskFactory.StartNew and Task.Run; and discussion of whether they should be used for asynchronous and/or parallel code."
---

Last time, we looked at [some archaic ways to start Delegate Tasks]({% post_url 2015-02-05-a-tour-of-task-part-8-starting %}). Today we'll look at a few members for creating Delegate Tasks in more modern code. Unline the task constructor, these methods return a Delegate Task that is *already running* (or at least *already scheduled to run*).

## TaskFactory.StartNew

First up is the oft-overused `TaskFactory.StartNew` method. There are a few overloads available:

{% highlight csharp %}
Task StartNew(Action);
Task StartNew(Action, CancellationToken);
Task StartNew(Action, TaskCreationOptions);
Task StartNew(Action, CancellationToken, TaskCreationOptions, TaskScheduler);

Task StartNew(Action<object>, object);
Task StartNew(Action<object>, object, CancellationToken);
Task StartNew(Action<object>, object, TaskCreationOptions);
Task StartNew(Action<object>, object, CancellationToken, TaskCreationOptions, TaskScheduler);

Task<TResult> StartNew<TResult>(Func<TResult>);
Task<TResult> StartNew<TResult>(Func<TResult>, CancellationToken);
Task<TResult> StartNew<TResult>(Func<TResult>, TaskCreationOptions);
Task<TResult> StartNew<TResult>(Func<TResult>, CancellationToken, TaskCreationOptions, TaskScheduler);

Task<TResult> StartNew<TResult>(Func<object, TResult>, object);
Task<TResult> StartNew<TResult>(Func<object, TResult>, object, CancellationToken);
Task<TResult> StartNew<TResult>(Func<object, TResult>, object, TaskCreationOptions);
Task<TResult> StartNew<TResult>(Func<object, TResult>, object, CancellationToken, TaskCreationOptions, TaskScheduler);
{% endhighlight %}

The overloads containing an `object` parameter simply pass that value through to the continuation delegate; this is just an optimization to avoid an extra allocation in some cases, so we can ignore those overloads for now. That leaves two sets of overloads, which act like default parameters for the two core methods:

{% highlight csharp %}
Task StartNew(Action, CancellationToken, TaskCreationOptions, TaskScheduler);
Task<TResult> StartNew<TResult>(Func<TResult>, CancellationToken, TaskCreationOptions, TaskScheduler);
{% endhighlight %}

`StartNew` can take a delegate without a return value (`Action`) or with a return value (`Task<TResult>`), and returns an appropriate task type based on whether the delegate returns a value. Note that neither of these delegate types are [`async`-aware delegates]({% post_url 2014-02-20-synchronous-and-asynchronous-delegate %}); this causes complications when developers try to use `StartNew` to start an asynchronous task.

<div class="alert alert-danger" markdown="1">
<i class="fa fa-exclamation-triangle fa-2x pull-left"></i>

`TaskFactory.StartNew` doesn't support `async`-aware delegates. `Task.Run` does.
</div>

The "default values" for the `StartNew` overloads come from their `TaskFactory` instance. The `CancellationToken` parameter defaults to `TaskFactory.CancellationToken`. The `TaskCreationOptions` parameter defaults to `TaskFactory.CreationOptions`. The `TaskScheduler` parameter defaults to `TaskFactory.Scheduler`. Let's consider each of these parameters in turn.

### CancellationToken

First, the `CancellationToken`. This paramter is often misunderstood. I've seen many (smart) developers pass a `CancellationToken` to `StartNew` believing that the token can be used to cancel the delegate at any time during its execution. However, this is not what happens. The `CancellationToken` passed to `StartNew` is only effective *before* the delegate starts executing. In other words, it cancels the *starting* of the delegate, not the delegate itself. Once that delegate starts executing, the `CancellationToken` argument cannot be used to cancel that delegate. The delegate itself must observe the `CancellationToken` (e.g., with `CancellationToken.ThrowIfCancellationRequested`) in order to support cancellation after it starts executing.

{:.center}
[![]({{ site_url }}/assets/i-do-not-think-it-means.jpg)]({{ site_url }}/assets/i-do-not-think-it-means.jpg)

However, there is a minor difference in behavior if you do also pass a `CancellationToken` to `StartNew`. If the delegate itself observes the `CancellationToken`, then it will raise an `OperationCanceledException`. If the `StartNew` call does not include that `CancellationToken`, then the returned task is faulted with that exception. However, if the delegate raises an `OperationCanceledException` from the same `CancellationToken` passed to `StartNew`, then the returned task is *canceled* instead of faulted, and the `OperationCanceledException` is replaced with a `TaskCanceledException`.

OK, that was a bit much to describe in words. If you want to see the same details expressed in code, see the unit tests in [this gist](https://gist.github.com/StephenCleary/37d95619f7803f444d3d).

However, this difference in behavior does not impact your code as long as you use one of the the common patterns for detecting cancellation. For asynchronous code, you'd `await` the task and catch an `OperationCanceledException` (for more complete examples, see the unit tests in [this gist](https://gist.github.com/StephenCleary/dfd2a8b0a50ea3040695)):

{% highlight csharp %}
try
{
  // "task" was started by StartNew, and either StartNew or
  // the task delegate observes a cancellation token.
  await task;
}
catch (OperationCanceledException ex)
{
  // ex.CancellationToken contains the cancellation token,
  // if you need it.
}
{% endhighlight %}

For synchronous code, you'd call `Wait` (or `Result`) on the task and expect an `AggregateException` whose `InnerException` is an `OperationCanceledException` (for more complete examples, see the unit tests in [this gist](https://gist.github.com/StephenCleary/6674ae30974f478a4b7f)):

{% highlight csharp %}
try
{
  // "task" was started by StartNew, and either StartNew or
  // the task delegate observes a cancellation token.
  task.Wait();
}
catch (AggregateException exception)
{
  var ex = exception.InnerException as OperationCanceledException;
  if (ex != null)
  {
    // ex.CancellationToken contains the cancellation token,
    // if you need it.
  }
}
{% endhighlight %}

In conclusion, the `CancellationToken` parameter of `StarNew` is nearly useless. It introduces some subtle changes in behavior, and is confusing to many developers. I never use it, myself.

### TaskCreationOptions

There are a couple of "scheduling options" that are just passed to the `TaskScheduler` that schedules the task. `PreferFairness` is a hint asking for FIFO behavior. `LongRunning` is a hint that the task will execute for a long time. As of this writing, the `TaskScheduler.Default` task scheduler will create a separate thread (outside the thread pool) for tasks with the `LongRunning` flag; however, this behavior is not guaranteed. Note that both of these options are just hints; it is entirely appropriate for the `TaskScheduler` to ignore them both.

There are a few more "scheduling options" that are not passed to the `TaskScheduler`. The `HideScheduler` option (introduced in .NET 4.5) will use the given task scheulder to schedule the task, but then will pretend that there is no current task scheduler while the task is executing; this can be used as a workaround for the unexpected default task scheduler (described below). The `RunContinuationsAsynchronously` option (introduced in .NET 4.6) will force any continuations of this task to execute asynchronously.

The "parenting options" control how the task is attached to the currently-executing task. [Attached child tasks](https://msdn.microsoft.com/en-us/library/vstudio/dd997417(v=vs.110).aspx) change the [behavior of their parent task]({% post_url 2014-06-05-a-tour-of-task-part-3-status %}) in ways that are convenient in some [dynamic task parallelism](https://msdn.microsoft.com/en-us/library/ff963551.aspx) scenarios, but are unexpected and awkward anywhere outside that (extremely small) use case. `AttachedToParent` will attach the task as a child task of the currently-executing task. In modern code, you almost never want this option; more importantly, you almost never want *other* code to attach child tasks to your tasks. For this reason, the `DenyChildAttach` option was introduced in .NET 4.5, which prevents any other tasks from using `AttachedToParent` to attach to this task.

<div class="alert alert-danger" markdown="1">
<i class="fa fa-exclamation-triangle fa-2x pull-left"></i>

`Task.Factory.StartNew` has a non-optimal default option setting of `TaskCreationOptions.None`. `Task.Run` uses the more appropriate default of `TaskCreationOptions.DenyChildAttach`.
</div>

### TaskScheduler

The `TaskScheduler` is used to schedule the continuation. A `TaskFactory` may define its own `TaskScheduler` which it uses by default. Note that the default `TaskScheduler` of the static `Task.Factory` instance is *not* `TaskScheduler.Default`, but rather `TaskScheduler.Current`. This fact has caused quite a bit of confusion over the years, because the vast majority of the time, developers expect (and desire) `TaskScheduler.Default`. I've [described this problem in detail before]({% post_url 2013-08-29-startnew-is-dangerous %}), but a little review never hurts.

The following code first creates a UI task factory to schedule work to the UI thread. Then, as a part of that work, it starts some work to run in the background.

{% highlight csharp %}
private void Button_Click(object sender, RoutedEventArgs e)
{
    var ui = new TaskFactory(TaskScheduler.FromCurrentSynchronizationContext());
    ui.StartNew(() =>
    {
        Debug.WriteLine("UI on thread " + Environment.CurrentManagedThreadId);
        Task.Factory.StartNew(() =>
        {
            Debug.WriteLine("Background work on thread " + Environment.CurrentManagedThreadId);
        });
    });
}
{% endhighlight %} 

The output on my system is:

    UI on thread 9
    Background work on thread 9

The problem is that while the outer `StartNew` is running, `TaskScheduler.Current` is the UI task scheduler. This is picked up as the default value for the `TaskScheduler` parameter by the inner `StartNew`, which causes the background work to be scheduled to the UI thread rather than a thread pool thread. This scenario can be avoided by passing `HideScheduler` to the outer `StartNew` task, or by passing an explicit `TaskScheduler.Default` to the inner `StartNew`.

<div class="alert alert-danger" markdown="1">
<i class="fa fa-exclamation-triangle fa-2x pull-left"></i>

`Task.Factory.StartNew` has a confusing default scheduler `TaskScheduler.Current`. `Task.Run` always uses the appropriate default of `TaskScheduler.Default`.
</div>

**In conclusion**, I do not recommend using `Task.Factory.StartNew` at all, unless you are doing [dynamic task parallelism](https://msdn.microsoft.com/en-us/library/ff963551.aspx) (which is extremely rare). In modern code, you should almost always use `Task.Run` instead. If you do have a custom `TaskScheduler` (e.g., one of the schedulers in `ConcurrentExclusiveSchedulerPair`), then it is appropriate to create your own `TaskFactory` instance and use `StartNew` on that; however, `Task.Factory.StartNew` should be avoided.

## Task.Run

`Task.Run` is the modern, preferred method for queueing work to the thread pool. It does not work with custom schedulers, but provides a simpler API than `Task.Factory.StartNew`, and is `async`-aware to boot:

{% highlight csharp %}
Task Run(Action);
Task Run(Action, CancellationToken);

Task Run(Func<Task>);
Task Run(Func<Task>, CancellationToken);

Task<TResult> Run<TResult>(Func<TResult>);
Task<TResult> Run<TResult>(Func<TResult>, CancellationToken);

Task<TResult> Run<TResult>(Func<Task<TResult>>);
Task<TResult> Run<TResult>(Func<Task<TResult>>, CancellationToken);
{% endhighlight %}

There are three axis of overloading going on here: whether or not there is a `CancellationToken`, whether the delegate returns a `TResult` value, and whether the delegate is synchronous (`Action`/`Func<TResult>`) or asynchronous (`Func<Task>`/`Func<Task<TResult>>`). Technically, `Task.Run` does not always create a Delegate Task; when it is given an asynchronous delegate, it actually returns a Promise Task. But conceptually, `Task.Run` is specifically for executing delegates on the thread pool, so I'm covering this set of overloads along with `StartNew` (which always does create Delegate Tasks).

The `CancellationToken` parameter sadly has the same problems described above for `StartNew`. That is, it really only cancels the *scheduling* of the delegate, which happens almost immediately. The presence of the `CancellationToken` argument does change the semantics slightly, similarly to `StartNew`. The full unit tests are [in this gist](https://gist.github.com/StephenCleary/37d95619f7803f444d3d), which has only one result that may be surprising: if an asynchronous delegate explicitly observes a `CancellationToken`, the returned task will be *canceled* instead of *faulted*. Just like `TaskFactory.StartNew`, these minor differences in semantics don't matter if the consuming code uses the standard pattern for detecting cancellation.

So, I conclude that the `CancellationToken` parameter of `Task.Run` is pretty much useless.

However, the other overloads are quite useful, and `Task.Run` is the best modern way for most code to queue work to the thread pool.