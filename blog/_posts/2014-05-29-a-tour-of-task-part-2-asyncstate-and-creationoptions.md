---
layout: post
title: "A Tour of Task, Part 2: AsyncState and CreationOptions"
series: "A Tour of Task"
seriesTitle: "AsyncState and CreationOptions"
---
I skipped last week's blog post (since I was working on the site redesign), so today is a 2-for-1 deal! :)

## AsyncState

{% highlight csharp %}
object AsyncState { get; } // implements IAsyncResult.AsyncState
{% endhighlight %}

The `AsyncState` property implements `IAsyncResult.AsyncState`. This member was useful back in the day, but not so much in modern applications.

When asynchronous programming was going through its awkward teen stage, `AsyncState` was an important part of the [Asynchronous Programming Model (APM)](http://msdn.microsoft.com/en-us/library/ms228963(v=vs.110).aspx). The `Begin*` method would take a `state` parameter, which is assigned to the `IAsyncResult.AsyncState` member. Later, when the application code's callback is invoked, it could access the `AsyncState` value to determine which asynchronous operation completed.

<div class="alert alert-info" markdown="1">
<i class="fa fa-hand-o-right fa-2x pull-left"></i>

`IAsyncResult.AsyncState` (and other "state"-like parameters) are no longer necessary these days; a lambda callback can easily capture any number of local variables in a type-safe way. I prefer the lambda approach; it is more expressive, less brittle, and more flexible than a single `object state` parameter. However, the `state` parameter approach avoids memory allocation, so it is still sometimes used in performance-sensitive code.
</div>

In modern code, the `Task.AsyncState` member is mainly used for [interoperation from `Task` to APM](http://msdn.microsoft.com/en-us/library/hh873178(v=vs.110).aspx#TapToApm). This is only necessary if you're writing `async`/`await` code that must exist within an older asynchronous framework (a rare situation these days). In that scenario, you're implementing the `Begin*`/`End*` methods and using a `Task` instance as your implementation of `IAsyncResult`. The standard approach is to create a `Task<T>` using `TaskCompletionSource<T>`, and you pass the `state` parameter into the `TaskCompletionSource<T>` constructor:

{% highlight csharp %}
public static IAsyncResult BeginOperation(AsyncCallback callback, object state)
{
    var tcs = new TaskCompletionSource<TResult>(state);
    ... // start operation, and complete "tcs" when operation completes
    return tcs.Task;
}
{% endhighlight %}

There really isn't a need to read `AsyncState` in modern code; it's mainly important just because it implements `IAsyncResult.AsyncState`.

## CreationOptions

{% highlight csharp %}
TaskCreationOptions CreationOptions { get; }
{% endhighlight %}

`CreationOptions` merely allows you to read the task creation options that were used to create this task. You can specify these options when you create a task using [the task constructor]({% post_url 2014-05-15-a-tour-of-task-part-1-constructors %}), `Task.Factory.StartNew`, or `TaskCompletionSource<T>`. I'll cover the meanings of these options later, when we cover `Task.Factory.StartNew`.

However, there's almost no reason to _read_ the task creation options once the task has been created. This would only be necessary if you were doing some really funky work with parent/child tasks or task scheduling - _far_ beyond the normal scenarios for asynchronous or parallel tasks.

## Conclusion

The running total of useful members:

<div class="panel panel-default" markdown="1">

{:.table .table-striped}
|Type|Actual Members|Logical Members|Useful for async|Useful for parallel|
|-
|`Task`|10|3|0|0|
|`Task<T>`|10|3|0|0|

</div>
