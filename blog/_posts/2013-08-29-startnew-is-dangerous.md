---
layout: post
title: "StartNew is Dangerous"
---
I see a lot of code on blogs and in SO questions that use Task.Factory.StartNew to spin up work on a background thread. Stephen Toub has an [excellent blog article that explains why Task.Run is better than Task.Factory.StartNew](http://blogs.msdn.com/b/pfxteam/archive/2011/10/24/10229468.aspx), but I think a lot of people just haven't read it (or don't understand it). So, I've taken the same arguments, added some more forceful language, and we'll see how this goes. :)

StartNew does offer many more options than Task.Run, but it is quite dangerous, as we'll see. You should prefer Task.Run over Task.Factory.StartNew in async code.

## Why Use Task.Factory.StartNew?

There are only four reasons that you would _ever_ want to use Task.Factory.StartNew in async code:

1. You need a task that represents only the first (synchronous) part of an asynchronous method, instead of a task representing the entire method.
1. You need to specify a custom TaskScheduler, instead of using the thread pool scheduler.
1. You need to specify custom TaskCreationOptions.
1. You need to pass a state object to the delegate to reduce memory pressure from lambda variable capture.

Let's consider each of these in turn.

1. _You need a task that represents only the first (synchronous) part of an asynchronous method, instead of a task representing the entire method._ This can be useful if you're writing something like a task scheduler. In which case, you should ask yourself why you're writing Yet Another Task Scheduler in the first place. The vast majority of the time, you want a task that represents the entire asynchronous method instead of just the first bit.

2. _You need to specify a custom TaskScheduler, instead of using the thread pool scheduler._ If you need to pass a specialized TaskScheduler to StartNew, then reconsider your API design. There are better ways of creating tasks for a specific context (e.g., TaskFactory).

3. _You need to specify custom TaskCreationOptions._ Let's consider [each of the options](http://msdn.microsoft.com/en-us/library/system.threading.tasks.taskcreationoptions.aspx). AttachedToParent shouldn't be used in async tasks, so that's out. DenyChildAttach should _always_ be used with async tasks (hint: if you didn't already know that, then StartNew isn't the tool you need). DenyChildAttach is passed by Task.Run. HideScheduler might be useful in some really obscure scheduling scenarios but in general should be avoided for async tasks. That only leaves LongRunning and PreferFairness, which are both optimization hints that **should only be specified after application profiling**. I often see LongRunning misused in particular. In the vast majority of situations, the threadpool will adjust to any long-running task in 0.5 seconds - _without_ the LongRunning flag. Most likely, you don't really need it.

4. _You need to pass a state object to the delegate to reduce memory pressure from lambda variable capture._ Again, avoid the premature optimization. You should discover that you need this after doing memory profiling.

Now, I'm not saying to _never_ use Task.Factory.StartNew. If you're writing a low-level, general-purpose async library, then there are some rare situations where you do want to use StartNew. But for the vast majority of async code, Task.Factory.StartNew is a mistake.

> As a side note, the context of this discussion is _async code_. If you're writing _parallel code_ (e.g., [dynamic task-based parallelism](http://msdn.microsoft.com/en-us/library/ff963551.aspx)), then StartNew is the tool you want to use.

## Why Not to Use Task.Factory.StartNew?

We've covered some (very obscure) situations where Task.Factory.StartNew could be useful. Now, let's take a look at why you _shouldn't_ use Task.Factory.StartNew:

 1. Does not understand async delegates.
 1. Confusing default scheduler.

Let's consider each of these in turn.

1. _Does not understand async delegates._ This is actually the same as point 1 in the reasons why you _would_ want to use StartNew. The problem is that when you pass an async delegate to StartNew, it's natural to assume that the returned task represents that delegate. However, since StartNew does not understand async delegates, what that task actually represents is just the beginning of that delegate. This is one of the first pitfalls that coders encounter when using StartNew in async code.

2. _Confusing default scheduler._ OK, trick question time: in the code below, what thread does the method "A" run on?

{% highlight csharp %}
Task.Factory.StartNew(A);

private static void A() { }
{% endhighlight %}

Well, you know it's a trick question, eh? If you answered "a thread pool thread", I'm sorry, but that's not correct. "A" will run on whatever TaskScheduler is currently executing! OK, so let's give a bit of context to the question: in the code below, what thread does the method "A" run on?

{% highlight csharp %}
private void Form1_Load(object sender, EventArgs e)
{
    Task.Factory.StartNew(A);
}

private static void A() { }
{% endhighlight %}

In this case, "A" _will_ run on a thread pool thread! But it's important to understand _why_: Task.Factory.StartNew will first examine the current TaskScheduler. There is none, so it uses the thread pool TaskScheduler. Easy enough for the simple case, but let's consider a more realistic example:

{% highlight csharp %}
private void Form1_Load(object sender, EventArgs e)
{
    Compute(3);
}

private void Compute(int counter)
{
    // If we're done computing, just return.
    if (counter == 0)
        return;

    var ui = TaskScheduler.FromCurrentSynchronizationContext();
    Task.Factory.StartNew(() => A(counter))
        .ContinueWith(t =>
        {
            Text = t.Result.ToString(); // Update UI with results.

            // Continue working.
            Compute(counter - 1);
        }, ui);
}

private int A(int value)
{
    return value; // CPU-intensive work.
}
{% endhighlight %}

This is a bit more complex, but it uses a pattern common in older code that used tasks before async was introduced. The "Compute" method first checks to see if it's done computing; if it is, then it returns. If there's more work to do, then it throws "A" onto the thread pool to do the CPU-intensive calculations. "Compute" also registers a continuation, so that when "A" is complete, it will marshal back to the UI thread, update the UI with the results so far, and then continue computing.

Now, the question returns: what thread does "A" run on? Go ahead and walk through it; you should have enough knowledge at this point to figure out the answer.

Ready? The method "A" runs on a thread pool thread the first time, and then it runs on the UI thread the last two times.

Here's why: the first time through "Compute", there's no task scheduler and StartNew schedules "A" to a thread pool thread. However, when the first continuation runs, it _is_ running inside a task scheduler: the TaskScheduler.FromCurrentSynchronizationContext, which is the UI task scheduler. So when "Compute" executes the _second_ time, there _is_ a current task scheduler _which is picked up by StartNew_, and StartNew schedules "A" to that task scheduler (i.e., the UI thread). The same thing happens the third time through.

This is some pretty dangerous and unexpected behavior, IMO. In fact, many software teams have implemented rules for their source that _disallow StartNew unless you pass an explicit TaskScheduler_. And I think that's a great idea.

Unfortunately, the only overloads for StartNew that take a TaskScheduler also require you to specify the CancellationToken and TaskCreationOptions. This means that in order to use Task.Factory.StartNew to reliably, predictably queue work to the thread pool, you have to use an overload like this:

{% highlight csharp %}
Task.Factory.StartNew(A, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
{% endhighlight %}

And really, that's kind of ridiculous. Just use `Task.Run(() => A());`.

