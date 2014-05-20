---
layout: post
title: "Synchronous and Asynchronous Delegate Types"
---
Delegate types can be confusing to developers who are learning more about async and await.

There is a pattern to asynchronous delegate types, but first you must understand how asynchronous methods are related to their synchronous counterparts. When transforming a synchronous method to async, one of the steps is to change the return type. If `MyMethod` returns `void`, then `MyMethodAsync` should return `Task`. Otherwise (that is, if `MyMethod` returns `T`), then `MyMethodAsync` should return `Task<T>`. This modification of the return type is what makes delegate type translation a bit tricky.

<!--<blockquote>Actually, if C# had a true "void type" (commonly called "unit" in functional languages), we wouldn't have this problem. But it's too late for that now.</blockquote>-->

This return-type transformation can also be applied to delegate types. If the delegate is one of the `Action` delegate types, then change it to `Func` and append a `Task` (as the return type). Otherwise (that is, the delegate is already a `Func`), change the last type argument from `T` to `Func<T>`.

This a bit complex to describe in words, so here's a little table that lays out several examples. Each synchronous example is paired with its asynchronous counterpart:

<div class="panel panel-default">
  <table class="table table-striped">

<tr>
  <th>Standard Type</th>
  <th>Example Lambda</th>
  <th>Parameters</th>
  <th>Return Value</th>
</tr>
<tr>
  <td>
    <code class="csharp">Action</code>
    <br />
    <code class="csharp">Func&lt;Task&gt;</code>
  </td>
  <td>
    <code class="csharp">() =&gt; { }</code>
    <br />
    <code class="csharp">
      <span class="keyword">async</span> () =&gt; { <span class="keyword">await</span> Task.Yield(); }</code>
  </td>
  <td>None</td>
  <td>None</td>
</tr>
<tr>
  <td>
    <code class="csharp">Func&lt;TResult&gt;</code>
    <br />
    <code class="csharp">Func&lt;Task&lt;TResult&gt;&gt;</code>
  </td>
  <td>
    <code class="csharp">() =&gt; { <span class="keyword">return</span> 13; }</code>
    <br />
    <code class="csharp">
      <span class="keyword">async</span> () =&gt; { <span class="keyword">await</span> Task.Yield(); <span class="keyword">return</span> 13; }</code>
  </td>
  <td>None</td>
  <td>
    <code class="csharp">TResult</code>
  </td>
</tr>
<tr>
  <td>
    <code class="csharp">Action&lt;TArg1&gt;</code>
    <br />
    <code class="csharp">Func&lt;TArg1, Task&gt;</code>
  </td>
  <td>
    <code class="csharp">x =&gt; { }</code>
    <br />
    <code class="csharp">
      <span class="keyword">async</span> x =&gt; { <span class="keyword">await</span> Task.Yield(); }</code>
  </td>
  <td>
    <code class="csharp">TArg1</code>
  </td>
  <td>None</td>
</tr>
<tr>
  <td>
    <code class="csharp">Func&lt;TArg1, TResult&gt;</code>
    <br />
    <code class="csharp">Func&lt;TArg1, Task&lt;TResult&gt;&gt;</code>
  </td>
  <td>
    <code class="csharp">x =&gt; { <span class="keyword">return</span> 13; }</code>
    <br />
    <code class="csharp">
      <span class="keyword">async</span> x =&gt; { <span class="keyword">await</span> Task.Yield(); <span class="keyword">return</span> 13; }</code>
  </td>
  <td>
    <code class="csharp">TArg1</code>
  </td>
  <td>
    <code class="csharp">TResult</code>
  </td>
</tr>
<tr>
  <td>
    <code class="csharp">Action&lt;TArg1, TArg2&gt;</code>
    <br />
    <code class="csharp">Func&lt;TArg1, TArg2, Task&gt;</code>
  </td>
  <td>
    <code class="csharp">(x, y) =&gt; { }</code>
    <br />
    <code class="csharp">
      <span class="keyword">async</span> (x, y) =&gt; { <span class="keyword">await</span> Task.Yield(); }</code>
  </td>
  <td>
    <code class="csharp">TArg1, TArg2</code>
  </td>
  <td>None</td>
</tr>
<tr>
  <td>
    <code class="csharp">Func&lt;TArg1, TArg2, TResult&gt;</code>
    <br />
    <code class="csharp">Func&lt;TArg1, TArg2, Task&lt;TResult&gt;&gt;</code>
  </td>
  <td>
    <code class="csharp">(x, y) =&gt; { <span class="keyword">return</span> 13; }</code>
    <br />
    <code class="csharp">
      <span class="keyword">async</span> (x, y) =&gt; { <span class="keyword">await</span> Task.Yield(); <span class="keyword">return</span> 13; }</code>
  </td>
  <td>
    <code class="csharp">TArg1, TArg2</code>
  </td>
  <td>
    <code class="csharp">TResult</code>
  </td>
</tr>
  </table>
</div>

The table above ignores `async void` methods, which you [should be avoiding anyway](http://msdn.microsoft.com/en-us/magazine/jj991977.aspx). Async void methods are tricky because you _can_ assign a lambda like `async () => { await Task.Yield(); }` to a variable of type `Action`, even though the _natural_ type of that lambda is `Func<Task>`. Stephen Toub has written [more about the pitfalls of async void lambdas](http://blogs.msdn.com/b/pfxteam/archive/2012/02/08/10265476.aspx).

As a closing note, the C# compiler has been updated in VS2012 to correctly perform overload resolution in the presence of async lambdas. So, this kind of method declaration works fine:

{% highlight csharp %}
Task QueueAsync(Action action); // Sync, no return value
Task<T> QueueAsync<T>(Func<T> action); // Sync with return value
Task QueueAsync(Func<Task> action); // Async, no return value
Task<T> QueueAsync<T>(Func<Task<T>> action); // Async with return value
{% endhighlight %}