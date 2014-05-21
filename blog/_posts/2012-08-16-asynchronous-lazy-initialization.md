---
layout: post
title: "Asynchronous Lazy Initialization"
---
When you have a lazy-created value, it's often useful to lazy-create it in an asynchronous fashion. A simple Lazy<T> provides lazy initialization, but the initialization executes synchronously when the value is created.

Stephen Toub first introduced this approach [in a blog post way back in January, 2011](http://blogs.msdn.com/b/pfxteam/archive/2011/01/15/10116210.aspx). This is his code, which I've updated, documented, and tweaked slightly:

{% highlight csharp %}
 
/// <summary>
/// Provides support for asynchronous lazy initialization. This type is fully threadsafe.
/// </summary>
/// <typeparam name="T">The type of object that is being asynchronously initialized.</typeparam>
public sealed class AsyncLazy<T>
{
    /// <summary>
    /// The underlying lazy task.
    /// </summary>
    private readonly Lazy<Task<T>> instance;

    /// <summary>
    /// Initializes a new instance of the <see cref="AsyncLazy&lt;T&gt;"/> class.
    /// </summary>
    /// <param name="factory">The delegate that is invoked on a background thread to produce the value when it is needed.</param>
    public AsyncLazy(Func<T> factory)
    {
        instance = new Lazy<Task<T>>(() => Task.Run(factory));
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AsyncLazy&lt;T&gt;"/> class.
    /// </summary>
    /// <param name="factory">The asynchronous delegate that is invoked on a background thread to produce the value when it is needed.</param>
    public AsyncLazy(Func<Task<T>> factory)
    {
        instance = new Lazy<Task<T>>(() => Task.Run(factory));
    }

    /// <summary>
    /// Asynchronous infrastructure support. This method permits instances of <see cref="AsyncLazy&lt;T&gt;"/> to be await'ed.
    /// </summary>
    public TaskAwaiter<T> GetAwaiter()
    {
        return instance.Value.GetAwaiter();
    }

    /// <summary>
    /// Starts the asynchronous initialization, if it has not already started.
    /// </summary>
    public void Start()
    {
        var unused = instance.Value;
    }
}
{% endhighlight %}

The idea is to have a lazy-initialized task, which represents the initialization of the resource.

The factory delegate passed to the constructor can be either synchronous (Func<T>) or asynchronous (Func<Task<T>>); either way, it will be run on a thread pool thread. It will not be executed more than once, even when multiple threads attempt to start it simultaneously (this is guaranteed by the Lazy type).

There are two "triggers" which can start the initialization: awaiting an AsyncLazy<T> instance or explicitly calling Start. When the factory delegate completes, the value is available, and any methods awaiting the AsyncLazy<T> instance receive the value.

It takes a few minutes to wrap your head around the theory, but it's really easy in practice:

{% highlight csharp %}

private static readonly AsyncLazy<MyResource> myResource = new AsyncLazy<MyResource>(
    () => new MyResource()
    // or:
    // async () => { var ret = new MyResource(); await ret.InitAsync(); return ret; }
);

...

public async Task UseResource()
{
  MyResource resource = await myResource;
  ...
}
{% endhighlight %}

## Update, 2012-09-30

The AsyncLazy<T> type is now part of [Nito.AsyncEx](http://nitoasyncex.codeplex.com/), which you can get via NuGet.

