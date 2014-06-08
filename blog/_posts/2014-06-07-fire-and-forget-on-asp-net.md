---
layout: post
title: "Fire and Forget on ASP.NET"
description: "A summary of the various ways to schedule background or fire-and-forget tasks on ASP.NET."
---

.NET 4.5.2 added a built-in way to queue background (a.k.a. "fire and forget") work in ASP.NET. This post is a summary of the different techniques available today.

## ThreadPool

The easiest method is to just throw work onto a background thread, e.g., `Task.Run`, `Task.Factory.StartNew`, `Delegate.BeginInvoke`, `ThreadPool.QueueUserWorkItem`, etc. However, this is a **really bad idea**!

The reason is that the ASP.NET runtime has no idea that you've queued this work, so it's not aware that the background work even _exists_. For a variety of reasons, IIS/ASP.NET has to occasionally recycle your application. If you have background work running when this recycling takes place, that work will mysteriously disappear.

## HostingEnvironment.QueueBackgroundWorkItem

`QueueBackgroundWorkItem` (QBWI) was added in .NET 4.5.2 to help mitigate loss of background work. [QBWI will register its background work with the ASP.NET runtime](http://blogs.msdn.com/b/webdev/archive/2014/06/04/queuebackgroundworkitem-to-reliably-schedule-and-run-long-background-process-in-asp-net.aspx). 

Since the ASP.NET runtime is aware of the background work, it will not immediately yank your AppDomain when it's time to recycle. However, this does not mean that the background work can just do whatever it wants!

QBWI will register the work with ASP.NET. When ASP.NET has to recycle, it will notify the background work (by setting a `CancellationToken`) and will then wait up to 30 seconds for the work to complete. If the background work doesn't complete in that time frame, the work will mysteriously disappear.

So, QBWI is a step in the right direction. I would say it's the "minimum viable" alternative.

## IRegisteredObject

The standard way to register with ASP.NET is via `IRegisteredObject`. The semantics aren't officially documented, but they're described in [this blog post under the third answer of question 5](http://blogs.msdn.com/b/tmarq/archive/2010/04/14/performing-asynchronous-work-or-tasks-in-asp-net-applications.aspx):

> You can create an object that implements the `IRegisteredObject` interface and call `HostingEnvironment.RegisterObject` to "register" it with ASP.NET.  When the AppDomain is about to be unloaded, we will call your implementation of `IRegisteredObject.Stop(bool immediate)`.  The `Stop` method is called twice by ASP.NET, once with the `immediate` parameter set to `false` and once again with that argument set to `true`.  You are supposed to call `HostingEnvironment.UnregisterObject` as soon as your registered object has stopped, so ASP.NET knows it doesn’t need to call your `Stop` method.  You can call it at anytime, but you definitely should call it before returning from `Stop` when it is called with `immediate` set to `true`, because that’s your final chance and if you’re still running after that you will be rudely aborted...  If you need to, you can hold up the unload as long as you like, because we won’t unload until your `Stop` method returns the second time.

This blog post clarifies the semantics. Essentially, they boil down to this:

1. When ASP.NET wants to unload your application, it will call `IRegisteredObject.Stop(false)` on all objects registered with the ASP.NET runtime.
2. This is a friendly notification that the AppDomain is going away. Registered objects should start cleaning up and call `UnregisterObject` when their cleanup is complete.
3. If there are still registered objects running after 30 seconds, ASP.NET will call `IRegisteredObject.Stop(true)` on them.
4. ASP.NET will unload the AppDomain after all those second notifications have returned.

I use `IRegisteredObject` in my [ASP.NET Background Tasks library](https://github.com/StephenCleary/AspNetBackgroundTasks), and (as of this writing) the built-in `QueueBackgroundWorkItem` _also_ uses `IRegisteredObject`. These approaches are slightly different, though. `QueueBackgroundWorkItem` ignores the second call to `Stop` and returns immediately, so any background work that takes longer than 30 seconds will be terminated. My AspNetBackgroundTasks library will wait for the background work to complete, and will delay the AppDomain shutdown indefinitely.

Also, QBWI will log any exceptions from the background work, while AspNetBackgroundTasks will not.

Neither one of those choices is better; they're just different. `QueueBackgroundWorkItem` chose to keep the system as a whole more reliable by sacrificing long-running background work items. AspNetBackgroundTasks chose not to terminate background tasks, which could impact the regular application recycling if one of those tasks is misbehaved.

Neither of these solutions is ideal. They have to make tradeoffs, and of course neither of them can recover from situations like power losses and server failures. So, I characterize these registration-based solutions as "more reliable" than just queueing work to the thread pool (which is totally unreliable), but even registering with ASP.NET is not totally reliable.

## HangFire

One interesting low-entry reliable solution is [HangFire](http://hangfire.io/). This library is pretty easy to use but does make some assumptions.

The first and most obvious assumption is that it relies on some kind of reliable storage already in your architecture: specifically, SQL Server, Redis, or Microsoft Message Queues (MSMQ). This is a good start, but I'd like to see support for Azure tables and queues as well.

The second assumption is that the code will not significantly change. The way that HangFire serializes the background work makes assumptions around the delegates that are queued. If the code changes significantly and ASP.NET restarts with the new code, I'm not sure how the system will respond when it attempts to run the old background work.

So, I'd need to research this library more to feel comfortable using it (and extend it to support Azure queues), but it does look like a very interesting library with a low bar to entry.

## Distributed Architecture

The proper solution is to use a basic distributed architecture.

The first thing you need is a reliable storage medium. I prefer queues (e.g., Azure queues, WebSphere message queues, Microsoft message queueing, etc), but any kind of reliable storage would work (as previously mentioned, HangFire prefers databases).

Then you'll need a way to store the background work in that storage. HangFire uses an interesting method of serializing the delegate, which I am a little leery of. I'd prefer a solution that stored the background work _semantically_. This also means that the schema for the background work should be versioned.

The next thing is a host to perform the background work. These days I'd prefer an Azure WebJob, but you could also use a separate thread in an ASP.NET app. The host must be reliable in the sense that it should either complete the work or leave the work in storage to try again, but not both. Usually, the easiest way to satisfy this is to make all the work items idempoent and use a "lease" when reading from storage. Most reliable queues have built-in support for leases, but it's up to you to make the background work idempotent.

The last thing in this kind of architecture is some kind of "poison message" recovery. That is, if there is some background work that gets into the system, and that work cannot complete successfully for whatever reason, there has to be some procedure for removing that piece of background work and setting it aside so that the system as a whole can continue processing.

Distributed architecture is complex, but it's The Right Way&trade;. It is the most reliable and resilient option available.