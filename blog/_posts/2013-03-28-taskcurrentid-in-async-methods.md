---
layout: post
title: "Task.CurrentId in Async Methods"
---
`Task.CurrentId` returns the identifier of the _currently executing_ `Task`, or `null` if there is no currently executing `Task`. So, what is `Task.CurrentId` in an `async` method?

{% highlight csharp %}
using System;
using System.Threading.Tasks;

class Program
{
    static string beforeYield, afterYield, mainAsync;

    static void Main(string[] args)
    {
        var task = MainAsync();
        task.Wait();
        mainAsync = task.Id.ToString();

        Console.WriteLine(beforeYield + "," + afterYield + "," + mainAsync);
        Console.ReadKey();
    }

    static async Task MainAsync()
    {
        beforeYield =Task.CurrentId.HasValue ? Task.CurrentId.ToString() : "null";
        await Task.Yield();
        afterYield =Task.CurrentId.HasValue ? Task.CurrentId.ToString() : "null";
    }
}
{% endhighlight %}

The output of this program is `null,null,1`. A lot of developers are surprised at this; after all, when you return a `Task` from an `async` method, doesn't that `Task` represent the complete method? Yes, it does _conceptually_ represent the `async` method, but it does not _literally execute_ the `async` method.

In fact, `Task` instances returned from `async` methods are not _executed_ at all; under the hood, they are `TaskCompletionSource<TResult>`-style (event-based) tasks, not delegate (code-based) tasks. (For more information on the differences, see my blog post on [creating tasks]({% post_url 2012-02-09-creating-tasks %}) or Stephen Toub's blog post on [the nature of TaskCompletionSource](https://devblogs.microsoft.com/pfxteam/the-nature-of-taskcompletionsourcetresult/?WT.mc_id=DT-MVP-5000058)). So `Task.CurrentId` returns `null` because there is no task actually _executing_.

As a reminder, code-based tasks are usually started with `Task.Run` to toss work onto the thread pool, or `TaskFactory.StartNew` to specify a scheduler where the task will run. But you can still end up surprised when you schedule an `async` method. Consider the modified program:

{% highlight csharp %}
using System;
using System.Threading.Tasks;

class Program
{
    static string beforeYield, afterYield, taskRun;

    static void Main(string[] args)
    {
        var task = Task.Run(() => MainAsync());
        task.Wait();
        taskRun = task.Id.ToString();

        Console.WriteLine(beforeYield + "," + afterYield + "," + taskRun);
        Console.ReadKey();
    }

    static async Task MainAsync()
    {
        beforeYield = Task.CurrentId.HasValue ? Task.CurrentId.ToString() : "null";
        await Task.Yield();
        afterYield = Task.CurrentId.HasValue ? Task.CurrentId.ToString() : "null";
    }
}
{% endhighlight %}

The output of this program is `1,null,2`. That pesky `null` is still there! The `null` comes into play because the `async` method is first executed as an actual task on the thread pool. However, after its `await`, it resumes as a regular delegate on the thread pool (not an actual task).

Note that this is an implementation detail. I'm not aware of any documentation stating that the first part of a thread pool `async` method runs as a task and the rest of it never will. It's likely that this behavior is just the result of the easiest and most efficient implementation.

I do make a different choice for my [AsyncContext](http://nitoasyncex.codeplex.com/wikipage?title=AsyncContext) type. I started off with a design similar to the thread pool task scheduler (executing bare `Action` delegates), but I found the code simplified quite nicely if I treated _everything_ as a `Task`. So this program will always use tasks:

{% highlight csharp %}
using System;
using System.Threading.Tasks;
using Nito.AsyncEx;

class Program
{
    static string beforeYield, afterYield;

    static void Main(string[] args)
    {
        AsyncContext.Run(() => MainAsync());

        Console.WriteLine(beforeYield + "," + afterYield);
        Console.ReadKey();
    }

    static async Task MainAsync()
    {
        beforeYield = Task.CurrentId.HasValue ? Task.CurrentId.ToString() : "null";
        await Task.Yield();
        afterYield = Task.CurrentId.HasValue ? Task.CurrentId.ToString() : "null";
    }
}
{% endhighlight %}

The output of this program is `1,2`, because `AsyncContext` wraps _everything_ into a `Task` before executing it, including `async` method continuations.

Note that this is also an implementation detail. Please do not depend on this behavior.

In conclusion, `Task.CurrentId` can be a bit tricky, especially within `async` methods. Personally, I find it best to only use `Task.CurrentId` in _parallel_ code and not in _asynchronous_ code.

