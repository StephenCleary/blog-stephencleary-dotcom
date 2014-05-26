---
layout: post
title: "Async and Scheduled Concurrency"
---
Async doesn't play well with traditional synchronization primitives. For example, [think about what happens when you await while holding a lock](http://stackoverflow.com/questions/7612602/why-cant-i-use-the-await-operator-within-the-body-of-a-lock-statement): the async method will _return_ to its caller, allowing other code to run on that thread while the lock is held. If the method later resumes on the same thread (e.g., a UI context), and the "other code" attempts to take the lock, then you have a deadlock. Another case is if the method resumes on a different thread (e.g.,  ConfigureAwait(false)); in that case the new thread will attempt to release a lock it doesn't have.

The compiler will prevent await from within a lock{}, but it's not omnipotent; async code has similar problems with Semaphore, ManualResetEvent, or really any other kind of traditional synchronization primitive that assumes thread affinity and works by blocking.

<div class="alert alert-info" markdown="1">
<i class="fa fa-hand-o-right fa-2x pull-left"></i>

You _can_ use the traditional primitives as "building blocks". E.g., ConcurrentDictionary will use locks internally, and it can be safely used from async code. This is permitted because all the code within the locks is synchronous; there is no way for the async method to yield while holding one of ConcurrentDictionary's internal locks.
</div>

## Async Concurrency Options

The best option is to restructure your code so that concurrency is removed. Instead of having different methods or different objects contending over the same state, focus on the data being moved. Often, you can use [TPL Dataflow](http://msdn.microsoft.com/en-us/devlabs/gg585582.aspx) or [Rx](http://msdn.microsoft.com/en-us/data/gg577609.aspx) to represent the logic more naturally, leaving all concurrency internal to those libraries. TPL Dataflow is built on Tasks and plays very well with async. Rx is a bit different but it also plays well with async.

<div class="alert alert-info" markdown="1">
<i class="fa fa-hand-o-right fa-2x pull-left"></i>

This is yet another example of async code gently pushing you towards a functional programming style.
</div>

The first option isn't always feasible, though. If you want minimal impact to your existing code, you can check out Stephen Toub's series where he creates async-friendly versions of [ManualResetEvent](http://blogs.msdn.com/b/pfxteam/archive/2012/02/11/10266920.aspx), [AutoResetEvent](http://blogs.msdn.com/b/pfxteam/archive/2012/02/11/10266923.aspx), [CountdownEvent](http://blogs.msdn.com/b/pfxteam/archive/2012/02/11/10266930.aspx), [Barrier](http://blogs.msdn.com/b/pfxteam/archive/2012/02/11/10266932.aspx), [Semaphore](http://blogs.msdn.com/b/pfxteam/archive/2012/02/12/10266983.aspx), [lock](http://blogs.msdn.com/b/pfxteam/archive/2012/02/12/10266988.aspx), and [ReaderWriterLock](http://blogs.msdn.com/b/pfxteam/archive/2012/02/12/building-async-coordination-primitives-part-7-asyncreaderwriterlock.aspx). There's some tremendous knowledge in these posts, even if you don't use the code.

Today, though, I'm going to explore a third option: **scheduled concurrency**. A lot of people aren't even aware that it's an option.

## Async Scheduled Concurrency

By default, [when an async method resumes after awaiting a Task, it resumes in a "context"]({% post_url 2012-02-02-async-and-await %}). I'm always careful not to say "synchronization context" there; the actual async context is SynchronizationContext _unless it is null_, in which case the async context is the current TaskScheduler.

So if we want to control the async context, one option is to implement a custom SynchronizationContext. That's somewhat painful. It does have its uses (e.g., async unit testing), but it's not something we want to do a lot.

The other option - using a custom TaskScheduler - is what we'll be considering today. Most of the time, you'll only need to specify the TaskScheduler when creating the highest-level async Task. By default, the async context (including our TaskScheduler) will be inherited by all the async methods in the call tree.

<div class="alert alert-info" markdown="1">
<i class="fa fa-hand-o-right fa-2x pull-left"></i>

Remember, resuming the async context after an await is **by default**. You can always jump out of your TaskScheduler by awaiting ConfigureAwait(false).
</div>

There are two important TaskSchedulers you should know about. The first one is TaskScheduler.Default, which schedules tasks to the ThreadPool. That one's not very interesting because it's the default behavior if we don't provide a custom TaskScheduler.

The other TaskScheduler is very interesting. It's new in .NET 4.5 and goes by the name of [ConcurrentExclusiveScheculerPair](http://msdn.microsoft.com/en-us/library/system.threading.tasks.concurrentexclusiveschedulerpair(v=VS.110).aspx). It's actually a pair of schedulers: one concurrent and one exclusive. It acts like a reader/writer lock, only at the scheduler level. So instead of blocking a task (synchronously via ReaderWriterLock or asynchronously via [AsyncReaderWriterLock](http://blogs.msdn.com/b/pfxteam/archive/2012/02/12/building-async-coordination-primitives-part-7-asyncreaderwriterlock.aspx)), it simply doesn't execute the task until it's permitted to run.

<div class="alert alert-info" markdown="1">
<i class="fa fa-hand-o-right fa-2x pull-left"></i>

As of this writing, the .NET 4.5 docs are a bit lacking. For more information on the design of ConcurrentExclusiveSchedulerPair, see [this blog post by Stephen Toub](http://blogs.msdn.com/b/pfxteam/archive/2010/04/08/9990422.aspx){:.alert-link} (who else?) or [the original implementation](http://code.msdn.microsoft.com/Samples-for-Parallel-b4b76364/sourcecode?fileId=44488&pathId=2072038893){:.alert-link}. At that time, it was called ConcurrentExclusiveInterleave.
</div>

ConcurrentExclusiveSchedulerPair actually handles any mixture of these scenarios:

- Reader/writer locks (obviously). Schedule reader tasks to ConcurrentScheduler and writer tasks to ExclusiveScheduler.
- Plain 'ol locks (obviously). Schedule all tasks to ExclusiveScheduler.
- Limited concurrency. You can pass a parameter to ConcurrentExclusiveSchedulerPair to indicate how many concurrent tasks can be scheduled on the ConcurrentScheduler at a time. Then schedule all tasks to ConcurrentScheduler.

It's also possible to define your own special TaskScheduler, if you really need one. E.g., a UI scheduler that gave its tasks a low dispatcher priority, or an STA scheduler for COM interop.

<div class="alert alert-danger" markdown="1">
<i class="fa fa-exclamation-triangle fa-2x pull-left"></i>

**Note:** When an asynchronous method awaits, it returns back to its context. This means that ExclusiveScheduler is perfectly happy to run one task _at a time_, not one task _until it completes_. As soon as an asynchronous method awaits, it's no longer the "owner" of the ExclusiveScheduler. Stephen Toub's async-friendly primitives like [AsyncLock](http://blogs.msdn.com/b/pfxteam/archive/2012/02/12/10266988.aspx){:.alert-link} use a different strategy, allowing an asynchronous method to hold the lock while it awaits.
</div>

<!--

<h4>Schedulers, Schedulers, Everywhere!</h4>

<p>Schedulers can actually do much more than just synchronization. They can also specify a context.</p>

<p>The most obvious example is TaskScheduler.FromCurrentSynchronizationContext, which is a TaskScheduler that schedules tasks on the current SynchronizationContext. Await does't use this scheduler because it will use SynchronizationContext directly if it is present.</p>

<p>What about other contexts? Stephen Toub (again) has been there and done that with his <a href="http://blogs.msdn.com/b/pfxteam/archive/2010/04/07/9990421.aspx">StaTaskScheduler</a> (for scheduling tasks to an STA thread for COM interop) and <a href="http://blogs.msdn.com/b/pfxteam/archive/2010/04/09/9990424.aspx">many other interesting schedulers</a>. However, out of all of these, only ConcurrentExclusiveSchedulerPair made it into production.</p>

-->

## Mix and Match

In this blog post, I've mentioned three options for concurrency control: TPL Dataflow/Rx, async-friendly primitives, and scheduled concurrency. These are not exclusive.

ConcurrentExclusiveSchedulerPair is particularly useful with TPL Dataflow. Using dataflow, you can define a "mesh" for your data to travel through, and you can use ConcurrentExclusiveSchedulerPair to synchronize or throttle any parts of that mesh. Combining TPL Dataflow with scheduled concurrency is truly powerful!

