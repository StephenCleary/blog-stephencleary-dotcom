---
layout: post
title: "CancellationToken Behavior with AsyncEx Coordination Primitives"
---
Today I'm going to talk a little bit about some behavior that I standardized on for the asynchronous coordination primitives in my AsyncEx library. I'll first review what's currently available in the BCL and then describe my approach.



## Waiting on SemaphoreSlim

The only built-in .NET asynchronous-compatible coordination primitive (as of .NET 4.5) is [SemaphoreSlim](http://msdn.microsoft.com/en-us/library/system.threading.semaphoreslim.aspx). This type can be used by both synchronous and asynchronous code, which is a very interesting aspect. What I'm looking at today is all the different ways to _wait_ for a semaphore to be available (and I'm just going to consider asynchronous code).



First, there's the obvious [Task WaitAsync()](http://msdn.microsoft.com/en-us/library/hh462805.aspx) method for an **unconditional wait**. This is the most commonly-used type of wait: the code knows it needs to acquire the semaphore and it will wait however long it takes until the semaphore is available.



Next, there's a couple of overloads for timeouts. [Task<bool> WaitAsync(int)](http://msdn.microsoft.com/en-us/library/hh462740.aspx) and [Task<bool> WaitAsync(TimeSpan)](http://msdn.microsoft.com/en-us/library/hh462723.aspx) both do an (asynchronous) **timed wait**, which will return `false` if the wait ran out of time before it was granted access.



There's an overload taking a cancellation token, [Task WaitAsync(CancellationToken)](http://msdn.microsoft.com/en-us/library/hh462773.aspx). This performs a **cancelable wait**, where some event can interrupt the wait if it determines that the code doesn't need that semaphore anymore. If a wait is canceled, the wait task is canceled (instead of returning `false`).



You can also have cancelable timed waits, via the [Task<bool> WaitAsync(int, CancellationToken)](http://msdn.microsoft.com/en-us/library/hh462846.aspx) and [Task<bool> WaitAsync(TimeSpan, CancellationToken)](http://msdn.microsoft.com/en-us/library/hh462788.aspx). These overloads will cancel their tasks if the cancellation token fires, and return `false` if they hit the timeout.



Finally, there's one other important kind of wait you can do: an **atomic wait**, where you immediately (synchronously) acquire the semaphore if it is available. Asynchronous code can use [bool Wait(int)](http://msdn.microsoft.com/en-us/library/dd289488.aspx) and pass zero to perform an atomic wait. This is logically similar to `Monitor.TryEnter`.



## Waiting on AsyncSemaphore

I'll skip right to the punchline. There are only two wait overloads in the `AsyncSemaphore` class: `Task WaitAsync()` and `Task WaitAsync(CancellationToken)`.



The **unconditional wait** uses the same obvious method: `Task WaitAsync()` (which is shorthand for `WaitAsync(CancellationToken.None)`). The other overload `Task WaitAsync(CancellationToken)` handles everything else.



To do a **timed wait**, create a cancellation token to cancel the wait (e.g., using `CancellationTokenSource.CancellationTokenSource(TimeSpan)` or `CancellationTokenHelpers.Timeout(TimeSpan)`) and pass it to `WaitAsync`. When the timer expires, the wait is canceled.



Of course, a **cancelable wait** can just pass in its `CancellationToken`. Cancelable timed waits can use chained cancellation tokens (via `CancellationTokenSource.CreateLinkedTokenSource` or `CancellationTokenHelpers.Normalize`), and they can follow up with `CancellationToken.IsCancellationRequested` if it's important to distinguish _why_ the wait was canceled.



That just leaves the **atomic wait**, and this is where the "interesting design" comes in. If you pass in a token that is _already canceled_, then `WaitAsync` will always return synchronously: a successfully completed task if the semaphore is available, otherwise a canceled task. This gives you the "TryLock" kind of behavior, but this also means that if you have a regular cancellation token, it will not result in a canceled task if the semaphore is available.



This special behavior is supported for every kind of "wait" in AsyncEx where atomic waits make sense and that modifies state: `AsyncSemaphore.WaitAsync`, `AsyncLock.LockAsync`, `AsyncAutoResetEvent.WaitAsync`, `AsyncMonitor.EnterAsync`, `AsyncReaderWriterLock.WriterLockAsync`, `AsyncReaderWriterLock.ReaderLockAsync`, `AsyncReaderWriterLock.UpgradeableReaderLockAsync`, and `AsyncReaderWriterLock.UpgradeableReaderKey.UpgradeAsync`.


