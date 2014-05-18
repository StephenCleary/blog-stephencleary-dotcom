---
layout: post
title: "A Tour of Task, Part 0: Overview"
tags: ["async", ".NET", "Task members"]
---


I recently posted a poll on [The Twitter](https://www.youtube.com/watch?v=VqQrUngBDxU); here it is with all the responses:



{:.center}
![](http://2.bp.blogspot.com/-wpQPbufSHic/U1kR83AyjrI/AAAAAAAALUI/4i3XPJZjc-A/s1600/Poll.png)



It's unanimous! This post is the first in a series that will take a look at all the `Task` members (as of .NET 4.5).



## A Bit of Task History



One of the biggest stumbling blocks to developers learning `async` is actually the `Task` type itself. Most developers fall into one of two categories:




- Developers who have used `Task` and the [TPL (Task Parallel Library)](http://msdn.microsoft.com/en-us/library/dd460717(v=vs.110).aspx) since it was introduced in .NET 4.0. These developers are familiar with `Task` and [how it is used in parallel processing](http://msdn.microsoft.com/en-us/library/ff963553.aspx). **The danger** that these developers face is that `Task` (as it is used by the TPL) is pretty much _completely different_ than `Task` (as it is used by `async`).
- Developers who have never heard of `Task` until `async` came along. To them, `Task` is just a part of `async` - one more (fairly complicated) thing to learn. "Continuation" is a foreign word. **The danger** that these developers face is assuming that every member of `Task` is applicable to `async` programming, which is most certainly not the case.




The `async` team at Microsoft did consider writing their own "Promise" type that would act as an asynchronous task, but the `Task` type was too tempting. `Task` actually did support promise-style asynchronous tasks (somewhat awkwardly) even in .NET 4.0, and it only took a bit of extension for it to support `async` fully. Also, by merging this "Promise" with the existing `Task` type, we end up with a nice unification: it's trivially easy to kick off some operation on a background thread and treat it asynchronously. No conversion from `Task` to "Promise" is necessary.





The downside to using the same type is that it does create some developer confusion. As noted above, developers who have used `Task` in the past tend to try to use it the same way in the `async` world (which is wrong); and developers who have not used `Task` in the past face a bewildering selection of `Task` members, almost all of which should not be used in the `async` world.





So, that's how we got to where we are today. This blog series will go through all the various `Task` members (yes, _all_ of them), and explain the purpose behind each one. As we'll see, the vast majority of `Task` members have no place in `async` code.



## Two Types of Task



There are two types of tasks. The first type is a Delegate Task; this is a task that has code to run. The second type is a Promise Task; this is a task that represents some kind of event or signal. Promise Tasks are often I/O-based signals (e.g., "the HTTP download has completed"), but they can actually represent anything (e.g., "the 10-second timer has expired").





In the TPL world, most tasks were Delegate Tasks (with some support for Promise Tasks). When code does parallel processing, the various Delegate Tasks are divided up among different threads, which then actually _execute_ the code in those Delegate Tasks. In the `async` world, most tasks are Promise Tasks (with some support for Delegate Tasks). When code does an `await` on a Promise Task, [there is no thread](http://blog.stephencleary.com/2013/11/there-is-no-thread.html) tied up waiting for that promise to complete.





In the past, I've used the terms "code-based Task" and "event-based Task" to describe the two kinds of tasks. In this series, I will try to use the terms "Delegate Task" and "Promise Task" to distinguish the two.



<!--

<h4>Historical Task Use Cases</h4>

<p>Before we dive into the <code class="csharp">Task</code> members, I want to briefly describe the use cases for the <code class="csharp">Task</code> type in .NET 4.0 code. If you're one of the "never heard of <code class="csharp">Task</code>" developers, feel free to skip this section (unless you need to maintain <code class="csharp">Task</code>-based .NET 4.0-era code).<p>

<h5>Queue a Delegate to Another Thread</h5>

<p>Probably the most common use case was just a one-off "queue this work to another thread" kind of call. Usually, the "another thread" was just "some thread pool thread", and the code looks something like this:</p>



<h5>Promise Tasks</h5>

<h5>Pipelines</h5> - ?

<h5>Dynamic Task Parallelism</h5>

<p>Parallel processing can be broadly divided into <a href="http://msdn.microsoft.com/en-us/library/vstudio/dd537608(v=vs.110).aspx">data parallelism</a> and <a href="http://msdn.microsoft.com/en-us/library/vstudio/dd537609(v=vs.110).aspx">task parallelism</a>.</p>

<p>Data parallelism is the most common: that's when you have a sequence of values that you need to process in parallel. Data parallelism is handled quite nicely (and exhaustively) by <a href="http://msdn.microsoft.com/en-us/library/vstudio/system.threading.tasks.parallel"><code class="csharp">Parallel.For</code>, <code class="csharp">Parallel.ForEach</code></a>, and <a href="http://msdn.microsoft.com/en-us/library/vstudio/dd460688(v=vs.110).aspx">Parallel LINQ</a>.</p>

<p>Task parallelism is more rare: that's when you have a number of delegates that you need to execute in parallel. Basic task parallelism is supported by the <a href="http://msdn.microsoft.com/en-us/library/vstudio/system.threading.tasks.parallel"><code class="csharp">Parallel.Invoke</code> methods</a>. That support is only "basic" because you have to know at the beginning of the parallel call how many delegates to schedule. There's another scenario called <a href="http://msdn.microsoft.com/en-us/library/ff963551.aspx">dynamic task parallelism</a> that is extremely flexible and enables you to dynamically add more Delegate Tasks to the parallel processing.</p>

<p>Note how far we got before mentioning Delegate Tasks. The most common parallelism (data parallelism) should be using higher-level abstractions (which do use Delegate Tasks under the covers). The only parallel scenario where you should be dealing with the <code class="csharp">Task</code> type directly is the quite rare dynamic task parallelism scenario. I've seen a number of projects in my time that attempted to use <code class="csharp">Task</code> directly instead of <code class="csharp">Parallel</code> or PLINQ, and ended up way overcomplicated as a result.</p>

-->