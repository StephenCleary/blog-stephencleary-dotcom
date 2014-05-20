---
layout: post
title: "Introduction to Dataflow, Part 1"
---
So far, we've been learning quite a bit about the core `async` / `await` support that was added to C# in VS2012. Today, we'll start with a conceptual overview of the first large, useful library built with `async` in mind: [TPL Dataflow]( http://msdn.microsoft.com/en-us/devlabs/gg585582.aspx).

TPL Dataflow allows you to easily create a _mesh_ through which your data flows. The simplest meshes are pipelines (very similar to pipelines in PowerShell). More complex meshes can split and join the data flows, and even contain data flow loops!

Every mesh is composed of a number of _blocks_ which are linked together. TPL Dataflow provides quite a few blocks which address different needs; we will just use the most basic blocks for our examples, but you can see the [Introduction to TPL Dataflow document](http://www.microsoft.com/en-us/download/details.aspx?id=14782) for a full description of the different types of blocks.

## The Block

A block is a part of a dataflow mesh through which data can flow. The block usually processes the data in some way, but it doesn't have to.

Blocks themselves have components: usually a _buffer_ component and a _task_ component. Buffers hold data (either data that has been sent to this block but which has not yet been processed, or data that has been processed and is waiting to leave the block).

Different block types have different configurations of buffers and tasks. Some blocks have multiple buffers (e.g., "join" blocks). Tasks have two purposes. Most blocks use a task to push data out of the block; some blocks also use a task to do the processing of the data.

## Basic Data Flow

You can push data to a block by calling [`Post(item)`](http://msdn.microsoft.com/en-us/library/hh194836.aspx) or [`await SendAsync(item)`](http://msdn.microsoft.com/en-us/library/system.threading.tasks.dataflow.dataflowblock.sendasync.aspx). Normally, you could just use `Post(item)` to (synchronously) place the data into the block's input buffer. However, it's possible to throttle a block by limiting its buffer size; in this case, you could use `SendAsync` to (asynchronously) wait for space to be available and then place the data into the block's input buffer.

Getting data out at first appears pretty easy: you can call [`Receive`](http://msdn.microsoft.com/en-us/library/system.threading.tasks.dataflow.dataflowblock.receive.aspx), [`TryReceive`](http://msdn.microsoft.com/en-us/library/hh194808.aspx), or [`ReceiveAsync`](http://msdn.microsoft.com/en-us/library/system.threading.tasks.dataflow.dataflowblock.receiveasync.aspx) to retrieve the next item, and there's an [`OutputAvailableAsync`](http://msdn.microsoft.com/en-us/library/system.threading.tasks.dataflow.dataflowblock.outputavailableasync.aspx) that lets you know when the next item is ready to be retrieved. However, there are a couple of "gotchas":

1. When a block finishes processing, `Receive` and `await ReceiveAsync` will both throw exceptions. This is not ideal.
1. `OutputAvailableAsync` is not very useful if there are multiple receivers reading from the same block.

From a procedural perspective, it would be great if we had a `Tuple<bool, T> TryReceiveAsync` that would either retrieve the item or return false, rather than raising an exception.

But if you look at the problem from a _dataflow_ perspective, there is already a solution: [`ActionBlock<T>`](http://msdn.microsoft.com/en-us/library/hh194684.aspx) will execute a callback for any data pushed to it. This is the "dataflowish" way of getting the results out of a dataflow mesh: just _link_ your block to an `ActionBlock`.

## Linking

You can connect two blocks by linking them together. The actual negotiation of data transfer is fairly complex (to allow scenarios such as "join" blocks) - but you usually don't have to think about it. Just link from source to target ([`source.LinkTo(target);`](http://msdn.microsoft.com/en-us/library/hh160311.aspx)), and the library takes care of propagating the data.

There's no limit to what you can link. It is possible to have a loop in your dataflow mesh.

There are several options you have when setting up a link. You can have the link disengage after so many data items, or specify a filter so only certain data items will be propagated along the link, etc.

A lot of my dataflow "meshes" end up being "pipelines", so one option I use more than others is [`PropagateCompletion`](http://msdn.microsoft.com/en-us/library/system.threading.tasks.dataflow.dataflowlinkoptions.propagatecompletion.aspx), which propagates completion as well as data items.

## Completion

Eventually, you're going to finish sending data to your dataflow mesh, and you're going to want to know when the mesh is done processing it. Each block supports an asynchronous form of completion: you call [`Complete`](http://msdn.microsoft.com/en-us/library/system.threading.tasks.dataflow.idataflowblock.complete.aspx) and some time later, the block's [`Completion`](http://msdn.microsoft.com/en-us/library/system.threading.tasks.dataflow.idataflowblock.completion.aspx) task will complete.

> Side note: you should _always_ assume that `Completion` might be signaled asynchronously, even if there are no data items to process.

If you have a simple dataflow mesh (like a pipeline), then you can tell the blocks to propagate their completion when you link them together. Then when you're finished, you can just complete the first block and await the completion of the last block.

If your dataflow mesh is more complex, then you may have to propagate completion manually. The common tools for this are [`Task.WhenAll`](http://msdn.microsoft.com/en-us/library/hh160384.aspx) and [`Task.ContinueWith`](http://msdn.microsoft.com/en-us/library/system.threading.tasks.task.continuewith.aspx). If you do have to do this, I recommend that you wrap your dataflow mesh into a separate class (possibly exposing the blocks as properties) and implement your own `Complete` and `Completion` members just like a dataflow block does.

