---
layout: post
title: "Task.Run Etiquette Examples: Don't Use Task.Run in the Implementation"
series: "Task.Run Etiquette"
seriesTitle: "Task.Run in the Implementation"
---
Last time we looked at [using `Task.Run` for the wrong thing]({% post_url 2013-11-06-taskrun-etiquette-examples-using %}) (code that is not CPU-bound). So let's move on to the proper use case of `Task.Run`: CPU-bound code. We start off with some existing code, which synchronously does some heavy calculations.

{% highlight csharp %}
class MyService
{
  public int CalculateMandelbrot()
  {
    // Tons of work to do in here!
    for (int i = 0; i != 10000000; ++i)
      ;
    return 42;
  }
}

...

private void MyButton_Click(object sender, EventArgs e)
{
  // Blocks UI thread! :(
  myService.CalculateMandelbrot();
}
{% endhighlight %}

Now, we want to use this from a UI thread, but this method will block our thread. This **is** a problem that should be solved using `Task.Run`. Doing these calculations is a CPU-bound operation.

{% highlight csharp %}
// Warning: bad code!
class MyService
{
  public Task<int> CalculateMandelbrotAsync()
  {
    return Task.Run(() =>
    {
      // Tons of work to do in here!
      for (int i = 0; i != 10000000; ++i)
        ;
      return 42;
    });
  }
}

...

private async void MyButton_Click(object sender, EventArgs e)
{
  // Does not block UI thread! Yay!
  await myService.CalculateMandelbrotAsync();
}
{% endhighlight %}

At first glance, it may look like this solves the problem. And it does solve _this_ problem, but it does not do it in the best way.

Let's say that this service is a generic dll that can be used inside any .NET application. It has an API, and **APIs are all about etiquette**.

How would an ASP.NET application react to this change?

Let's introduce a simple ASP.NET MVC controller that returns a view using the (original, synchronous) service.

{% highlight csharp %}
class MyService
{
  public int CalculateMandelbrot()
  {
    // Tons of work to do in here!
    for (int i = 0; i != 10000000; ++i)
      ;
    return 42;
  }
}

...

public class MandelbrotController: Controller
{
  public ActionResult Index()
  {
    var result = myService.CalculateMandelbrot();
    return View(result);
  }
}
{% endhighlight %}

So far, so good. When a request comes in, the controller uses the service to (synchronously) calculate the view data. A single request thread is used the entire time during that calculation.

But the desktop app required a change in the service. It's now `async`, which is "no problem" because ASP.NET MVC naturally supports asynchronous actions.

{% highlight csharp %}
// Warning: bad code!
class MyService
{
  public Task<int> CalculateMandelbrotAsync()
  {
    return Task.Run(() =>
    {
      // Tons of work to do in here!
      for (int i = 0; i != 10000000; ++i)
        ;
      return 42;
    });
  }
}

...

public class MandelbrotController: Controller
{
  public async Task<ActionResult> IndexAsync()
  {
    var result = await myService.CalculateMandelbrotAsync();
    return View(result);
  }
}
{% endhighlight %}

And when we do testing, it works! Unfortunately, this change introduced a performance problem.

With the original (synchronous) code, only one thread was used to process the request, from beginning to end. That's a heavily-optimized ASP.NET scenario. With this `async` code using `Task.Run`, instead of a single request thread, this is what happens:

- The request starts processing on an ASP.NET thread.
- `Task.Run` starts a task on the thread pool to do the calculations. The ASP.NET thread pool has to deal with (unexpectedly) losing one of its threads for the duration of this request.
- The original request thread is returned to the ASP.NET thread pool.
- When the calculation is complete, that thread completes the request and is returned to the ASP.NET thread pool. The ASP.NET thread pool has to deal with (unexpectedly) getting another thread.

This will _work_ correctly, but it's not at all _efficient_.

There are (at least) four efficiency problems introduced as soon as you use `await` with `Task.Run` in ASP.NET:

 - Extra (unnecessary) thread switching to the `Task.Run` thread pool thread. Similarly, when that thread finishes the request, it has to enter the request context (which is not an actual thread switch but does have overhead).
 - Extra (unnecessary) garbage is created. Asynchronous programming is a tradeoff: you get increased responsiveness at the expense of higher memory usage. In this case, you end up creating more garbage for the asynchronous operations that is totally unnecessary.
 - The ASP.NET thread pool heuristics are thrown off by `Task.Run` "unexpectedly" borrowing a thread pool thread. I don't have a lot of experience here, but my gut instinct tells me that the heuristics should recover well if the unexpected task is really short and would not handle it as elegantly if the unexpected task lasts more than two seconds.
 - ASP.NET is not able to terminate the request early, i.e., if the client disconnects or the request times out. In the synchronous case, ASP.NET knew the request thread and could abort it. In the asynchronous case, ASP.NET is not aware that the secondary thread pool thread is "for" that request. It _is_ possible to fix this by using cancellation tokens, but that's outside the scope of this blog post.

If you have multiple calls to `Task.Run`, then the performance problems are compounded. On a busy server, this kind of implementation can kill scalability.

That's why one of the principles of ASP.NET is to **avoid using thread pool threads** (except for the request thread that ASP.NET gives you, of course). More to the point, this means that **ASP.NET applications should avoid `Task.Run`**.

Whew! OK, so now we know what the problem is with that implementation. The plain fact is that ASP.NET prefers synchronous methods if the operation is CPU-bound. And this is also true for other scenarios: Console applications, background threads in desktop applications, etc. In fact, the only place we really _need_ an asynchronous calculation is when we call it from the UI thread.

But watch out! There's another pitfall just ahead...

## Using Task.Run for Asynchronous Wrappers

Let's continue the "Mandelbrot" example. We've learned that some clients prefer asynchronous APIs for CPU-bound code and others prefer synchronous APIs for CPU-bound code.

So, let's be good API citizens ("APIs are all about etiquette") and keep the new synchronous version along with the asynchronous version. That way there's no breaking changes, right? It's simple enough, and we can even implement it easily so that there's no code duplication!

{% highlight csharp %}
// Warning: bad code!
class MyService
{
  public int CalculateMandelbrot()
  {
    // Tons of work to do in here!
    for (int i = 0; i != 10000000; ++i)
      ;
    return 42;
  }

  public Task<int> CalculateMandelbrotAsync()
  {
    return Task.Run(() => CalculateMandelbrot());
  }
}
{% endhighlight %}

Sweet. The UI app has its nice asynchronous method, and the ASP.NET app has its original synchronous method. Easy! And there are many other examples where synchronous and asynchronous APIs exist side-by-side, so developers are already used to this! But the fact is that **using `Task.Run` for asynchronous wrappers is a code smell**.

The problem with this approach is what is implied by this API design. Consider all the examples where synchronous and asynchronous APIs exist side-by-side, e.g., Entity Framework 6, or the `WebClient` class. Notice anything? _They're all naturally asynchronous!_ Not a single one of them is _CPU-bound_.

When a developer sees two methods in an API `Method` and `MethodAsync`, the convention is that they represent a naturally-asynchronous operation. In other words, the developer expects that `MethodAsync` is the "natural" implementation and that `Method` is essentially a synchronous (blocking) equivalent of that operation. That API implies that `Method` will at some point have the calling thread enter a wait state as it blocks for the naturally-asynchronous operation to complete.

Let's make this a bit more practical. When a new ASP.NET developer approaches our service API, they'll see `CalculateMandelbrot` and `CalculateMandelbrotAsync` (and let's pretend that our method names are not so obviously CPU-bound). If they're familiar with asynchronous APIs at all, they'll assume that this calculation is naturally asynchronous. And on ASP.NET, you _should_ use naturally-asynchronous methods. Therefore, they will choose what they think is the naturally-asynchronous `CalculateMandelbrotAsync` method and end up inheriting the performance problems discussed earlier.

`CalculateMandelbrotAsync` is what I call "fake-asynchronous" because it's just a thread pool wrapper around a synchronous operation. But when developers see that API, they assume that it is a naturally-asynchronous operation.

This is just a brief description and I only covered one facet of this problem. Stephen Toub has an excellent blog post explaining in detail [why you should not write asynchronous wrappers for synchronous methods](https://devblogs.microsoft.com/pfxteam/should-i-expose-asynchronous-wrappers-for-synchronous-methods/).

## OK, enough about the _wrong_ solutions? How do we fix this the _right_ way???

Back up to the original problem for a moment. What is the problem? The UI thread is blocked. How did we solve it? By changing the service. Who needs the asynchronous API? Only the UI thread. Sounds like we just seriously violated the "Separation of Concerns" principle.

The key here is that the solution does not belong in the service. It belongs in the UI layer itself. Let the UI layer solve its own problems and leave the service out of it.

{% highlight csharp %}
class MyService
{
  public int CalculateMandelbrot()
  {
    // Tons of work to do in here!
    for (int i = 0; i != 10000000; ++i)
      ;
    return 42;
  }
}

...

private async void MyButton_Click(object sender, EventArgs e)
{
  await Task.Run(() => myService.CalculateMandelbrot());
}
{% endhighlight %}

Now the service API is clean (it exposes a synchronous API for a naturally-synchronous, CPU-bound method), it works for all consumers (e.g., ASP.NET), and the _UI_ layer is responsible for not blocking the _UI_ thread.

Conclusion: **do not use `Task.Run` in the _implementation_ of the method; instead, use `Task.Run` to _call_ the method**.

