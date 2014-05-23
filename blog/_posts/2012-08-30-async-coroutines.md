---
layout: post
title: "Async Coroutines"
---
We've been [introduced to scheduled concurrency]({% post_url 2012-08-23-async-and-scheduled-concurrency %}). Now how about a quick example?

Today we're going to use the exclusive scheduler to create a simplistic kind of round-robin co-routine, similar to [Jon Skeet's EduAsync Coroutines](http://msmvps.com/blogs/jon_skeet/archive/2011/06/22/eduasync-part-13-first-look-at-coroutines-with-async.aspx).

<div class="alert alert-danger" markdown="1">
<i class="fa fa-exclamation-triangle fa-2x pull-left"></i>

Please note: this is only "playing around" code. Do not use this in production!
</div>

There isn't that much to it. We define three co-routines with slightly different behavior to make it a little interesting: FirstCoroutine yields twice, SecondCoroutine yields three times, and ThirdCoroutine yields once.

{% highlight csharp %}

using System;
using System.Threading.Tasks;

partial class Program
{
    // The first co-routine yields twice.
    private static async Task FirstCoroutine()
    {
        Console.WriteLine("Starting FirstCoroutine");
        Console.WriteLine("Yielding from FirstCoroutine...");

        await Task.Yield();

        Console.WriteLine("Returned to FirstCoroutine");
        Console.WriteLine("Yielding from FirstCoroutine again...");

        await Task.Yield();

        Console.WriteLine("Returned to FirstCoroutine again");
        Console.WriteLine("Finished FirstCoroutine");
    }

    // The second co-routine yields three times.
    private static async Task SecondCoroutine()
    {
        Console.WriteLine("  Starting SecondCoroutine");
        Console.WriteLine("  Yielding from SecondCoroutine...");

        await Task.Yield();

        Console.WriteLine("  Returned to SecondCoroutine");
        Console.WriteLine("  Yielding from SecondCoroutine again...");

        await Task.Yield();

        Console.WriteLine("  Returned to SecondCoroutine");
        Console.WriteLine("  Yielding from SecondCoroutine again...");

        await Task.Yield();

        Console.WriteLine("  Returned to SecondCoroutine again");
        Console.WriteLine("  Finished SecondCoroutine");
    }

    // The third co-routine yields once.
    private static async Task ThirdCoroutine()
    {
        Console.WriteLine("    Starting ThirdCoroutine");
        Console.WriteLine("    Yielding from ThirdCoroutine...");

        await Task.Yield();

        Console.WriteLine("    Returned to ThirdCoroutine");
        Console.WriteLine("    Finished ThirdCoroutine");
    }
}
{% endhighlight %}

To run the co-routines exclusively, we create a TaskFactory wrapping a ConcurrentExclusiveSchedulerPair.ExclusiveScheduler. We also create a convenience method RunCoroutineAsync, which takes a co-routine delegate and executes it on that scheduler.

{% highlight csharp %}

using System;
using System.Threading.Tasks;

partial class Program
{
    static void Main(string[] args)
    {
        var task = MainAsync();
        task.Wait();
        Console.ReadKey();
    }

    /// <summary>
    /// A task factory using an exclusive scheduler.
    /// </summary>
    private static TaskFactory coroutineFactory = new TaskFactory(new ConcurrentExclusiveSchedulerPair().ExclusiveScheduler);

    /// <summary>
    /// Executes a co-routine using an exclusive scheduler.
    /// </summary>
    private static async Task RunCoroutineAsync(Func<Task> coroutine)
    {
        await await coroutineFactory.StartNew(coroutine);
    }

    /// <summary>
    /// Starts three co-routines and awaits for them all to complete.
    /// </summary>
    static async Task MainAsync()
    {
        var coroutines = new[]
        {
            RunCoroutineAsync(FirstCoroutine),
            RunCoroutineAsync(SecondCoroutine),
            RunCoroutineAsync(ThirdCoroutine),
        };

        await Task.WhenAll(coroutines);
    }
}
{% endhighlight %}

The tricky part in this code is the double-await in RunCoroutineAsync. This is a [normal pattern](http://blogs.msdn.com/b/pfxteam/archive/2011/10/24/10229468.aspx) when you use TaskFactory.StartNew with asynchronous delegates (alternatively, you could use Task.Unwrap).

Logically, the "coroutine" parameter to RunCoroutineAsync is an asynchronous delegate (referring to one of the async co-routine methods). When we pass it to StartNew, we get back a Task<Task> representing the _starting_ of that asynchronous delegate on our exclusive scheduler. The inner Task represents the _completion_ of that asynchronous delegate. So the "await await" is used because we want RunCoroutineAsync to complete only when the asynchronous delegate completes.

If we execute this program, we can clearly see the co-routine behavior:

    Starting FirstCoroutine
    Yielding from FirstCoroutine...
      Starting SecondCoroutine
      Yielding from SecondCoroutine...
        Starting ThirdCoroutine
        Yielding from ThirdCoroutine...
    Returned to FirstCoroutine
    Yielding from FirstCoroutine again...
      Returned to SecondCoroutine
      Yielding from SecondCoroutine again...
        Returned to ThirdCoroutine
        Finished ThirdCoroutine
    Returned to FirstCoroutine again
    Finished FirstCoroutine
      Returned to SecondCoroutine
      Yielding from SecondCoroutine again...
      Returned to SecondCoroutine again
      Finished SecondCoroutine

Just one final word. There are benign race conditions in this code: e.g., it's possible that FirstCoroutine may run and yield to itself before SecondCoroutine even starts. The ExclusiveScheduler does not make guarantees about queueing or fairness (though it does _try_ to be fair) - it only guarantees exclusive scheduling.

