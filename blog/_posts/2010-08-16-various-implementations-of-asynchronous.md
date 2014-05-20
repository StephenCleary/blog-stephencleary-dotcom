---
layout: post
title: "Various Implementations of Asynchronous Background Tasks"
---
This is going to be a long blog post, because there's plenty of ground to cover. Executive summary: there are lots of ways to do background tasks in .NET, but use the new Task class if possible because it's the best. :D

Many user interface applications find that they need to support some kind of an asynchronous "background" task. The exact requirements vary, but most applications need some kind of ability to kick off an operation that will run without blocking the user interface; and have that operation report back to the user interface thread when completed.

## Common Requirements

Not all applications need all of these, but some common requirements are:

- **Results.** Usually, the purpose of the asynchronous operation is to determine some value or set of values that is then used to update the state of the program.
- **Errors.** If there is an exception during the background processing, then it's nice to have that exception preserved, including its original call stack.
- **Progress.** In addition to just updating program state upon completion (or error), it's often useful to incrementally report progress.
- **Cancellation.** For long-running operations (especially of the CPU-intensive variety), one should include some kind of cancellation mechanism. If possible, the background operation should have a way to detect when cancellation has been requested and respond properly.
- **Nesting.** A commonly-overlooked requirement is the ability to nest background operations. This is not needed for simple UI-layer background operations, but it becomes more important when designing business-layer background operations.
- **Synchronization.** Usually there is some UI that must be updated when a background task reports progress, completes with a result, completes with an error, or is cancelled.

## Tasks (Async Methods)

The best overall method is to use [Task-returning asynchronous methods]({% post_url 2012-02-02-async-and-await %}), new in .NET 4.5 and C# 5.0. They naturally support all of the common requirements:

 - **Results.** Any asynchronous method returning Task<TResult> can just return its result directly. The result is retrieved by awaiting the Task<TResult>.
 - **Errors.** Any exceptions thrown by a task are rethrown when the task is awaited. The call stack is properly preserved.
 - **Progress.** Asynchronous methods use a progress reporting abstraction (IProgress<T>) to report progress. The caller of the asynchronous method determines what happens to the progress updates.
 - **Cancellation.** Asynchronous methods integrate with the [unified cancellation framework](http://msdn.microsoft.com/en-us/library/dd997364.aspx), which provides exhaustive cancellation support.
 - **Nesting.** Asynchronous methods naturally nest by awaiting the results of other asynchronous methods. Exceptions from inner methods are correctly propagated out. Cancellation can easily be propagated by passing the CancellationToken down to the inner method.
 - **Synchronization.** Asynchronous methods by default will capture and resume their context automatically.

## Tasks (Task Parallel Library)

You can also use tasks without asynchronous methods. Tasks were introduced in the [Task Parallel 
Library](http://msdn.microsoft.com/en-us/library/dd537609.aspx) (.NET 4.0). The following requirements are fully supported:

  - **Results.** The Task<TResult> class allows the natural returning of the result. The result is retrieved by reading Task<TResult>.Result.
  - **Errors.** Any exceptions thrown by a task are rethrown when the task is [observed](http://msdn.microsoft.com/en-us/library/dd997415.aspx). The original exception is wrapped in an AggregateException, so the outer exception no longer carries the correct call stack.
  - **Cancellation.** .NET 4.0 includes a [unified cancellation framework](http://msdn.microsoft.com/en-us/library/dd997364.aspx) that provides exhaustive cancellation support.
  - **Nesting.** Tasks can be [nested](http://msdn.microsoft.com/en-us/library/dd997417.aspx) if desired; child tasks properly propagate any exceptions upward, and parent tasks may optionally propagate cancellation downward. Nesting is not automatic, so this ability should be exposed by any business-layer API that is Task-based.
  - **Synchronization.** Tasks introduce a very flexible model of synchronization by separating the actual operation from how it is [scheduled](http://msdn.microsoft.com/en-us/library/dd997402.aspx). Synchronization with the user interface is only slightly awkward; a user interface task scheduler can be retrieved by calling TaskScheduler.FromCurrentSynchronizationContext while on the UI thread. This scheduler [can then be used](http://msdn.microsoft.com/en-us/library/dd997394.aspx) to schedule a [task continuation](http://msdn.microsoft.com/en-us/library/ee372288.aspx) to marshal the result, error, or cancellation update to the UI thread.

Progress reporting is a bit complex for tasks:

   - **Progress.** One way to report progress from a task is to create another task (to update the UI), schedule it to the UI thread, and wait for it to complete. There is a [ProgressReporter wrapper class on this blog]({% post_url 2010-06-18-reporting-progress-from-tasks %}) which helps simplify the code.

## BackgroundWorker

Before .NET 4.0 was released, [BackgroundWorker](http://msdn.microsoft.com/en-us/library/8xs8549b.aspx) was the de-facto standard. It supports most of the requirements:

    - **Results.** Supporting a result is slightly awkward; the DoWork delegate has to set the DoWorkEventArgs.Result property of its argument. This value is then passed to the RunWorkerCompleted delegate, as the RunWorkerCompletedEventArgs.Result property.
    - **Errors.** Exceptions thrown by DoWork are caught and passed to the RunWorkerCompleted delgate, as the RunWorkerCompletedEventArgs.Error property. That exception object does include the correct call stack; however, if the RunWorkerCompleteEventArgs.Result property is accessed when the operation completed with an error, then the original exception is wrapped in a TargetInvocationException, so the outer exception no longer carries the correct call stack.
    - **Progress.** Any BackgroundWorker whose WorkerSupportsProgress property is true may report progress. The DoWork delegate invokes ReportProgress, which causes the ProgressChanged event to fire. Progress reporting is always asynchronous, so DoWork will continue to run before the ProgressChanged event actually executes.
    - **Cancellation.** Any BackgroundWorker whose WorkerSupportsCancellation property is true may be cancelled. The cancelling thread first calls BackgroundWorker.CancelAsync. This causes the BackgroundWorker.CancellationPending property to become true. The DoWork delegate should monitor that property (checking it on a regular basis), and set DoWorkEventArgs.Cancel to true and return if the operation is cancelled. The RunWorkerCompleted delegate detects a cancelled result by checking RunWorkerCompletedEventArgs.Cancelled.
    - **Synchronization.** The biggest benefit of BackgroundWorker is its support for automatic synchronization. The ProgressChanged and RunWorkerCompleted events are synchronized to the SynchronizationContext that was in place when RunWorkerAsync was called. In most situations, RunWorkerAsync is called from a UI thread, and so the ProgressChanged and RunWorkerCompleted events are invoked on the UI thread.

BackgroundWorker does have one rather significant drawback. It works perfectly for less complex systems, but does not nest easily.

     - **Nesting.** BackgroundWorker's problem with nesting is because the DoWork delegate is not given a SynchronizationContext in which to run. Because of this, any time RunWorkerAsync is called from DoWork, the ProgressChanged and RunWorkerCompleted events on the child BackgroundWorker are _not_ synchronized to the UI thread (or to the parent BackgroundWorker).
This can be solved one of two ways:

      - If the child BackgroundWorker should synchronize to the UI, then the parent BackgroundWorker can manually install the UI SynchronizationContext in its DoWork delegate by calling SynchronizationContext.SetSynchronizationContext.
      - If the child BackgroundWorker should synchronize to the parent BackgroundWorker (not the UI), then the parent BackgroundWorker can run a synchronization loop using an object like [Nito.Async.ActionDispatcher](http://nitoasync.codeplex.com/). Alternatively, the parent BackgroundWorker could be replaced entirely by a [Nito.Async.ActionThread](http://nitoasync.codeplex.com/).

## Delegate.BeginInvoke

Every delegate in .NET supports [asynchronous invocation](http://msdn.microsoft.com/en-us/library/2e08f6yc.aspx). This is a lower-level technique that does not require a separate object (e.g., Task or BackgroundWorker) to define an asynchronous operation. Because it is at a lower level, it supports fewer of the standard requirements:

       - **Results.** The result of the delegate may be retrieved by calling Delegate.EndInvoke, even if the asynchronous delegate has already completed.
       - **Errors.** Any exception thrown by the delegate is preserved and rethrown by Delegate.EndInvoke, properly preserving the call stack.

This lower-level approach does not cleanly support these requirements:

        - **Progress.** A delegate must be designed to support progress reporting; one way to do this is to have the method take another delegate as one of its arguments and invoke that delegate to report progress.
        - **Cancellation.** There is no built-in cancellation support, but a delegate may regularly check for a cancellation signal (e.g., a volatile bool or ManualResetEvent).
        - **Nesting.** Delegates may of course asynchronously invoke other delegates; however, there is no concept of "parent" and "child" asynchronous delegates. Propagation of errors is automatic, but propagation of cancellation is not.
        - **Synchronization.** There is no automatic synchronization for asynchronous delegates. There are two common solutions:

         - Use the AsyncOperation and AsyncOperationManager classes. These types provide a thin wrapper around SynchronizationContext, allowing for simple (asynchronous) synchronization of progress and completion. The disadvantage of these classes is that they do not support nesting. [Note: BackgroundWorker just uses these classes with an asynchronous delegate, so if you need synchronization, it's usually just best to use Tasks or BackgroundWorker]
         - Use the SynchronizationContext class directly. The synchronization code is a bit more complex, but it is possible to support nesting.

## ThreadPool.QueueUserWorkItem

One of the lowest-level approaches is to queue the work directly to the ThreadPool. Unfortunately, this approach does not support _any_ of the requirements directly; every requirement needs a fair amount of work:

          - **Results.** The delegate passed to ThreadPool.QueueUserWorkItem cannot return a value. To return a result, one must either use a child object of an argument (similar to BackgroundWorker) or pass a lambda expression bound to a variable holding the return value.
          - **Errors.** If a delegate queued to the ThreadPool allows an exception to propagate, then the entire process is killed. If any errors are possible, then they should be wrapped in a try...catch and the exception object "returned" to the calling thread (either using a child object of an argument, or using a bound variable of a lambda expression). The exception could be rethrown with the correct stack trace by calling PrepareForRethrow from the [Rx library](http://msdn.microsoft.com/en-us/devlabs/ee794896.aspx).

The other requirements have the same problems (and mitigating solutions) as the Delegate.BeginInvoke approach above.

## Thread

Of course, one obvious approach is to place a background operation in its own thread. This is often a sub-optimal solution, since the ThreadPool is designed to handle varying loads. There is almost never a need to manually create background Thread objects. However, many programmers naturally turn to the Thread class as an obvious solution.

Just like the ThreadPool.QueueUserWorkItem approach, the manual Thread approach does not support _any_ of the requirements out of the box. Manual threads have the same problems (and mitigating solutions) as the ones listed under ThreadPool.QueueUserWorkItem. In addition, manual Thread objects almost always are less efficient than the built-in ThreadPool.

