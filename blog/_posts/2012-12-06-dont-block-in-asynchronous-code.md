---
layout: post
title: "Don't Block in Asynchronous Code"
---
One of my most famous blog posts is [Don't Block on Asynchronous Code]({% post_url 2012-07-12-dont-block-on-async-code %}), which took an in-depth look at how a synchronous method could deadlock if it blocked on asynchronous code (e.g., using `Task.Wait` or `Task.Result`). This is a fairly common beginner's mistake.

Recently, I came across another deadlock situation: in some cases, an `async` method may deadlock if it blocks on a `Task`. I found this behavior surprising and [reported it as a bug](https://connect.microsoft.com/VisualStudio/feedback/details/769322/waiting-on-task-can-deadlock-in-free-threaded-context). I suspect it won't be fixed because it's a very uncommon situation and the easiest fix would have a negative impact on performance for _all_ `async` code.

<div class="alert alert-danger" markdown="1">
<i class="fa fa-exclamation-triangle fa-3x pull-left"></i>

This deadlock scenario is due to an undocumented implementation detail. This post is accurate as of the initial release of .NET 4.5 in 2012. Microsoft may change this behavior in the future.
</div>

This code will deadlock in a free-threaded context (e.g., a Console application, unit test, or `Task.Run`):

{% highlight csharp %}
// Creates a new task on the thread pool and waits for it.
// This method will deadlock if called in a free-threaded context.
static async Task Test()
{
  // Indicates the task has been started and is ready.
  var taskReady = new TaskCompletionSource<object>();

  // Start the task, running on a thread pool thread.
  var task = Task.Run(() =>
  {
    // Spend a bit of time getting ready.
    Thread.Sleep(100);

    // Let the Test method know we've been started and are ready.
    taskReady.SetResult(null);

    // Spend a bit more time doing nothing in particular.
    Thread.Sleep(100);
  });

  // Wait for the task to be started and ready.
  await taskReady.Task;

  // Block until the task is completed.
  task.Wait();
}
{% endhighlight %}

Do you see the problem? I didn't.

The deadlock is due to an optimization in the implementation of `await`: **an `async` method's continuation is scheduled with `TaskContinuationOptions.ExecuteSynchronously`**.

So, stepping through the example code:

1. We kick off a task running on the thread pool. So far, so good.
1. The thread pool task does a bit of "work". This is just to make sure `taskReady` is awaited before we call `SetResult`.
1. Meanwhile, the `Test` method continues running and awaits `taskReady`.
1. After a short time, the thread pool task completes its "work" and invokes `SetResult`. This is where things get interesting! `Test` is _already awaiting_ `taskReady` **and** its continuation is expecting to run in a thread pool context. In this case, `SetResult` will _not_ asynchronously schedule the continuation; it will execute it directly.
1. The `Test` method continues execution, only it's no longer independent from the thread pool task. Instead, `Test` is executing on that same thread pool thread. So when we proceed to `Wait` on the thread pool task, we are blocking on something that we're supposed to be completing.
1. As a result, the last `Sleep` never actually runs. The thread pool task never completes, and `Test` never completes.

If you place this same method in a GUI project or ASP.NET project, you won't see a deadlock. The difference is in step 4: the continuation must be run in the captured `SynchronizationContext`, but it's being scheduled by a thread pool thread; so in this case `SetResult` will schedule the continuation to run asynchronously instead of executing it directly.

One fun twist to this scenario is that if we use `ConfigureAwait`, then the method will _consistently_ deadlock, regardless of its initial context:

{% highlight csharp %}
// Creates a new task on the thread pool and waits for it.
// This method will always deadlock.
static async Task Test()
{
  // Indicates the task has been started and is ready.
  var taskReady = new TaskCompletionSource<object>();

  // Start the task, running on a thread pool thread.
  var task = Task.Run(() =>
  {
    // Spend a bit of time getting ready.
    Thread.Sleep(100);

    // Let the Test method know we've been started and are ready.
    taskReady.SetResult(null);

    // Spend a bit more time doing nothing in particular.
    Thread.Sleep(100);
  });

  // Wait for the task to be started and ready.
  await taskReady.Task.ConfigureAwait(continueOnCapturedContext: false);

  // Block until the task is completed.
  task.Wait();
}
{% endhighlight %}

Most people would not write code like this. It's very unnatural to call `Task.Wait` in an `async` method; the natural code would use `await` instead. I only came across this behavior while writing unit tests for my [AsyncEx](http://nitoasyncex.codeplex.com/) library; these unit tests can get pretty complex and can involve a mixture of synchronous and asynchronous code.

{:.center}
![]({{ site_url }}/assets/Method%2Bno%2BBlocking.png)  

In conclusion, we already knew [not to block **on** asynchronous code]({% post_url 2012-07-12-dont-block-on-async-code %}); now we know not to block **in** asynchronous code either!

