---
layout: post
title: "Async OOP 6: Disposal"
series: "Async OOP"
seriesTitle: "Disposal"
---
Instance disposal, like instance construction, poses an interesting problem when designing `async`-ready types. The question that usually comes up is: how should I handle disposal if my type has asynchronous operations in progress? There are two common options, either (or both) of which may be appropriate for different situations.

## Option 1: Dispose Means Cancel

Historically, disposing a resource on Windows while there are outstanding operations on that resource causes those operations to complete in a canceled state. Translating this pattern into the .NET `async` world means that each `IDisposable` type should have a private `CancellationTokenSource` that is canceled when `Dispose` is called. Internally, every asynchronous operation would use that private `CancellationToken` while also allowing the user to supply their own `CancellationToken` (this is easy to do using [CancellationTokenHelpers.Normalize](http://nitoasyncex.codeplex.com/wikipage?title=CancellationTokenHelpers) or [CancellationTokenSource.CreateLinkedTokenSource](http://msdn.microsoft.com/en-us/library/dd642252.aspx)).

This approach has a synchronous `IDisposable.Dispose` method that can return before the asynchronous operations complete. The asynchronous operations then encounter a race condition: if they were about to complete, then they may complete successfully, faulted, _or_ canceled. This race condition is almost always benign, so you usually don't have to worry about it.

For some situations this works out just fine (e.g., `HttpClient` uses this approach). But some situations cannot accept the fact that `Dispose` returns with operations still in flight; sometimes you _need_ to know when the resource is free, and tracking all the operations all the time would be too messy.

## Option 2: Asynchronous Disposal (Completion)

You can use this approach when you need asynchronous disposal; "dispose" becomes an asynchronous operation. You can't use `IDisposable.Dispose` for this, since `Dispose` must be a synchronous method (OK, technically you could block in `Dispose`, but that's a hack that will cause other problems). Once again, you can turn to Microsoft to see how they handled this situation. As usual, Stephen Toub has been there, done that, and designed the T-shirt.

<div class="alert alert-info" markdown="1">
<i class="fa fa-hand-o-right fa-2x pull-left"></i>

Terminology note: I use the term "asynchronous disposal" for what should really be called "completion" (an asynchronous operation). "Asynchronous disposal" has _nothing_ to do with `IDisposable`. However, I'm sticking with the "asynchronous disposal" term to reduce confusion. E.g., I can say "when the disposal completes" rather than "when the completion completes".
</div>

Consider [ConcurrentExclusiveSchedulerPair](http://msdn.microsoft.com/en-us/library/hh194868.aspx). This is a pair of `Task` schedulers; it's responsible for queueing tasks to run. The scheduler pair instance itself supports asynchronous "disposal"; the semantics are that once "disposal" is requested, no new tasks are accepted by the scheduler pair. However, the already-queued tasks are not canceled; the "disposal" is considered complete once those already-queued tasks have been run.

This permits a nice, clean, `async`-friendly shutdown. The "asynchronous dispose" API for `ConcurrentExclusiveSchedulerPair` looks like this:

{% highlight csharp %}
public class ConcurrentExclusiveSchedulerPair
{
  // Informs the scheduler pair that it should not accept any more tasks.
  public void Complete();

  // Gets a Task that will complete when the scheduler has completed processing.
  public Task Completion { get; }

  ...
}
{% endhighlight %}

This pattern is similar to the [asynchronous initialization pattern]({% post_url 2013-01-17-async-oop-2-constructors %}) covered earlier in this series. With asynchronous initialization, the operation is started in the constructor and the `Initialization` property is used to detect the completion of (and get the results of) the asynchronous initialization. With asynchronous disposal, the operation is started by invoking `Complete` and the `Completion` property is used to detect the completion of (and get the results of) the asynchronous disposal. In both cases, the tasks are properties on the instance because they pertain strongly to the instance; this is a better design than returning a `Task` from the `Complete` method.

The next example takes this pattern a bit further. Blocks in a [TPL Dataflow](http://msdn.microsoft.com/en-us/library/hh228603.aspx) mesh support a clean, cooperative, asynchronous shutdown. Similar to the task scheduler example, dataflow blocks stop receiving input when completion is requested, and will (eventually) complete once all queued data has been processed. Dataflow blocks have an additional twist: as well as completing normally, they can complete in a faulted state. In this case, the dataflow block will stop receiving input and drop all its queued data; however, if there's a piece of data _currently_ being processed, it will gracefully wait for that processing to complete.

Dataflow blocks have this kind of API:

{% highlight csharp %}
public interface IDataflowBlock
{
  // Signals to the IDataflowBlock that it should not accept any more messages.
  void Complete();

  // Causes the IDataflowBlock to complete in a Faulted state.
  void Fault(Exception exception);

  // Gets a Task that represents the asynchronous operation and completion of the dataflow block.
  Task Completion { get; }
}
{% endhighlight %}

<div class="alert alert-info" markdown="1">
<i class="fa fa-hand-o-right fa-2x pull-left"></i>

Aside: Interestingly, these are the _only_ members of `IDataflowBlock`. I [asked Stephen Toub](http://social.msdn.microsoft.com/Forums/en-US/tpldataflow/thread/102885d5-67c0-4287-b5d0-98b8bb5420d8){:.alert-link} about this, and he said that they did consider naming this interface something like `IAsyncCompletable`, but that the `Complete` and `Fault` members don't always make sense for all types.
</div>

So, if you need to support "asynchronous disposal", I recommend following this same pattern. Just like the [asynchronous initialization pattern]({% post_url 2013-01-17-async-oop-2-constructors %}), you may want to define your own marker interface if you have implementations that _might_ need asynchronous disposal.

Types with asynchronous disposal should have at least the `Completion` member, and most likely the `Complete` member. Only add the `Fault` member if it really makes sense for your type. As a final note, instance-level _cancellation_ is handled by passing a `CancellationToken` into the constructor; there's no existing examples of a `Cancel` member for asynchronous disposal. If you do need "external" cancellation like this, consider implementing `IDisposable` with cancellation semantics (as described above) in addition to the standard asynchronous disposal API.

<div class="alert alert-info" markdown="1">
<i class="fa fa-hand-o-right fa-2x pull-left"></i>

Update (2014-12-01): For more details, see Recipe 10.6 in my [Concurrency Cookbook](http://tinyurl.com/ConcurrencyCookbook){:.alert-link}.
</div>
