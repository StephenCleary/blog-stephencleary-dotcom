---
layout: post
title: "Async Producer/Consumer Queue using Dataflow"
---
Today we'll build on what we learned about Dataflow to build an async-compatible producer/consumer queue.



A _producer/consumer queue_ is a classic problem in multithreading: you have one (or more) "producers" which are producing data, you have one (or more) "consumers" which are consuming data, and you need some kind of data structure that will receive data from the producer(s) and provide it to the consumer(s).



Using TPL Dataflow, this is incredibly easy: a `BufferBlock` is an async-ready producer/consumer queue. We'll start with the simple example of a single producer and consumer, and build from there.



Our producer can just enqueue a sequence of values, and then mark the queue as complete:



{% highlight csharp %}private static void Produce(BufferBlock<int> queue, IEnumerable<int> values)
{
    foreach (var value in values)
    {
        queue.Post(value);
    }

    queue.Complete();
}
{% endhighlight %}

Similarly, the consumer can just await until a value is ready in the queue, and then add it to its collection of received values. Note that this works only if we have a single consumer; if we have multiple consumers, then they would all see the output available, but they wouldn't all be able to receive it.



{% highlight csharp %}private static async Task<IEnumerable<int>> Consume(BufferBlock<int> queue)
{
    var ret = new List<int>();
    while (await queue.OutputAvailableAsync())
    {
        ret.Add(await queue.ReceiveAsync());
    }

    return ret;
}
{% endhighlight %}

We can wrap these up in a simple unit test:



{% highlight csharp %}[TestMethod]
public async Task ConsumerReceivesCorrectValues()
{
    // Define the mesh.
    var queue = new BufferBlock<int>();

    // Start the producer and consumer.
    var values = Enumerable.Range(0, 10);
    Produce(queue, values);
    var consumer = Consume(queue);

    // Wait for everything to complete.
    await Task.WhenAll(consumer, queue.Completion);

    // Ensure the consumer got what the producer sent (in the correct order).
    var results = await consumer;
    Assert.IsTrue(results.SequenceEqual(values));
}
{% endhighlight %}

## Throttling

A common requirement for producer/consumer queues is a throttling restriction. We don't want to run out of memory if the producers can produce data items faster than consumers can consume them!



First, we need to change our producer. `Post` will (synchronously) block once the throttling threshold is reached, so we'll switch to the asynchronous `SendAsync` (and make the producer itself asynchronous):



{% highlight csharp %}private static async Task Produce(BufferBlock<int> queue, IEnumerable<int> values)
{
    foreach (var value in values)
    {
        await queue.SendAsync(value);
    }

    queue.Complete();
}
{% endhighlight %}

Dataflow blocks have built-in support for throttling, so adding this to the mesh is rather easy once we have an asynchronous producer. We can say there should never be more than 5 data items in the queue, and it's a one-line change (the line that defines the mesh):



{% highlight csharp %}[TestMethod]
public async Task ConsumerReceivesCorrectValues()
{
    // Define the mesh.
    var queue = new BufferBlock<int>(new DataflowBlockOptions { BoundedCapacity = 5, });

    // Start the producer and consumer.
    var values = Enumerable.Range(0, 10);
    var producer = Produce(queue, values);
    var consumer = Consume(queue);

    // Wait for everything to complete.
    await Task.WhenAll(producer, consumer, queue.Completion);

    // Ensure the consumer got what the producer sent (in the correct order).
    var results = await consumer;
    Assert.IsTrue(results.SequenceEqual(values));
}
{% endhighlight %}

## Multiple Producers

We can have multiple producers pushing data to the same queue (or any dataflow mesh). The only thing we have to change is when the block is completed.



First, we remove the block completion from the producers:



{% highlight csharp %}private static async Task Produce(BufferBlock<int> queue, IEnumerable<int> values)
{
    foreach (var value in values)
    {
        await queue.SendAsync(value);
    }
}
{% endhighlight %}

Now, we can write a simple "producer manager" method that will complete the queue when all producers complete:



{% highlight csharp %}public async Task ProduceAll(BufferBlock<int> queue)
{
    var producer1 = Produce(queue, Enumerable.Range(0, 10));
    var producer2 = Produce(queue, Enumerable.Range(10, 10));
    var producer3 = Produce(queue, Enumerable.Range(20, 10));
    await Task.WhenAll(producer1, producer2, producer3);
    queue.Complete();
}
{% endhighlight %}

The updated test looks like this (note that because we have three independent producers, the order of results is no longer guaranteed):



{% highlight csharp %}[TestMethod]
public async Task ConsumerReceivesCorrectValues()
{
    // Define the mesh.
    var queue = new BufferBlock<int>(new DataflowBlockOptions { BoundedCapacity = 5, });

    // Start the producers and consumer.
    var producers = ProduceAll(queue);
    var consumer = Consume(queue);

    // Wait for everything to complete.
    await Task.WhenAll(producers, consumer, queue.Completion);

    // Ensure the consumer got what the producer sent.
    var results = await consumer;
    Assert.IsTrue(results.OrderBy(x => x).SequenceEqual(Enumerable.Range(0, 30)));
}
{% endhighlight %}

## Multiple Consumers

The consumer side of this example does _work,_ but it's not done in a TPL Dataflowish sort of way. It starts to get more complicated when we consider multiple consumers, because there's no `TryReceiveAsync` available on our block.



Instead of fighting the flow, let's change our consumer side to be TPL Dataflowish. Specifically, we're going to replace the consumer _method_ with a dataflow `ActionBlock`:



{% highlight csharp %}[TestMethod]
public async Task ConsumerReceivesCorrectValues()
{
    var results = new List<int>();

    // Define the mesh.
    var queue = new BufferBlock<int>(new DataflowBlockOptions { BoundedCapacity = 5, });
    var consumerOptions = new ExecutionDataflowBlockOptions { BoundedCapacity = 1, };
    var consumer = new ActionBlock<int>(x => results.Add(x), consumerOptions);
    queue.LinkTo(consumer, new DataflowLinkOptions { PropagateCompletion = true, });

    // Start the producers.
    var producers = ProduceAll(queue);

    // Wait for everything to complete.
    await Task.WhenAll(producers, consumer.Completion);

    // Ensure the consumer got what the producer sent.
    Assert.IsTrue(results.OrderBy(x => x).SequenceEqual(Enumerable.Range(0, 30)));
}
{% endhighlight %}

Notice that we set the `ExecutionDataflowBlockOptions.BoundedCapacity` for the consumer block to `1`. This is necessary if we want to maintain throttling. Without this set, the producers could produce tons of data items which pass through the queue block and get buffered up in the consumer block (making our queue throttling meaningless).



Now that we have a consumer block, it's much more straightforward to add multiple consumers:



{% highlight csharp %}[TestMethod]
public async Task ConsumerReceivesCorrectValues()
{
    var results1 = new List<int>();
    var results2 = new List<int>();
    var results3 = new List<int>();

    // Define the mesh.
    var queue = new BufferBlock<int>(new DataflowBlockOptions { BoundedCapacity = 5, });
    var consumerOptions = new ExecutionDataflowBlockOptions { BoundedCapacity = 1, };
    var consumer1 = new ActionBlock<int>(x => results1.Add(x), consumerOptions);
    var consumer2 = new ActionBlock<int>(x => results2.Add(x), consumerOptions);
    var consumer3 = new ActionBlock<int>(x => results3.Add(x), consumerOptions);
    var linkOptions = new DataflowLinkOptions { PropagateCompletion = true, };
    queue.LinkTo(consumer1, linkOptions);
    queue.LinkTo(consumer2, linkOptions);
    queue.LinkTo(consumer3, linkOptions);

    // Start the producers.
    var producers = ProduceAll(queue);

    // Wait for everything to complete.
    await Task.WhenAll(producers, consumer1.Completion, consumer2.Completion, consumer3.Completion);

    // Ensure the consumer got what the producer sent.
    var results = results1.Concat(results2).Concat(results3);
    Assert.IsTrue(results.OrderBy(x => x).SequenceEqual(Enumerable.Range(0, 30)));
}
{% endhighlight %}

Note that `ExecutionDataflowBlockOptions.BoundedCapacity` is now performing another important function: in addition to maintaining the throttling, it is performing load balancing. If this is left at the default value (`DataflowBlockOptions.Unbounded`), then all of the data items will end up in the first consumer, which will buffer them up until it can process them. With the buffer limited to a single data item, the queue will offer its item to the next consumer when the first consumer is busy.



In summary, we just reviewed two scenarios where we should set `BoundedCapacity` to a low number: when we want to maintain throttling throughout a pipeline, and when we have a "T" in our dataflow mesh.


