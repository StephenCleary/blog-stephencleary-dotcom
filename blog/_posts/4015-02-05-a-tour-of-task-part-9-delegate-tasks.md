---
layout: post
title: "A Tour of Task, Part 9: Delegate Tasks"
series: "A Tour of Task"
seriesTitle: "Delegate Tasks"
description: "An analysis of Task.Factory.StartNew and Task.Run; and discussion of whether they should be used for asynchronous and/or parallel code."
---




When a task is [created using its constructor]({% post_url 2014-05-15-a-tour-of-task-part-1-constructors %}), it is [initially in a `Created` state]({% post_url 2014-06-05-a-tour-of-task-part-3-status %}). This is a kind of "holding" state, where the task won't do anything until it is started. Today's post considers ways to start tasks that have already been created.

<div class="alert alert-danger" markdown="1">
<i class="fa fa-exclamation-triangle fa-2x pull-left"></i>

There is *absolutely nothing* in this blog post that is recommended for modern code. If you're looking for *best* practices, move along; there's nothing to see here.
</div>

## Start

The most basic way to start a task is to just call its `Start` method. Sounds simple, right?

{% highlight csharp %}
void Start();
void Start(TaskScheduler);
{% endhighlight %}

As you may guess by now, the default `TaskScheduler` is not `TaskScheduler.Default`. It is `TaskScheduler.Current`. And, once again, this brings up [all the same problems that `Task.Factory.StartNew` has with its similar treatment of `TaskScheduler`]({% post_url 2013-08-29-startnew-is-dangerous %}).

Originally, `Start` (and the task constructors) were intended so that developers could define tasks to be executed kind of like a fancy delegate. Then, other code could execute those tasks in whatever way it felt appropriate (e.g., on the UI thread, or on a background thread). But in the real world, this is almost never useful, because *what the delegate does* usually determines its required *context* (e.g., a delegate accessing UI elements *must* be run on the UI thread). So this separation doesn't make sense for most code, and even when it does, developers just use delegates directly instead of tasks.

`Start` can only be called on a task that is created with the task constructor; that is, it only works on Delegate Tasks that are in the `Created` state. Once `Start` is called, the task moves to the `WaitingToRun` state (and never returns to the `Created` state), so `Start` cannot be called on a task more than once. `Start` cannot be called on Promise Tasks at all, since they are never in the `Created` state.

<div class="alert alert-info" markdown="1">
<i class="fa fa-hand-o-right fa-2x pull-left"></i>

For more information on the task states, Delegate Tasks, and Promise Tasks, see [Part 3 (Status) of this series]({% post_url 2014-06-05-a-tour-of-task-part-3-status %}){:.alert-link}.
</div>

In modern code - even dynamic task parallel code - `Start` just doesn't have a place anymore. Instead of the task constructor and `Start`, use `Task.Run`, which creates *and* schedules a task.

## RunSynchronously

`RunSynchronously` is very similar to `Start`, and has the same overloads:

{% highlight csharp %}
void RunSynchronously();
void RunSynchronously(TaskScheduler);
{% endhighlight %}

`RunSynchronously` will attempt to start the task immediately and execute it *on the current thread*. This does not always happen, however; the final decision is up to the task scheduler that is passed to `RunSynchronously`. For example, a task scheduler for the UI thread will not permit a task to run on a thread pool thread. If the task scheduler refuses to execute the task synchronously, then `RunSynchronously` behaves just like `Start`; that is, the task is queued to the task scheduler for future execution.

Once again, the default `TaskSchedler` is `TaskScheduler.Current`. However, this time this behavior does make sense: since `RunSynchronously` will attempt to execute the task's delegate on the current thread, it is reasonable to assume the current task scheduler is the correct one to use.

Similarly to `Start`, `RunSynchronously` doesn't have any place in modern code.

## IAsyncResult.CompletedSynchronously

`Task` has an explicit interface implementation of a member called `CompletedSynchronously`:

{% highlight csharp %}
bool IAsyncResult.CompletedSynchronously { get; }
{% endhighlight %}

If you believe the MSDN documentation, this member should return `true` if the task was completed synchronously. Unfortunately, this member actually always returns `false`, even for synchronously-completed Promise Tasks such as those returned from `Task.FromResult`.

`IAsyncResult.CompletedSynchronously` is used by some legacy `IAsyncResult`-based code. But generally speaking, this member shouldn't be used in modern code. In particular, you can't depend on it being anything other than `false` for tasks.