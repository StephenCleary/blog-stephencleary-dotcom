---
layout: post
title: "Thinking about Async Tasks"
---
In this post, I'm going to clarify how Tasks are used by async/await. This is a little bit different than Tasks _as used by the Task Parallel Library,_ and it's also a little bit different than _awaitables_ as used by async/await.

## Tasks Are a Future

A **future** is [some operation that will complete at some future time](http://en.wikipedia.org/wiki/Futures_and_promises).

An async Task is _not_ a thread - not even a tiny one. This is one of the most common conceptual hurdles to working with async. **Task != Thread**.

Similarly, an async Task is _not_ a delegate.

Some tasks do _contain_ a delegate, and they represent some work to be done on a thread. However, as we saw in [the Creating Tasks post]({% post_url 2012-02-09-creating-tasks %}), tasks created with TaskCompletionSource\<T> have no code or delegate at all!

## Tasks Complete Once

A task will complete exactly one time. It can complete successfully or with error (cancellation is treated as a special kind of error).

Because tasks complete only once, they're not ideal for representing _streams of data_ or _event subscriptions_. We'll deal with stream/subscription scenarios in a later post.

## Tasks Support Continuations

A **continuation** is [some code that is attached to a task and executed when that task completes](http://msdn.microsoft.com/en-us/library/ee372288.aspx). Tasks have direct support for continuations via the ContinueWith method.

However, you usually do not need to call that method. The await keyword will use task continuations to schedule the remainder of the async method as necessary.

## Differences between Async Tasks and TPL Tasks

The Task class was introduced with the Task Parallel Library. The TPL usage of Task is a bit more general than the Async usage of Task. Also, the TPL was designed with fork/join parallelism in mind, and those portions of the Task class API are not used with async tasks.

Under the TPL, the creation of a task and the scheduling of that task may be separate. It is possible to create a Task object and not start it until later. Under Async, every task is already in progress; its operation is started when the Task object is created. Because of this, you may have to call Task.Start on a Task returned from TPL code if you want to await it.

TPL has a concept of parent and child tasks. Async tasks do not use this mechanism. There _is_ a _logical_ hierarchy among async tasks, but they do not use the [parent/child relationship provided by the TPL](http://msdn.microsoft.com/en-us/library/dd997417.aspx).

Each TPL task may have multiple errors. Even if a task only has one exception, it is wrapped in an AggregateException. Async tasks are only expected to have one error, so the await operator will avoid this AggregateException wrapper.

## Differences between Async Tasks and Awaitables

An awaitable is a very generic form of background operation. Awaitables support testing for completion (IsCompleted), scheduling continuations (OnCompleted), and retrieving the results of the operation (GetResult).

The await operator uses [a well-defined pattern](http://blogs.msdn.com/b/lucian/archive/2011/04/15/async-ctp-refresh-design-changes.aspx), so it's possible to have some very strange awaitables that do work correctly.

For example, the awaitable returned by the Task.Yield method _never_ returns true for IsCompleted, and its OnCompleted will immediately schedule the completions. So, on the one hand it never completes, but on the other hand it is already completed.

WinRT awaitables are also not quite like Task objects. <del>The most important difference is that WinRT operations do not start their operation immediately. Normally, the WinRT awaitable will start the operation for you when it is used in an await expression. However, this won't work as well if you want to have multiple operations running simultaneously. In this case, you can convert any WinRT awaitable into an async Task by calling the **StartAsTask** extension method.</del> **Update:** WinRT operations [have been changed](http://blogs.msdn.com/b/windowsappdev/archive/2012/03/20/keeping-apps-fast-and-fluid-with-asynchrony-in-the-windows-runtime.aspx) so that they _do_ start immediately.

## Functional Concepts

In conclusion, I'd like to point out that we're witnessing more functional concepts gradually enter our imperative language: both _future_ and _continuation_ are concepts borrowed from functional languages.

This helps explain _why_ async code will gently nudge you into a functional programming style. And I'll say it again: this is natural, and should be embraced.

