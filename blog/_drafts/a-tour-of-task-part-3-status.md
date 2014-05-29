---
layout: post
title: "A Tour of Task, Part 3: Status"
series: "A Tour of Task"
seriesTitle: "Status"
---
## Status

If you view a `Task` as a state machine, then the `Status` property represents the current state. Different types of tasks take different paths through the state machine.

<div class="alert alert-info" markdown="1">
<i class="fa fa-hand-o-right fa-2x pull-left"></i>

As usual, this post is just taking [what Stephen Toub already said](http://blogs.msdn.com/b/pfxteam/archive/2009/08/30/9889070.aspx), expounding on it a bit, and drawing some ugly pictures. :)
</div>

### Delegate Tasks



{:.center}
[![]({{ site_url }}/assets/TaskStates.Delegate.png)]({{ site_url }}/assets/TaskStates.Delegate.png)


{:.center}
[![]({{ site_url }}/assets/TaskStates.Promise.png)]({{ site_url }}/assets/TaskStates.Promise.png)


## Conclusion

The running total of useful members:

<div class="panel panel-default" markdown="1">

{:.table .table-striped}
|Type|Actual Members|Logical Members|Useful for async|Useful for parallel|
|-
|`Task`|10|3|0|0|
|`Task<T>`|10|3|0|0|

</div>
