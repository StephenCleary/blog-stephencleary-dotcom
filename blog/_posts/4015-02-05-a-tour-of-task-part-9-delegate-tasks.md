---
layout: post
title: "A Tour of Task, Part 9: Delegate Tasks"
series: "A Tour of Task"
seriesTitle: "Delegate Tasks"
description: "An analysis of TaskFactory.StartNew and Task.Run; and discussion of whether they should be used for asynchronous and/or parallel code."
---

Last time, we looked at [some archaic ways to start Delegate Tasks]({% post_url 2015-02-05-a-tour-of-task-part-8-starting %}). Today we'll look at a few members for creating Delegate Tasks in more modern code. Unline the archaic task constructor, these methods return a Delegate Task that is *already running* (or at least *already scheduled to run*).

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

First, the `CancellationToken`. This paramter is often misunderstood. I've seen many (smart) developers pass a `CancellationToken` to `StartNew` believing that the token can be used to cancel the delegate at any time during its execution. However, this is not what happens. The `CancellationToken` passed to `StartNew` is only effective *before* the delegate starts executing. In other words, it cancels the *starting* of the delegate, not the delegate itself. Once that delegate starts executing, the `CancellationToken` argument cannot be used to cancel that delegate. The delegate itself must observe the `CancellationToken` (e.g., with `CancellationToken.ThrowIfCancellationRequested`) in order to support cancellation after it starts executing.

{:.center}
[![]({{ site_url }}/assets/i-do-not-think-it-means.jpg)]({{ site_url }}/assets/i-do-not-think-it-means.jpg)

There is a minor difference in behavior if you specify a `CancellationToken`. If the delegate itself observes the `CancellationToken`, then it will raise an `OperationCanceledException`. If the `StartNew` call does not include that `CancellationToken`, then the returned task is faulted with that exception. However, if the delegate raises an `OperationCanceledException` from the same `CancellationToken` passed to `StartNew`, then the returned task is *canceled* instead of faulted, and the `OperationCanceledException` is replaced with a `TaskCanceledException`.

OK, that was a bit much to describe in words. If you want to see the same details expressed in code, see the unit tests in this gist. (TODO: link)

However, this difference in behavior does not impact your code as long as you use this pattern for detecting cancellation asynchronously: (TODO: gist)




## Run


{% highlight csharp %}
{% endhighlight %}

