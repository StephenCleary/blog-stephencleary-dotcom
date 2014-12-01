---
layout: post
title: "Introduction to Dataflow, Part 3"
---
So far in this series, we've covered [an introduction to some Dataflow concepts]({% post_url 2012-09-20-introduction-to-dataflow-part-1 %}) and [some of the Dataflow blocks that are available]({% post_url 2012-09-27-introduction-to-dataflow-part-2 %}). Today we'll look at some of the details to consider when building dataflow meshes.

## Completing Blocks

I mentioned in [my first Dataflow post]({% post_url 2012-09-20-introduction-to-dataflow-part-1 %}) that completion can be handled by calling [`Complete`](http://msdn.microsoft.com/en-us/library/system.threading.tasks.dataflow.idataflowblock.complete.aspx), which will eventually cause the [`Completion`](http://msdn.microsoft.com/en-us/library/system.threading.tasks.dataflow.idataflowblock.completion.aspx) task to complete. This is the way to indicate successful completion. In a pipeline-style dataflow mesh, you can easily propagate this completion by ensuring the links have their [`PropagateCompletion`](http://msdn.microsoft.com/en-us/library/system.threading.tasks.dataflow.dataflowlinkoptions.propagatecompletion.aspx) option set to `true`.

You can also complete a block with an error indication. To do this, call the block's [`Fault`](http://msdn.microsoft.com/en-us/library/system.threading.tasks.dataflow.idataflowblock.fault.aspx) method. This will drop all currently-buffered items and cause the [`Completion`](http://msdn.microsoft.com/en-us/library/system.threading.tasks.dataflow.idataflowblock.completion.aspx) task to complete in a faulted state. However, if you propagate completion, then each block will wrap the previous block's fault in an `AggregateException`. In a pipeline mesh, this can leave you with a deeply nested exception coming out of the last block.

Finally, you can also cancel a block. Cancellation has similar semantics to faulting: when you cancel a block, it will drop all of its currently-buffered items and then its [`Completion`](http://msdn.microsoft.com/en-us/library/system.threading.tasks.dataflow.idataflowblock.completion.aspx) task will complete in a canceled state. To cancel a block, you must set a [`CancellationToken` in the options when constructing the block](http://msdn.microsoft.com/en-us/library/system.threading.tasks.dataflow.dataflowblockoptions.cancellationtoken.aspx). Cancellation is most useful when shutting down an entire dataflow mesh.

## Block Options: Parallel Processing

As I discussed [last time]({% post_url 2012-09-27-introduction-to-dataflow-part-2 %}), different blocks have different task configurations. Most blocks have at least one task that will spin up to push output data further along in the mesh, and some blocks have other tasks that do processing of the data - most notably [`ActionBlock`](http://msdn.microsoft.com/en-us/library/hh194684.aspx), [`TransformBlock`](http://msdn.microsoft.com/en-us/library/hh194782.aspx), and [`TransformManyBlock`](http://msdn.microsoft.com/en-us/library/hh194784.aspx).

By default, each block will not process more than one item at a time. Each block is independent of other blocks, so one block may process one item while another block is processing another item, but each block limits itself to a single item at once. This behavior can be changed by setting [`MaxDegreeOfParallelism` in the options when constructing the block](http://msdn.microsoft.com/en-us/library/system.threading.tasks.dataflow.executiondataflowblockoptions.maxdegreeofparallelism.aspx). When this is set to a value other than one, the "processing" blocks may process items using multiple tasks at once. The block will take care of ordering the results, so you don't have to worry about parallel processing messing up the sequencing of the data.

## Block Options: Synchronization

By default, each block is independent of other blocks, so one block may process one item while another block is processing another item. This provides some natural parallelism to the mesh as a whole. However, there are some cases where the processing done by one block must be exclusive to the processing done by another block. In these cases, you can specify the [`TaskScheduler` in the options when constructing the block](http://msdn.microsoft.com/en-us/library/system.threading.tasks.dataflow.dataflowblockoptions.taskscheduler.aspx).

This is where the [`ConcurrentExclusiveSchedulerPair`](http://msdn.microsoft.com/en-us/library/system.threading.tasks.concurrentexclusiveschedulerpair.aspx) type (which I have [blogged about before]({% post_url 2012-08-23-async-and-scheduled-concurrency %})) is really useful. By applying the exclusive (or concurrent) scheduler to certain blocks in your dataflow, you can synchronize different parts of your mesh while allowing the rest of your mesh to benefit from the natural parallelism inherent in the default task scheduler.

Remember the standard caveat for `ConcurrentExclusiveSchedulerPair`: it only applies _while the task was executing_. If the block is using an `async` method, it is not considered "in" the scheduler while it is `await`ing.

You can also use the `TaskScheduler` option to execute a block's actions on a specific context captured by [`TaskScheduler.FromCurrentSynchronizationContext`](http://msdn.microsoft.com/en-us/library/system.threading.tasks.taskscheduler.fromcurrentsynchronizationcontext.aspx). E.g., an `ActionBlock` at the end of a mesh to update a user interface.

## Block Options: Throttling Data

Almost every block has at least one buffer. If your dataflow is getting its source data from I/O, you'll probably want to limit the buffering that goes on. Remember: different computers have different performance characteristics, and your computer may have a bottleneck in one part of your mesh while a client computer may have a bottleneck in a different part of your mesh. So, whenever you are looking at a potentially large amount of data, you should consider throttling the data buffers.

Each buffer can be limited by setting the [`BoundedCapacity` in the options when constructing the block](http://msdn.microsoft.com/en-us/library/system.threading.tasks.dataflow.dataflowblockoptions.boundedcapacity.aspx). But the story doesn't end there: you often need to limit _all_ the following buffers in your mesh. A good understanding of the blocks and how they work is necessary to properly throttle data. Later in this series we'll do some simple throttling of a producer/consumer queue, but for your own meshes you should thoroughly read and understand the [Introduction to TPL Dataflow document](http://www.microsoft.com/en-us/download/details.aspx?id=14782).

Data throttling should be used when the data is coming from I/O, but there's another important scenario as well: if you have a "T" in your dataflow mesh, you may need to set `BoundedCapacity` to do load balancing. By default, any block will greedily buffer any data offered to it, so if you have (unfiltered) output going to multiple blocks, you'll want to limit their buffers. We'll look at an example of this a little later in this series.

## Block Options: Throttling CPU Usage

By default, the "processing" blocks spin up one task (or several tasks if you've enabled parallelism) on the thread pool (or a specified scheduler if you're using synchronization). This task will continue running in a loop as long as there is data available. This behavior is efficient, but it can cause some fairness issues if the data is continuous.

To mitigate this, you can set [`MaxMessagesPerTask` in the options when constructing the block](http://msdn.microsoft.com/en-us/library/system.threading.tasks.dataflow.dataflowblockoptions.maxmessagespertask.aspx). As the name implies, this value specifies the number of data items that an individual task will process. If there are more data items, the task will be recycled to continue processing. This is less efficient but more fair to other tasks in the system.

## Further information

This introductory series on Dataflow is just scratching the surface. The resources below have much more information.

- [Document: Introduction to TPL Dataflow](http://www.microsoft.com/en-us/download/details.aspx?id=14782)
- [Document: Guide to Implementing Custom TPL Dataflow Blocks](http://blogs.msdn.com/b/pfxteam/archive/2011/12/05/10244302.aspx)
- [TPL Dataflow forum](http://social.msdn.microsoft.com/Forums/en/tpldataflow/threads)

<div class="alert alert-info" markdown="1">
<i class="fa fa-hand-o-right fa-2x pull-left"></i>

Update (2014-12-01): For more details, see Chapter 4 in my [Concurrency Cookbook](http://tinyurl.com/ConcurrencyCookbook){:.alert-link}.
</div>
