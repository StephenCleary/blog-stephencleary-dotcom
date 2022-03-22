---
layout: post
title: "A Tour of Task, Part 4: Id"
series: "A Tour of Task"
seriesTitle: "Id"
description: "An analysis of Task.Id and Task.CurrentId, and discussion of whether they should be used for asynchronous and/or parallel code."
---
## Id

{% highlight csharp %}
int Id { get; }
{% endhighlight %}

I've talked a bit about [task identifiers before]({% post_url 2013-03-21-a-few-words-on-taskid-and %}), so I'll just cover the high points here.

First, in spite of what the documentation says, the identifiers are not actually _unique_. They are pretty close, but not actually unique. The identifiers are generated on-demand, and will never be zero.

The task identifiers are useful if you're [reading the ETW events](http://msdn.microsoft.com/en-us/library/ee517329.aspx?WT.mc_id=DT-MVP-5000058) or [debugging with the Tasks window](http://msdn.microsoft.com/en-us/library/dd998369.aspx?WT.mc_id=DT-MVP-5000058), but they don't really have a use case outside of diagnostics and debugging.

Sometimes developers try to use the task identifiers as keys in a collection, to associate "extra data" with a task. This is an incorrect approach; usually what they're looking for is an [`async` local]({% post_url 2013-04-04-implicit-async-context-asynclocal %}).

## CurrentId

{% highlight csharp %}
static int? CurrentId { get; }
{% endhighlight %}

The `CurrentId` property returns the identifier of the currently-executing task, or `null` if no task is executing. The key word here is _executing_ - `CurrentId` only works for Delegate Tasks, not Promise Tasks.

In particular, the task returned by an `async` method is a Promise Task; it _logically_ represents the `async` method, but it is not a Delegate Task, and does not actually have the asynchronous code as its delegate. `CurrentId` may or may not be `null` within an `async` method, depending on the implementation details of the underlying `SynchronizationContext` or `TaskScheduler`.

<div class="alert alert-info" markdown="1">
<i class="fa fa-hand-o-right fa-2x pull-left"></i>

For more information, including sample code, see my [post on `CurrentId` in `async` methods]({% post_url 2013-03-28-taskcurrentid-in-async-methods %}){:.alert-link}.
</div>

In parallel code, it is _possible_ to use the current task identifier as a key into a collection to store task-local values or results, but that is a poor approach IMO. It's usually far better to use the PLINQ/`Parallel` built-in local value and result aggregation support.
