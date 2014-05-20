---
layout: post
title: "Task.Run Etiquette Examples: Don't Use Task.Run for the Wrong Thing"
---
I had quite a few comments on [my last post]({% post_url 2013-10-17-taskrun-etiquette-and-proper-usage %}) asking for more explicit examples of Correct vs. Incorrect `Task.Run` usage.

First, let's consider the "beginner's error". This is where the user misuses `Task.Run` because they want to make their code "asynchronous" but aren't sure how to do it.

This kind of user starts off with existing code, which usually does some kind of synchronous work (often database access or a web request).

{% highlight csharp %}
class MyService
{
  public int RetrieveValue(int id)
  {
    // Do some blocking work.
    // DB access, web request, etc.
    Thread.Sleep(500);
    return 42;
  }
}
{% endhighlight %}

They've read a bit about how `async` helps in those areas, and decide to give it a spin. "Let's see if I can figure out this `async` thing. I'll just add an `async` and see what happens. Oh, I have to change the return type to `Task`, too."

{% highlight csharp %}
// Warning: bad code!
class MyService
{
  public async Task<int> RetrieveValueAsync(int id)
  {
    // Do some blocking work.
    // DB access, web request, etc.
    Thread.Sleep(500);
    return 42;
  }
}
{% endhighlight %}

"Now the compiler is complaining that I'm not using `await`. OK, so what can I await? [Google-Fu] Ah, `Task.Run` looks promising!"

{% highlight csharp %}
// Warning: bad code!
class MyService
{
  public async Task<int> RetrieveValueAsync(int id)
  {
    await Task.Run(() =>
    {
      // Do some blocking work.
      // DB access, web request, etc.
      Thread.Sleep(500);
      return 42;
    });
  }
}
{% endhighlight %}

"Hey, it worked! My UI thread is no longer blocked! Yay for `async`!"

Unfortunately, this is a misuse of `Task.Run`. The problem is that it's not _truly_ asynchronous. It's still executing blocking work, blocking a thread pool thread the whole time the operation is in progress.

The proper approach is to _change the blocking call to an asynchronous call first_ and then _allow `async` to grow from there_. So, starting from the same point, we first change the blocking call to an asynchronous call. In the real world, this would be like replacing `WebClient` with `HttpClient` or converting your Entity Framework queries to be asynchronous. In this example, I'm replacing `Thread.Sleep` with `Task.Delay`.

{% highlight csharp %}
// Warning: bad code!
class MyService
{
  public int RetrieveValue(int id)
  {
    // Converted to nonblocking work.
    // DB access, web request, etc.
    await Task.Delay(500);
    return 42;
  }
}
{% endhighlight %}

Now we're getting a compiler error, and we need to make the method `async`.

{% highlight csharp %}
class MyService
{
  public async Task<int> RetrieveValueAsync(int id)
  {
    // Converted to nonblocking work.
    // DB access, web request, etc.
    await Task.Delay(500);
    return 42;
  }
}
{% endhighlight %}

And now we end up with a more correct implementation.

Note that this was an example of using `Task.Run` for the _wrong thing_. To reiterate a sentence from my last post, **use Task.Run to call CPU-bound methods.** Do _not_ use it just to "provide something awaitable for my async method to use".

