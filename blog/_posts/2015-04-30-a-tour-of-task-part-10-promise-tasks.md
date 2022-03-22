---
layout: post
title: "A Tour of Task, Part 10: Promise Tasks"
series: "A Tour of Task"
seriesTitle: "Promise Tasks"
description: "An analysis of Task.Delay, Task.Yield, and Task.FromResult; and discussion of whether they should be used for asynchronous and/or parallel code."
---

Last time, we looked at [ways to start Delegate Tasks]({% post_url 2015-03-03-a-tour-of-task-part-9-delegate-tasks %}). Today we'll look at the most common ways to create Promise Tasks. As a reminder, Promise Tasks are tasks that represent a kind of "event" within a system; they don't have any user-defined code to execute.

## Task.Delay

`Task.Delay` is the asynchronous equivalent of `Thread.Sleep`.

{% highlight csharp %}
Task Delay(int);
Task Delay(TimeSpan);

Task Delay(int, CancellationToken);
Task Delay(TimeSpan, CancellationToken);
{% endhighlight %}

The `int` argument is treated as a number of milliseconds; I usually prefer the `TimeSpan` versions since they are more explicit. Using `int` millisecond values for timeouts is a holdover from an older API design; many Win32-level APIs only take timeout values as integer milliseconds. So, it makes sense to expose an `int` parameter for lower-level waits like `WaitHandle.WaitOne` or even `Task.Wait`. However, `Task.Delay` isn't a thin wrapper over any Win32 API; the `int` parameter in this case is just provided for tradition.

`Delay` may also take a `CancellationToken`, which allows the delay to be cancelled.

Under the hood, `Delay` starts a timer and completes its returned task when that timer fires. Or, if the `CancellationToken` is signaled first, then `Delay` cancels its returned task.

In real-world code, `Delay` is almost never used. Its primary use case is as a retry timeout, i.e., if an asynchronous operation failed, the code will (asynchronously) wait a period of time before trying again. Generally, retry logic is wrapped into a separate library (such as [Transient Fault Handling](https://msdn.microsoft.com/en-us/library/hh675232.aspx?WT.mc_id=DT-MVP-5000058) or [Polly](https://github.com/michael-wolfenden/Polly)), and `Delay` is only used internally by those libraries, not directly by application code.

## Task.Yield

`Task.Yield` has several interesting aspects. To begin with, it doesn't actually return a `Task`, so it's not really a Promise Task kind of method:

{% highlight csharp %}
YieldAwaitable Yield();
{% endhighlight %}

But it does kind of *act* kind of like a Promise Task. The `YieldAwaitable` type interacts with the `async` compiler transformation to *force* an asynchronous point within a method. By default, if `await` is used on an operation that has already completed, then the execution of the `async` method continues synchronously. `YieldAwaitable` throws a wrench into this by always claiming it is *not* completed, and then scheduling its continuations immediately. This causes `await` to schedule the rest of the `async` method for immediate execution and return.

I've used `Task.Yield` only occasionally during unit testing, when I needed to ensure that a particular method would in fact work if its asynchronous operation did not complete synchronously. I've found `Yield` most useful when the asynchronous operation in question normally *does* complete synchronously, and I need to force asynchrony to ensure the method behavior is correct.

However, I've never needed `Yield` in production code. There is one use case where developers sometimes (incorrectly) attempt to use `Yield`: to try to "refresh" the UI.

{% highlight csharp %}
// Bad code, do not use!!
async Task LongRunningCpuBoundWorkAsync()
{
  // This method is called directly from the UI, and
  //  does lots of CPU-bound work.
  // Since this blocks the UI, this method is given
  //  an async signature and periodically "yields".
  for (int i = 0; i != 1000000; ++i)
  {
    ... // CPU-bound work.
    await Task.Yield();
  }
}
{% endhighlight %}

However, this approach will not work. The reason is that UI message loops are *priority* queues, and any scheduled continuations have a much higher priority than "repaint the window". So, the `Yield` schedules the continuation and returns to the message loop, and the message loop immediately executes that continuation without processing any of its `WM_PAINT` messages.

Some developers have discovered that using `Task.Delay` instead of `Task.Yield` will allow message processing (messages are processed until the timer fires). However, a far cleaner solution is to do the CPU-bound work on a background thread:

{% highlight csharp %}
void LongRunningCpuBoundWork()
{
  for (int i = 0; i != 1000000; ++i)
  {
    ... // CPU-bound work.
  }
}

// Called as:
await Task.Run(() => LongRunningCpuBoundWork());
{% endhighlight %}

In conclusion, `Task.Yield` is occasionally useful when unit testing, but much less so for production code.

## Task.FromResult

`Task.FromResult` will create a *completed* task with the specified value:

{% highlight csharp %}
Task<TResult> FromResult<TResult>(TResult);
{% endhighlight %}

It might seem silly at first to return a task that is already completed, but this is actually useful in several scenarios.

For instance, an interface method may have an asynchronous (task-returning) signature, and if an implementation is synchronous, then it can use `Task.FromResult` to wrap up its (synchronous) result within a task. This is particularly useful when creating stubs for unit testing, but is also occasionally useful in production code:

{% highlight csharp %}
interface IMyInterface
{
  // Implementations *might* need to be asynchronous,
  //  so we define an asynchronous API.
  Task<int> DoSomethingAsync();
}

class MyClass : IMyInterface
{
  // This particular implementation is not asynchronous.
  public Task<int> DoSomethingAsync()
  {
    int result = 42; // Do synchronous work.
    return Task.FromResult(result);
  }
}
{% endhighlight %}

Be careful, though, that your synchronous implementation is not *blocking*. Implementing an asynchronous API with a blocking method is surprising behavior.

Another use case of `Task.FromResult` is when doing some form of caching. In this case, you have some data that is synchronously retrieved (from the cache), and need to return it directly. In the case of a cache miss, then a true asynchronous operation is performed:

{% highlight csharp %}
public Task<string> GetValueAsync(int key)
{
  string result;
  if (cache.TryGetValue(key, out result))
    return Task.FromResult(result);
  return DoGetValueAsync(key);
}

private async Task<string> DoGetValueAsync(int key)
{
  string result = await ...;
  cache.TrySetValue(key, result);
  return result;
}
{% endhighlight %}

<div class="alert alert-info" markdown="1">
<i class="fa fa-hand-o-right fa-2x pull-left"></i>

Tip: If you can, cache the task objects themselves instead of their resulting values; maintain a cache of *operations* rather than *results*.
</div>

As of this writing, one final common use of `Task.FromResult` is just as a completed task. For this, the expressions `Task.FromResult(0)` or `Task.FromResult<object>(null)` are commonly used. This use case is similar to the synchronous implementation of an asynchronous API:

{% highlight csharp %}
interface IPlugin
{
  // Permit each plugin to initialize asynchronously.
  Task InitializeAsync();
}

class MyPlugin : IPlugin
{
  public Task InitializeAsync()
  {
    // The async equivalent of a noop.
    return Task.FromResult<object>(null);
  }
}
{% endhighlight %}

<div class="alert alert-info" markdown="1">
<i class="fa fa-hand-o-right fa-2x pull-left"></i>

In the preview builds of .NET 4.6, there is a static `Task.CompletedTask` that should be used instead of `Task.FromResult(0)` or `Task.FromResult<object>(null)`.
</div>

You might be wondering if there's a way to return already-completed tasks in other states - particularly, canceled or faulted tasks. As of now, you have to write this yourself (using `TaskCompletionSource`), but .NET 4.6 will introduce the `Task.FromCanceled` and `Task.FromException` methods to return synchronously canceled or faulted tasks.
