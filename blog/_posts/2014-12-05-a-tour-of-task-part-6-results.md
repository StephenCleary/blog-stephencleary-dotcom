---
layout: post
title: "A Tour of Task, Part 6: Results"
series: "A Tour of Task"
seriesTitle: "Results"
description: "An analysis of Task.Result, Task.Exception, and Task.GetAwaiter; and discussion of whether they should be used for asynchronous and/or parallel code."
---

The task members discussed in this blog post are concerned with retrieving results from the task. Once the task completes, the consuming code must retrieve the results of the task. Even if the task has no result, it's important for the consuming code to examine the task for errors so it knows whether the task completed successfully or failed.

## Result

The `Result` member only exists on the `Task<T>` type; it does not exist on the `Task` type (which represents a task without a result value).

{% highlight csharp %}
T Result { get; }
{% endhighlight %}

[Like `Wait`]({% post_url 2014-10-10-a-tour-of-task-part-5-wait %}), `Result` will synchronously block the calling thread until the task completes. This is generally not a good idea for the same reason it wasn't a good idea for `Wait`: [it's easy to cause deadlocks]({% post_url 2012-07-12-dont-block-on-async-code %}).

Furthermore, `Result` will wrap any task exceptions inside an `AggregateException`. This usually just complicates the error handling.

## Exception

Speaking of exceptions, there's a member specifically just for retrieving the exceptions from a task:

{% highlight csharp %}
AggregateException Exception { get; }
{% endhighlight %}

Unlike `Result` and `Wait`, `Exception` will *not* block until the task completes; if called while the task is still in progress, it will just return `null`. If the task completes successfully *or* is cancelled, then `Exception` will still return `null`. If the task is faulted, then `Exception` will return the task's exceptions wrapped in an `AggregateException`. Again, this usually just serves to complicate the error handling.

## GetAwaiter().GetResult()

The `GetAwaiter` member was added to `Task` and `Task<T>` in .NET 4.5, and it's available as an extension method on .NET 4.0 using the `Microsoft.Bcl.Async` NuGet package. Normally, the `GetAwaiter` method is just used by `await`, but it *is* possible to call it yourself:

{% highlight csharp %}
Task<T> task = ...;
T result = task.GetAwaiter().GetResult();
{% endhighlight %}

The code above will synchronously block until the task completes. As such, it is subject to the [same old deadlock problems]({% post_url 2012-07-12-dont-block-on-async-code %}) as `Wait` and `Result`. However, it will *not* wrap the task exceptions in an `AggregateException`.

The code above will retrieve the result value from a `Task<T>`. The same code pattern can also be applied to `Task` (without a result value); in this case "GetResult" actually means "check the task for errors":

{% highlight csharp %}
Task task = ...;
task.GetAwaiter().GetResult();
{% endhighlight %}

In general, I try my best to avoid synchronously blocking on an asynchronous task. However, there are a handful of situations where I do violate that guideline. In those rare conditions, my preferred method is `GetAwaiter().GetResult()` because it preserves the task exceptions instead of wrapping them in an `AggregateException`.

## await

Of course, `await` is not a member of the task type; however, I feel it's important to remind today's readers that the *best* way of retrieving results from a [Promise Task]({% post_url 2014-06-05-a-tour-of-task-part-3-status %}) is to merely use `await`. `await` retrieves task results in the most benign manner possible: `await` will *asynchronously* wait (not block); `await` will return the result (if any) for a successful task; and `await` will (re-)throw exceptions for a failed task *without* wrapping them in an `AggregateException`.

In short, `await` should be your go-to option for retrieving task results. The vast majority of the time, `await` should be used instead of `Wait`, `Result`, `Exception`, or `GetAwaiter().GetResult()`.
