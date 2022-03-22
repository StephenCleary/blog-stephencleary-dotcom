---
layout: post
title: "A Tour of Task, Part 1: Constructors"
series: "A Tour of Task"
seriesTitle: "Constructors"
description: "An analysis of Task constructors, and discussion of whether they should be used for asynchronous and/or parallel code."
---
TL;DR: Do not use `Task` or `Task<T>` constructors.

I actually debated quite a bit on how to start this series! I finally decided on starting with the constructors, even though the `Task` constructors are a red herring.

{:.center}
![]({{ site_url }}/assets/miniatus-grouper-247578_640.jpg)  
(not actually a red herring)

The `Task` type has a whopping eight constructors:

{% highlight csharp %}
Task(Action);
Task(Action, CancellationToken);
Task(Action, TaskCreationOptions);
Task(Action<Object>, Object);
Task(Action, CancellationToken, TaskCreationOptions);
Task(Action<Object>, Object, CancellationToken);
Task(Action<Object>, Object, TaskCreationOptions);
Task(Action<Object>, Object, CancellationToken, TaskCreationOptions);
{% endhighlight %}

The BCL avoids default parameters because they don't work well with [versioning](http://haacked.com/archive/2010/08/10/versioning-issues-with-optional-arguments.aspx/) and reflection. However, I'm going to rewrite some of the members with optional parameters to reduce the number of overloads that I need to talk about.

I'm going to call the eight constructors "actual members" because they actually exist. However, these can be reduced to only one "logical member":

{% highlight csharp %}
Task(Action action, CancellationToken token = new CancellationToken(), TaskCreationOptions options = TaskCreationOptions.None)
    : this(_ => action(), null, token, options) { }
Task(Action<Object>, Object, CancellationToken = new CancellationToken(), TaskCreationOptions = TaskCreationOptions.None);
{% endhighlight %}

Similarly, the `Task<T>` type has eight actual constructors:

{% highlight csharp %}
Task<TResult>(Func<TResult>);
Task<TResult>(Func<TResult>, CancellationToken);
Task<TResult>(Func<TResult>, TaskCreationOptions);
Task<TResult>(Func<Object, TResult>, Object);
Task<TResult>(Func<TResult>, CancellationToken, TaskCreationOptions);
Task<TResult>(Func<Object, TResult>, Object, CancellationToken);
Task<TResult>(Func<Object, TResult>, Object, TaskCreationOptions);
Task<TResult>(Func<Object, TResult>, Object, CancellationToken, TaskCreationOptions);
{% endhighlight %}

Which simplify down to a single logical constructor:

{% highlight csharp %}
Task<TResult>(Func<TResult> action, CancellationToken token = new CancellationToken(), TaskCreationOptions options = TaskCreationOptions.None)
    : base(_ => action(), null, token, options) { }
Task<TResult>(Func<Object, TResult>, Object, CancellationToken, TaskCreationOptions);
{% endhighlight %}

So, we have 16 actual constructors and two logical constructors.

## What For?

The use case for the task constructors is extremely small.

Remember that there are two kinds of tasks: Promise Tasks and Delegate Tasks. The task constructors _cannot_ create Promise Tasks; they only create Delegate Tasks.

Task constructors should not be used with `async`, and they should only rarely be used with parallel programming.

Parallel programming can be split into two types: [data](http://msdn.microsoft.com/en-us/library/ff963552.aspx?WT.mc_id=DT-MVP-5000058) [parallelism](http://msdn.microsoft.com/en-us/library/ff963547.aspx?WT.mc_id=DT-MVP-5000058) and task parallelism, with the majority of parallel situations calling for data parallelism. Task parallelism can be further split into two types: [static task parallelism](http://msdn.microsoft.com/en-us/library/ff963549.aspx?WT.mc_id=DT-MVP-5000058) (where the number of work items is known at the beginning of the parallel processing) and [dynamic task parallelism](http://msdn.microsoft.com/en-us/library/ff963551.aspx?WT.mc_id=DT-MVP-5000058) (where the number of work items changes while they are being processed). The [Parallel class](http://msdn.microsoft.com/en-us/library/vstudio/system.threading.tasks.parallel?WT.mc_id=DT-MVP-5000058) and [PLINQ](http://msdn.microsoft.com/en-us/library/dd460688(v=vs.110).aspx?WT.mc_id=DT-MVP-5000058) types in the [Task Parallel Library](http://msdn.microsoft.com/en-us/library/dd460693(v=vs.110).aspx?WT.mc_id=DT-MVP-5000058) provide higher-level constructs for dealing with data parallelism and static task parallelism. The only reason you should ever create a Delegate Task for parallel code is if you are doing dynamic task parallelism. But even then, you _almost never_ want to use the task constructors! The task constructors create a task that is not ready to run; it has to be scheduled first. This is almost never necessary; in the real world, most tasks should be scheduled immediately. The only reason you'd want to create a task and then _not_ schedule it is if you wanted to allow the caller to determine which thread the task actually runs on. And even in that scenario, I'd recommend using `Func<Task>` instead of returning an unscheduled task.

Let me put that another way: if you are doing dynamic task parallelism and need to construct a task that can run on any thread, and leave that scheduling decision up to another part of the code, and for whatever reason cannot use `Func<Task>` instead, then (and _only_ then) you should use a task constructor. I've written countless asynchronous and parallel applications, and I have **never** been in this situation.

Even shorter version: Do Not Use!

## What Instead?

If you're writing `async` code, the easiest way to create a Promise Task is to use the `async` keyword. If you're wrapping another asynchronous API or event, use `Task.Factory.FromAsync` or `TaskCompletionSource<T>`. If you need to run some CPU-bound code and treat it asynchronously, use `Task.Run`. We'll look at all of these options and more in future posts.

If you're writing parallel code, first try to use [Parallel](http://msdn.microsoft.com/en-us/library/vstudio/system.threading.tasks.parallel?WT.mc_id=DT-MVP-5000058) or [PLINQ](http://msdn.microsoft.com/en-us/library/dd460688(v=vs.110).aspx?WT.mc_id=DT-MVP-5000058). If you actually are doing dynamic task parallelism, use `Task.Run` or `Task.Factory.StartNew`. We'll consider these options as well in future posts.

## Conclusion

Sorry that the first post just boiled down to "don't use this", but it is what it is. I'll cover all the constructor arguments such as `CancellationToken` later when I cover `Task.Factory.StartNew`.
