---
layout: post
title: "Async OOP 2: Constructors"
series: "Async OOP"
seriesTitle: "Async OOP 2: Constructors"
---
Asynchronous construction poses an interesting problem. It would be useful to be able to use `await` in a constructor, but this would mean that the constructor would have to return a `Task<T>` representing a value that will be constructed in the future, instead of a constructed value. This kind of concept would be very difficult to work into the existing language.



The bottom line is that `async` constructors are not allowed, so let's explore some alternatives.



## Factory Pattern

Constructors cannot be `async`, but static methods can. It's pretty easy to have a static creation method, making the type its own factory:



{% highlight csharp %}public sealed class MyClass
{
  private MyData asyncData;
  private MyClass() { ... }

  private async Task<MyClass> InitializeAsync()
  {
    asyncData = await GetDataAsync();
    return this;
  }

  public static Task<MyClass> CreateAsync()
  {
    var ret = new MyClass();
    return ret.InitializeAsync();
  }
}

public static async Task UseMyClassAsync()
{
  MyClass instance = await MyClass.CreateAsync();
  ...
}
{% endhighlight %}

It's possible to have `Create` do all the initialization work, but I prefer to have the `async InitializeAsync` method.



The factory method is the most common approach to asynchronous construction, but there are other approaches that are useful in some situations.



## AsyncLazy (for Resources)

If the instance you're creating is a _shared resource_, then you can use [asynchronous lazy initialization]({% post_url 2012-08-16-asynchronous-lazy-initialization %}) to create your shared instance:



{% highlight csharp %}private static AsyncLazy<MyResource> resource = new AsyncLazy<MyResource>(async () =>
{
  var data = await GetResource();
  return new MyResource(data);
});

public static async Task UseResourceAsync()
{
  MyResource res = await resource;
}
{% endhighlight %}

`AsyncLazy<T>` is a great fit for _resources_; in this example, `resource` will start being constructed the first time it's `await`ed. Any other methods that `await` it will tie into the same construction, and when the construction is complete, all waiters are released. Any `await`s after the construction is complete continue immediately since the value is already available.



This approach does _not_ work well if the instance is not used as a shared resource. If the instance is not a shared resource, you should another approach instead.



## The Asynchronous Initialization Pattern

The best approaches to asynchronous construction have already been covered: asynchronous factory methods and `AsyncLazy<T>`. These are the best approaches because you never expose an uninitialized instance.



However, there are times when you really _need_ a constructor, e.g., when some other component is using reflection to create an instance of your type. This includes data binding, IoC and DI frameworks, `Activator.CreateInstance`, etc.



In these cases, you _must_ return an uninitialized instance, but you can mitigate this by applying a common pattern: each object that needs asynchronous initialization will expose a property `Task Initialization { get; }` that will contain the results of the asynchronous initialization.



### The Pattern

If you want to treat asynchronous initialization as an implementation detail, you can (optionally) define a "marker" interface for types that use asynchronous initialization:



{% highlight csharp %}/// <summary>
/// Marks a type as requiring asynchronous initialization and provides the result of that initialization.
/// </summary>
public interface IAsyncInitialization
{
    /// <summary>
    /// The result of the asynchronous initialization of this instance.
    /// </summary>
    Task Initialization { get; }
}
{% endhighlight %}

The pattern for asynchronous initialization then looks like this:



{% highlight csharp %}public sealed class MyFundamentalType : IAsyncInitialization
{
    public MyFundamentalType()
    {
        Initialization = InitializeAsync();
    }

    public Task Initialization { get; private set; }

    private async Task InitializeAsync()
    {
        // Asynchronously initialize this instance.
        await Task.Delay(100);
    }
}
{% endhighlight %}

This pattern is quite simple, but it gives us some important semantics:




- The initialization is started in the constructor (when we call `InitializeAsync`).
- The completion of the initialization is exposed (via the `Initialization` property).
- Any exceptions raised from the asynchronous initialization will be captured and placed on the `Initialization` property.


An instance of this type can be (manually) constructed like this:



{% highlight csharp %}var myInstance = new MyFundamentalType();
// Danger: the instance is not initialized here!
await myInstance.Initialization;
// OK: the instance is initialized now.
{% endhighlight %}

### Composing with Asynchronous Initialization

It's easy to create another type that depends on this fundamental type (i.e., asynchronous composition):



{% highlight csharp %}public sealed class MyComposedType : IAsyncInitialization
{
    private readonly MyFundamentalType _fundamental;

    public MyComposedType(MyFundamentalType fundamental)
    {
        _fundamental = fundamental;
        Initialization = InitializeAsync();
    }

    public Task Initialization { get; private set; }

    private async Task InitializeAsync()
    {
        // Asynchronously wait for the fundamental instance to initialize.
        await _fundamental.Initialization;

        // Do our own initialization (synchronous or asynchronous).
        await Task.Delay(100);
    }
}
{% endhighlight %}

The main difference is that we wait for all of our components to initialize before we proceed with our initialization. Alternatively, you could proceed with some initialization and only wait on particular components when you need those particular ones to complete. However, every component should be initialized by the end of `InitializeAsync`.



There are a few key semantics that we get from this pattern when composing:




 - A composed type's initialization isn't complete until all its components' initializations are complete.
 - Any errors from component initializations are surfaced up through the composed type.
 - A composed type supports asynchronous initialization, and can be composed in turn just like any other type supporting asynchronous initialization.


In addition, if you're using the `IAsyncInitialization` "marker" interface, you can test for that and asynchronously initialize instances that are provided to you by IoC/DI. This slightly complicates your `InitializeAsync` but allows you to treat asynchronous initialization as an implementation detail. For example, if `_fundamental` is of type `IMyFundamentalType`:



{% highlight csharp %}private async Task InitializeAsync()
{
    // Asynchronously wait for the fundamental instance to initialize if necessary.
    var asyncFundamental = _fundamental as IAsyncInitialization;
    if (asyncFundamental != null)
        await asyncFundamental.Initialization;

    // Do our own initialization (synchronous or asynchronous).
    await Task.Delay(100);
}
{% endhighlight %}

### Top-Level Handling

We've covered how to write "fundamental" types with asynchronous initialization and how to "compose" them into other types with asynchronous initialization. Eventually, you'll need to consume the high-level types that support asynchronous initialization.



In many dynamic-creation scenarios (such as IoC/DI/`Activator.CreateInstance`), you can just check for `IAsyncInitialization` and initialize it directly:



{% highlight csharp %}object myInstance = ...;
var asyncInstance = myInstance as IAsyncInitialization;
if (asyncInstance != null)
    await asyncInstance.Initialization;
{% endhighlight %}

However, if you're creating a type by data binding, or using IoC/DI to inject your view models into your view's data context, then you don't really have a place where you interact with the top-level instance. Data binding will take care of updating the UI when the initialization completes _unless the initialization fails_, so you'll need to surface failures. Unfortunately, `Task` does not implement `INotifyPropertyChanged`, so the task completion is not surfaced automatically. You can use a type like [NotifyTaskCompletion type in the AsyncEx library](http://nitoasyncex.codeplex.com/wikipage?title=NotifyTaskCompletion) to make this easy:



{% highlight csharp %}public sealed class MyViewModel : INotifyPropertyChanged, IAsyncInitialization
{
    public MyViewModel()
    {
        InitializationNotifier = NotifyTaskCompletion.Create(InitializeAsync());
    }

    public INotifyTaskCompletion InitializationNotifier { get; private set; }
    public Task Initialization { get { return InitializationNotifier.Task; } }

    private async Task InitializeAsync()
    {
        await Task.Delay(100); // asynchronous initialization
    }
}
{% endhighlight %}

Your data-binding code can use paths like `InitializationNotifier.IsCompleted` and `InitializationNotifier.ErrorMessage` to respond to the completion of the initialization task.



### Asynchronous Initialization: Conclusion

I do prefer the asynchronous factory approach over the asynchronous initialization pattern. The asynchronous initialization pattern _does_ expose instances before they are initialized, and depends on the programmer to correctly use `Initialization`. But there are some situations where you can't use asynchronous factory methods, and asynchronous initialization is a decent workaround.



## What NOT To Do

Here's an example of what **not** to do:



{% highlight csharp %}public sealed class MyClass
{
  private MyData asyncData;
  public MyClass()
  {
    InitializeAsync();
  }

  // BAD CODE!!
  private async void InitializeAsync()
  {
    asyncData = await GetDataAsync();
  }
}
{% endhighlight %}

At first glance, this seems like a reasonable approach: you get a regular constructor that kicks off an asynchronous operation; however, there are several drawbacks that are due to the use of `async void`.



The first problem is that when the constructor completes, the instance is still being asynchronously initialized, and there isn't an obvious way to determine when the asynchronous initialization has completed.



The second problem is with error handling: any exceptions raised from `InitializeAsync` will be thrown directly on the `SynchronizationContext` that was current when the instance was constructed. The exception won't get caught by any `catch` clauses surrounding the object construction. Most applications treat this as a fatal error.



The first two solutions in this post (asynchronous factory method and `AsyncLazy<T>`) do not have these problems. They do not provide an instance until it has been asynchronously initialized, and exception handling is more natural. The third solution (asynchronous initialization) does return an instance before it has been initialized (which I don't like), but it mitigates this by providing a standard way to detect when initialization has completed as well as reasonable exception handling.

