---
layout: post
title: "Async Producer-Consumer Queue 3: More Flexibility"
---
[Last time]({% post_url 2012-12-20-async-producer-consumer-queue-2-more %}) we implemented an `async`-compatible producer/consumer queue using portable `async`-ready synchronization primitives. This time, we'll give up some of that portability but increase flexibility: we'll build an `async`-compatible producer/consumer collection that can be used as a queue (FIFO), stack (LIFO), or bag (unordered).

The BCL has already done some of the hard work for us here. The [BlockingCollection](http://msdn.microsoft.com/en-us/library/dd267312.aspx) type provides a wrapper around any kind of [IProducerConsumerCollection](http://msdn.microsoft.com/en-us/library/dd287147.aspx), including [ConcurrentQueue](http://msdn.microsoft.com/en-us/library/dd267265.aspx), [ConcurrentStack](http://msdn.microsoft.com/en-us/library/dd267331.aspx), and [ConcurrentBag](http://msdn.microsoft.com/en-us/library/dd381779.aspx). Our goal is to create the `async`-compatible equivalent of `BlockingCollection`.

Actually, it's pretty simple. It's almost exactly like [our last implementation]({% post_url 2012-12-20-async-producer-consumer-queue-2-more %}), which wrapped a `Queue`. This time, we wrap an `IProducerConsumerCollection`:

{% highlight csharp %}
public sealed class AsyncCollection<T>
{
    // The underlying collection of items.
    private readonly IProducerConsumerCollection<T> collection;

    // The maximum number of items allowed.
    private readonly int maxCount;

    // Synchronization primitives.
    private readonly AsyncLock mutex;
    private readonly AsyncConditionVariable notFull;
    private readonly AsyncConditionVariable notEmpty;

    public AsyncCollection(IProducerConsumerCollection<T> collection = null, int maxCount = int.MaxValue)
    {
        if (maxCount <= 0)
            throw new ArgumentOutOfRangeException("maxCount", "The maximum count must be greater than zero.");
        this.collection = collection ?? new ConcurrentQueue<T>();
        this.maxCount = maxCount;

        mutex = new AsyncLock();
        notFull = new AsyncConditionVariable(mutex);
        notEmpty = new AsyncConditionVariable(mutex);
    }

    // Convenience properties to make the code a bit clearer.
    private bool Empty { get { return collection.Count == 0; } }
    private bool Full { get { return collection.Count == maxCount; } }

    public async Task AddAsync(T item)
    {
        using (await mutex.LockAsync())
        {
            while (Full)
                await notFull.WaitAsync();

            if (!collection.TryAdd(item))
                throw new InvalidOperationException("The underlying collection refused the item.");
            notEmpty.NotifyOne();
        }
    }

    public async Task<T> TakeAsync()
    {
        using (await mutex.LockAsync())
        {
            while (Empty)
                await notEmpty.WaitAsync();

            T ret;
            if (!collection.TryTake(out ret))
                throw new InvalidOperationException("The underlying collection refused to provide an item.");
            notFull.NotifyOne();
            return ret;
        }
    }
}
{% endhighlight %}

Now we have an `AsyncCollection` that can be used as a front for many different kinds of collections.

