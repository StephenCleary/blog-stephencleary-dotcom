---
layout: post
title: "Async Producer-Consumer Queue 2: More Portability"
---
In my [last Dataflow post]({% post_url 2012-11-08-async-producerconsumer-queue-using %}), I implemented an `async`-compatible producer/consumer queue using `BufferBlock<T>`. While this approach works, it's limited to platforms that Dataflow supports (as of the time of this writing: .NET 4.5, .NET 4.0, Windows Store, and Portable net45+win8).

The [Microsoft.Bcl.Async](https://nuget.org/packages/Microsoft.Bcl.Async) package extends `async` support to .NET 4.5/4.0, Windows Store, Silverlight 5/4, Windows Phone 8.0/7.5, and portable libraries targeting any subset of these. Today we'll build an `async`-compatible producer/consumer queue that is supported on all those same platforms. This would be useful, e.g., for a portable library that had to target both Windows Store and Windows Phone.

Traditional producer/consumer queues are easily built using locks and condition variables (or monitors). I want the ability to throttle the queue with a maximum number of elements, so my implementation will use one lock and two condition variables. One condition variable will be signaled when the queue is not full (releasing a producer), and the other condition variable will be signaled when the queue is not empty (releasing a consumer).

Here's a simple implementation using `async`-compatible synchronization primitives from [AsyncEx](http://nitoasyncex.codeplex.com/) (which supports all the same targets as Microsoft.Bcl.Async):

{% highlight csharp %}
public sealed class AsyncProducerConsumerQueue<T>
{
    // The underlying queue of items.
    private readonly Queue<T> queue;

    // The maximum number of items allowed.
    private readonly int maxCount;

    // Synchronization primitives.
    private readonly AsyncLock mutex;
    private readonly AsyncConditionVariable notFull;
    private readonly AsyncConditionVariable notEmpty;

    public AsyncProducerConsumerQueue(int maxCount = int.MaxValue)
    {
        if (maxCount <= 0)
            throw new ArgumentOutOfRangeException("maxCount", "The maximum count must be greater than zero.");
        queue = new Queue<T>();
        this.maxCount = maxCount;

        mutex = new AsyncLock();
        notFull = new AsyncConditionVariable(mutex);
        notEmpty = new AsyncConditionVariable(mutex);
    }

    // Convenience properties to make the code a bit clearer.
    private bool Empty { get { return queue.Count == 0; } }
    private bool Full { get { return queue.Count == maxCount; } }

    Task EnqueueAsync(T item)
    {
        using (await mutex.LockAsync())
        {
            while (Full)
                await notFull.WaitAsync();

            queue.Add(item);
            notEmpty.NotifyOne();
        }
    }

    Task<T> DequeueAsync()
    {
        using (await mutex.LockAsync())
        {
            while (Empty)
                await notEmpty.WaitAsync();

            var ret = queue.Remove();
            notFull.NotifyOne();
            return ret;
        }
    }
}
{% endhighlight %}

Most of this is boilerplate. The interesting parts are in `EnqueueAsync` and `DequeueAsync`. They both parallel each other: take the lock, wait until their operation can be performed, perform the operation, and notify the other side that the other condition is met.

One question that comes up with condition variables is **why use a while loop instead of just an if statement?** It's actually because the condition may not be true by the time the method resumes! Consider this sequence of events (keeping in mind that [waiting on a condition variable temporarily releases the lock, and re-acquires it after the condition variable is signaled](http://nitoasyncex.codeplex.com/wikipage?title=AsyncConditionVariable)):

1. One producer is attempting to enqueue an item to a full queue. It waits on the `notFull` condition variable.
1. A consumer takes an item from the queue, signaling `notFull` and releasing the lock.
1. The producer is signaled and is about to re-acquire the lock when it is suspended.
1. Another producer calls `EnqueueAsync`, takes the lock, sees that the queue is not full, enqueues its item, and completes, releasing the lock.
1. The original producer resumes execution and re-acquires the lock. However, the queue is now full again.

This is a very unlikely scenario, but it _is_ possible. For this reason, condition variables are almost always used with `while` loops. There may be occasional situations where an `if` statement would suffice (e.g., if there is only one producer and it only produced one item at a time), but it's barely an optimization at all. It's safer to always use `while` loops with condition variables.

There is an implementation of this type in the [AsyncEx library](http://nitoasyncex.codeplex.com) which is more complex than the simple version in this blog post. The AsyncEx version includes:

 - Full cancellation support.
 - Marking a queue as "complete for adding". Any attempt to enqueue to a queue that is complete for adding will fail. Attempts to dequeue from a queue that is complete for adding will also fail once the queue is empty.
 - The ability to attempt an enqueue/dequeue to/from multiple queues simultaneously, with only one enqueue/dequeue actually taking place.
 - `Try*` variants for all operations.

<div class="alert alert-info" markdown="1">
<i class="fa fa-hand-o-right fa-2x pull-left"></i>

Update (2014-12-01): For more details, see Recipes 8.8 and 8.10 in my [Concurrency Cookbook]({{ '/book/' | prepend: site.url_www }}){:.alert-link}.
</div>
