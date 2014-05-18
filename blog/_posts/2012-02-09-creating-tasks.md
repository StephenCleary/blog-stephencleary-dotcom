---
layout: post
title: "Creating Tasks"
tags: ["async", ".NET"]
---


Microsoft will give us lots of awaitables in .NET 4.5, but there are some situations where we want to create our own awaitable. Task<T> and Task are the easiest awaitable types to work with in .NET, so today we'll look at different ways awaitable Task objects can be constructed.





All Task objects fall into one of two types: "code" and "event". Code-based tasks have a delegate that they are trying to run. Event-based tasks have no code; they're just waiting for some event to take place.



## Tasks as Events



Tasks without code can represent any kind of event. The most common examples are I/O completion events, but event-based tasks can actually wrap _any_ kind of event.





To create an event-based task, use the [**TaskCompletionSource<TResult>**](http://msdn.microsoft.com/en-us/library/dd449174.aspx) class:




public static Task<int> MyIntegerEventAsync()
{
  TaskCompletionSource<int> tcs = new TaskCompletionSource<int>();

  // Register for the "event".
  //   For example, if this is an I/O operation, start the I/O and register for its completion.

  // When the event fires, it should call:
  //   tcs.TrySetResult(...); // For a successful event.
  // or
  //   tcs.TrySetException(...); // For some error.
  // or
  //   tcs.TrySetCanceled(); // If the event was canceled.

  // TaskCompletionSource is thread-safe, so you can call these methods from whatever thread you want.

  // Return the Task<int>, which will complete when the event triggers.
  return tcs.Task;
}




Remember that Task<T> and Task are awaitable, so you can await the result of MyIntegerEventAsync even though it's not an async method.





You _probably_ won't have to use TaskCompletionSource<TResult> directly; you can use **TaskFactory.FromAsync** to wrap any operation that uses IAsyncResult (and most existing asynchronous I/O methods do use IAsyncResult). Internally, FromAsync does use TaskCompletionSource<TResult>.



> [Nito.AsyncEx](http://nuget.org/packages/Nito.AsyncEx) includes an **AsyncFactory** type which works like TaskFactory.FromAsync but is slightly easier to use and supports more arguments. It also includes a (non-generic) **TaskCompletionSource**, which is easier to use when creating (non-generic) Tasks.


## Tasks as Code



Remember (from our [async intro post](http://blog.stephencleary.com/2012/02/async-and-await.html)) that the async keyword does _not_ run the method on a background thread. However, you can use **Task.Run** to run code on a background thread:




public static void MyThreadPoolMethod()
{
  // Do work (assuming we're running on the thread pool).
}

public async Task DoStuffAsync()
{
  var cpuResult = await Task.Run(MyThreadPoolMethod);

  // Use cpuResult...
}




Task.Run will take a delegate and run it on the thread pool for you. It wraps the delegate into a Task<T> or Task, and the Task wrapper [takes care of all the error handling and other stuff correctly](http://blog.stephencleary.com/2010/08/various-implementations-of-asynchronous.html).





What about other threads? What if you don't want to run your code on the thread pool?





If you have a particular context in which you want to run your code, you can use a **TaskFactory** initialized with a **TaskScheduler** that targets your context. You can then call **TaskFactory.StartNew** to run your code in that context. In fact, Task.Run is just a shorthand for Task.Factory.StartNew.





It's possible to capture the current synchronization context into a TaskScheduler by calling **TaskScheduler.FromCurrentSynchronizationContext**, and then use it later (e.g., from a background thread) to run code within that context. _Normally there are better ways to do this,_ but it is an option.



> Before async/await, this was [a good way to send progress reports from a background task to the UI](http://blog.stephencleary.com/2010/06/reporting-progress-from-tasks.html). With async/await, there is now [a better way](http://blog.stephencleary.com/2012/02/reporting-progress-from-async-tasks.html).




Writing your own TaskScheduler is possible, but frustrating due to the lack of MSDN documentation. Fortunately, it's almost never necessary.



> If you need a specific thread (e.g., an STA thread) that supports TaskScheduler, you can use the [**AsyncContextThread** type](http://nitoasyncex.codeplex.com/wikipage?title=AsyncContextThread) in the [Nito.AsyncEx library](http://nuget.org/packages/Nito.AsyncEx).




There are some pretty cool tricks we can pull off using the built-in TaskScheduler/TaskFactory types. We'll look at more advanced TaskScheduler situations in later posts.



## Tasks as Async Methods



This is a special case of Task creation - and it's easy to overlook!





The flat-out easiest way to create Task objects is to write an asynchronous method:




public async Task<int> DivideAsync(int numerator, int denominator)
{
  await Task.Delay(100);
  return numerator / denominator;
}




We do not create a Task<int> in our code, but the compiler rewrites our code so that a Task<int> is created and returned. When the method completes, the task completes. This type of task is actually an event-based task, since an event (the method returning) causes the task to complete.





That's the easiest way to create Task objects! However, it only works if you're building on existing awaitables; if you aren't in this situation, then you should use TaskFactory.StartNew or TaskCompletionSource<TResult>.

