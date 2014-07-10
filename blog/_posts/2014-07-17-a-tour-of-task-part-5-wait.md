---
layout: post
title: "A Tour of Task, Part 5: Waiting"
series: "A Tour of Task"
seriesTitle: "Waiting"
description: "An analysis of Task.Wait, Task.WaitAll, Task.WaitAny, and Task.AsyncWaitHandle; and discussion of whether they should be used for asynchronous and/or parallel code."
---

Today, we'll look at a variety of ways that code can block on a task.

## Wait

There are five overloads of `Wait`:

{% highlight csharp %}
void Wait();
void Wait(CancellationToken);
void Wait(int);
void Wait(TimeSpan);
void Wait(int, CancellationToken);
{% endhighlight %}

These nicely simplify down to a single _logical_ method:

{% highlight csharp %}
void Wait() { Wait(-1, CancellationToken.None); }
void Wait(CancellationToken token) { Wait(-1, token); }
void Wait(int timeout) { Wait(timeout, CancellationToken.None); }
void Wait(TimeSpan timeout) { Wait(timeout.TotalMilliseconds); }
void Wait(int, CancellationToken);
{% endhighlight %}

`Wait` will block the calling thread until the task completes. 

## WaitAll and WaitAny

## AsyncWaitHandle



## Conclusion

The running total of useful members is still looking pretty bleak:

<div class="panel panel-default" markdown="1">

{:.table .table-striped}
|Type|Actual Members|Logical Members|Useful for async|Useful for parallel|
|-
|`Task`|16|9|0|0|
|`Task<T>`|16|9|0|0|

</div>
