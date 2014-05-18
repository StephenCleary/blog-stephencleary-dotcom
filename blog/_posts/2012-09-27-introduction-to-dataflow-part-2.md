---
layout: post
title: "Introduction to Dataflow, Part 2"
tags: ["async", "Dataflow", ".NET"]
---


[Last time](http://blog.stephencleary.com/2012/09/introduction-to-dataflow-part-1.html), we learned some basic concepts in the TPL Dataflow library. Today, let's look at some blocks in more detail.



## BufferBlock: A Queue



One of the simplest blocks is just a basic FIFO buffer, [`BufferBlock<T>`](http://msdn.microsoft.com/en-us/library/hh160414.aspx). The data that comes in is the data that goes out.





With a block this simple, you might wonder why you would even need it. `BufferBlock` is useful on its own as an `async`-compatible queue. It's also useful in a dataflow mesh when combined with different options (such as throttling) that we'll cover in next week's post.





And, of course, it's a great block to start playing with when you're learning TPL Dataflow.



## ActionBlock: Foreach



Possibly even simpler than `BufferBlock`, [`ActionBlock<T>`](http://msdn.microsoft.com/en-us/library/hh194684.aspx) is just an input buffer combined with a processing task, which executes a delegate for each input item. Conceptually, it's like running a "foreach" loop over the data passing through the block.





A very useful feature of `ActionBlock` is that its delegate may be `async`. By default, the `ActionBlock` will run the delegate to completion for one data item at a time. (We'll take a look next week at how to change these defaults).





`ActionBlock` does not provide any output data items. They are "pushed" to its own delegate, not to another block. As such, it represents the end of a dataflow mesh (unless your delegate posts or sends the data to another block, but that would be unusual).



## NullTarget: /dev/null



OK, [`NullTarget<T>`](http://msdn.microsoft.com/en-us/library/hh462765.aspx) has got to be the simplest block. It just accepts all data items and ignores them.





So why would you use it? Imagine you have a `BufferBlock` linked to an `ActionBlock`, but you [applied a filter when you called `LinkTo`](http://msdn.microsoft.com/en-us/library/hh160311.aspx). If a data item came along not matching the filter, the `ActionBlock` would refuse to take it, but then the `BufferBlock` would hold onto it. The data item would stick there, gumming up the whole system. An easy way to fix this is to link the `BufferBlock` to a second block (`NullTarget`), which would get any leftover data items (the ones rejected by the `ActionBlock`), and ignore them.



## TransformBlock: Select



[`TransformBlock<TInput, TOutput>`](http://msdn.microsoft.com/en-us/library/hh194782.aspx) is like a LINQ `Select` method: conceptually, it is a one-to-one mapping for data items.





You define the mapping function yourself in a delegate. Like `ActionBlock`, this delegate may be `async`. Also like `ActionBlock`, `TransformBlock` will only process one item at a time by default.





Unlike `ActionBlock`, `TransformBlock` does provide an output. So it actually has two buffers (data that has not been processed, and data that has been processed) and two tasks (one to process the data, and one to push data to the next block).



## TransformManyBlock: SelectMany



[`TransformManyBlock<TInput, TOutput>`](http://msdn.microsoft.com/en-us/library/hh194784.aspx) is very similar to `TransformBlock`, except it's a one-to-n mapping for data items. So it's like LINQ's `SelectMany`, where a single input item may result in zero, one, or any number of output items. The results of this mapping are "flattened", just like LINQ's `SelectMany`.





Again, you define the mapping function in a delegate, which may be `async`. And `TransformManyBlock` also processes only one input item at a time by default.





`TransformManyBlock` has a similar internal structure to `TransformBlock`: two buffers and two tasks. The only real difference between the two is that the mapping delegate returns a collection of items, which are inserted individually into the output buffer.



## Advanced Block Types



The blocks described above are a good starting point for playing around with TPL Dataflow, but the library offers much more (which I won't be covering in these intro posts):




- [`WriteOnceBlock<T>`](http://msdn.microsoft.com/en-us/library/hh194820.aspx) - Memorizes its first data item and passes out copies of it as its output. Ignores all other data items.
- [`BatchBlock<T>`](http://msdn.microsoft.com/en-us/library/hh194745.aspx) - Groups a certain number of sequential data items into collections of data items.
- [`BroadcastBlock<T>`](http://msdn.microsoft.com/en-us/library/hh160447.aspx) - Passes out copies of data items as its output. This block is just like `BufferBlock` except that a `BufferBlock` will only send a particular data item to a single block; `BroadcastBlock` will copy the item and send the copies to every block that it's linked to.
- [`JoinBlock<T1, T2>`](http://msdn.microsoft.com/en-us/library/hh194869.aspx) and [`JoinBlock<T1, T2, T3>`](http://msdn.microsoft.com/en-us/library/hh160286.aspx) - Collects two or three inputs and combines them into a `Tuple`.
- [`BatchedJoinBlock<T1, T2>`](http://msdn.microsoft.com/en-us/library/hh194683.aspx) and [`BatchedJoinBlock<T1, T2, T3>`](http://msdn.microsoft.com/en-us/library/hh160326.aspx) - Collects a certain number of total items from two or three inputs and groups them into a `Tuple` of collections of data items.




Please read the [official "Introduction to TPL Dataflow" document](http://www.microsoft.com/en-us/download/details.aspx?id=14782) for more details on these block types; that document covers information like the [option for greedy behavior](http://msdn.microsoft.com/en-us/library/system.threading.tasks.dataflow.groupingdataflowblockoptions.greedy.aspx), which is important for some batching and joining scenarios. Finally, if you're using the advanced blocks, I also recommend also hanging out on the [TPL Dataflow forum](http://social.msdn.microsoft.com/Forums/en/tpldataflow/threads).

