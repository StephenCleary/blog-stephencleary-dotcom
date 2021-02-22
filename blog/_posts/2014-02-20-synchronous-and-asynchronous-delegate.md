---
layout: post
title: "Synchronous and Asynchronous Delegate Types"
---
Delegate types can be confusing to developers who are learning more about async and await.

There is a pattern to asynchronous delegate types, but first you must understand how asynchronous methods are related to their synchronous counterparts. When transforming a synchronous method to async, one of the steps is to change the return type. If `MyMethod` returns `void`, then `MyMethodAsync` should return `Task`. Otherwise (that is, if `MyMethod` returns `T`), then `MyMethodAsync` should return `Task<T>`. This modification of the return type is what makes delegate type translation a bit tricky.

<!--<blockquote>Actually, if C# had a true "void type" (commonly called "unit" in functional languages), we wouldn't have this problem. But it's too late for that now.</blockquote>-->

This return-type transformation can also be applied to delegate types. If the delegate is one of the `Action` delegate types, then change it to `Func` and append a `Task` (as the return type). Otherwise (that is, the delegate is already a `Func`), change the last type argument from `T` to `Task<T>`.

This a bit complex to describe in words, so here's a little table that lays out several examples. Each synchronous example is paired with its asynchronous counterpart:

<div class="panel panel-default" markdown="1">

{:.table .table-striped}
|Standard Type|Example Lambda|Parameters|Return Value|
|-
|`Action`|`() => { }`|None|None|
|`Func<Task>`|`async () => { await Task.Yield(); }`|None|None|
|`Func<TResult>`|`() => { return 13; }`|None|`TResult`|
|`Func<Task<TResult>>`|`async () => { await Task.Yield(); return 13; }`|None|`TResult`|
|`Action<TArg1>`|`x => { }`|`TArg1`|None|
|`Func<TArg1, Task>`|`async x => { await Task.Yield(); }`|`TArg1`|None|
|`Func<TArg1, TResult>`|`x => { return 13; }`|`TArg1`|`TResult`|
|`Func<TArg1, Task<TResult>>`|`async x => { await Task.Yield(); return 13; }`|`TArg1`|`TResult`|
|`Action<TArg1, TArg2>`|`(x, y) => { }`|`TArg1, TArg2`|None|
|`Func<TArg1, TArg2, Task>`|`async (x, y) => { await Task.Yield(); }`|`TArg1, TArg2`|None|
|`Func<TArg1, TArg2, TResult>`|`(x, y) => { return 13; }`|`TArg1, TArg2`|`TResult`|
|`Func<TArg1, TArg2, Task<TResult>>`|`async (x, y) => { await Task.Yield(); return 13; }`|`TArg1, TArg2`|`TResult`|

</div>

The table above ignores `async void` methods, which you [should be avoiding anyway](http://msdn.microsoft.com/en-us/magazine/jj991977.aspx). Async void methods are tricky because you _can_ assign a lambda like `async () => { await Task.Yield(); }` to a variable of type `Action`, even though the _natural_ type of that lambda is `Func<Task>`. Stephen Toub has written [more about the pitfalls of async void lambdas](https://devblogs.microsoft.com/pfxteam/potential-pitfalls-to-avoid-when-passing-around-async-lambdas/).

As a closing note, the C# compiler has been updated in VS2012 to correctly perform overload resolution in the presence of async lambdas. So, this kind of method declaration works fine:

{% highlight csharp %}
Task QueueAsync(Action action); // Sync, no return value
Task<T> QueueAsync<T>(Func<T> action); // Sync with return value
Task QueueAsync(Func<Task> action); // Async, no return value
Task<T> QueueAsync<T>(Func<Task<T>> action); // Async with return value
{% endhighlight %}