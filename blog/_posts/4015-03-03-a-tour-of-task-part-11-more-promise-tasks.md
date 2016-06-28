---
layout: post
title: "A Tour of Task, Part 11: More Promise Tasks"
series: "A Tour of Task"
seriesTitle: "More Promise Tasks"
description: "An analysis of TaskCompletionSource, TaskExtensions.Unwrap, and Task.Factory.FromAsync; and discussion of whether they should be used for asynchronous and/or parallel code."
---

Last time we [started the dicussion of Promise Tasks by covering some simple creation methods]({% post_url 2015-04-30-a-tour-of-task-part-10-promise-tasks %}). Today we'll cover some more complex ways to create Promise Tasks.

## TaskCompletionSource

`TaskCompletionSource<T>` gives you manual control over a `Task<T>`; you use a `TaskCompletionSource<T>` to complete the `Task<T>` owned by that `TaskCompletionSource<T>`. `TaskCompletionSource<T>` is the most general-purpose way to create a Promise Task. You should consider `TaskCompletionSource<T>` if you find yourself needing to manually "signal" asynchronous code, or if you need to "await anything".

<div class="alert alert-info" markdown="1">
<i class="fa fa-hand-o-right fa-2x pull-left"></i>

Asynchronous terminology is still in a bit of flux at this point. In the Scala/Akka world, `Task<T>` would be a "future", and `TaskCompletionSource<T>` would be a "promise". In the Javascript world, `Task<T>` would be a "promise", and `TaskCompletionSource<T>` would not have a name (jQuery would call this a "deferred").
</div>

{% highlight csharp %}
TaskCompletionSource(); // constructor

Task<TResult> Task { get; }

bool TrySetResult(TResult);
bool TrySetException(Exception);
bool TrySetCanceled(CancellationToken); // Added in .NET 4.6
bool TrySetCanceled();
{% endhighlight %}



## TaskExtensions.Unwrap



## Task.Factory.FromAsync

The `FromAsync` methods are used to create [TAP wrappers for APM APIs](https://msdn.microsoft.com/en-us/library/hh873178%28v=vs.110%29.aspx#ApmToTap). A [TAP API](https://msdn.microsoft.com/en-us/library/hh873175(v=vs.110).aspx) is one that returns a task ready for use with `await`. An [APM API](https://msdn.microsoft.com/en-us/library/ms228963(v=vs.110).aspx) is an older style of asynchronous programming that uses `Begin`/`End` methods pairs and an `IAsyncResult` object that represents the asynchronous operation.

`FromAsync` has two different sets of overloads. The first set is the :

{% highlight csharp %}
Task FromAsync(Func<AsyncCallback, Object, IAsyncResult>, Action<IAsyncResult>,	object);
Task FromAsync(Func<AsyncCallback, Object, IAsyncResult>, Action<IAsyncResult>, object, TaskCreationOptions);
Task<TResult> FromAsync<TResult>(Func<AsyncCallback, Object, IAsyncResult>, Func<IAsyncResult, TResult>, object);
Task<TResult> FromAsync<TResult>(Func<AsyncCallback, Object, IAsyncResult>, Func<IAsyncResult, TResult>, object, TaskCreationOptions);

Task FromAsync<TArg1>(Func<TArg1, AsyncCallback, Object, IAsyncResult>, Action<IAsyncResult>, TArg1, object);
Task FromAsync<TArg1>(Func<TArg1, AsyncCallback, Object, IAsyncResult>, Action<IAsyncResult>, TArg1, object, TaskCreationOptions);
Task<TResult> FromAsync<TArg1, TResult>(Func<TArg1, AsyncCallback, Object, IAsyncResult>, Func<IAsyncResult, TResult>, TArg1, object);
Task<TResult> FromAsync<TArg1, TResult>(Func<TArg1, AsyncCallback, Object, IAsyncResult>, Func<IAsyncResult, TResult>, TArg1, object, TaskCreationOptions);

Task FromAsync<TArg1, TArg2>(Func<TArg1, TArg2, AsyncCallback, Object, IAsyncResult>, Action<IAsyncResult>, TArg1, TArg2, object);
Task FromAsync<TArg1, TArg2>(Func<TArg1, TArg2, AsyncCallback, Object, IAsyncResult>, Action<IAsyncResult>, TArg1, TArg2, object, TaskCreationOptions);
Task<TResult> FromAsync<TArg1, TArg2, TResult>(Func<TArg1, TArg2, AsyncCallback, Object, IAsyncResult>, Func<IAsyncResult, TResult>, TArg1, TArg2, object);
Task<TResult> FromAsync<TArg1, TArg2, TResult>(Func<TArg1, TArg2, AsyncCallback, Object, IAsyncResult>, Func<IAsyncResult, TResult>, TArg1, TArg2, object, TaskCreationOptions);

Task FromAsync<TArg1, TArg2, TArg3>(Func<TArg1, TArg2, TArg3, AsyncCallback, Object, IAsyncResult>, Action<IAsyncResult>, TArg1, TArg2, TArg3, object);
Task FromAsync<TArg1, TArg2, TArg3>(Func<TArg1, TArg2, TArg3, AsyncCallback, Object, IAsyncResult>, Action<IAsyncResult>, TArg1, TArg2, TArg3, object, TaskCreationOptions);
Task<TResult> FromAsync<TArg1, TArg2, TArg3, TResult>(Func<TArg1, TArg2, TArg3, AsyncCallback, Object, IAsyncResult>, Func<IAsyncResult, TResult>, TArg1, TArg2, TArg3, object);
Task<TResult> FromAsync<TArg1, TArg2, TArg3, TResult>(Func<TArg1, TArg2, TArg3, AsyncCallback, Object, IAsyncResult>, Func<IAsyncResult, TResult>, TArg1, TArg2, TArg3, object, TaskCreationOptions);
{% endhighlight %}

Task FromAsync(IAsyncResult, Action<IAsyncResult>);
Task FromAsync(IAsyncResult, Action<IAsyncResult>, TaskCreationOptions);
Task FromAsync(IAsyncResult, Action<IAsyncResult>, TaskCreationOptions, TaskScheduler);
Task<TResult> FromAsync<TResult>(IAsyncResult, Func<IAsyncResult, TResult>);
Task<TResult> FromAsync<TResult>(IAsyncResult, Func<IAsyncResult, TResult>, TaskCreationOptions);
Task<TResult> FromAsync<TResult>(IAsyncResult, Func<IAsyncResult, TResult>, TaskCreationOptions, TaskScheduler);

// http://blogs.msdn.com/b/pfxteam/archive/2009/06/09/9716439.aspx
// http://blogs.msdn.com/b/pfxteam/archive/2012/02/06/10264610.aspx