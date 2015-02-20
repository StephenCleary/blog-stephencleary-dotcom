---
layout: post
title: "Async OOP 3: Properties"
series: "Async OOP"
seriesTitle: "Properties"
---
Unlike `async` constructors, `async` properties could be added to the language without much difficulty (well, property _getters_ could, at least). Properties are just syntactic sugar for getter and setter methods, and it wouldn't be a huge leap to make these methods `async`. However, `async` properties are not allowed.

This is a purposeful design decision, because "asynchronous properties" is an oxymoron. Property getters should return current values; they should not be kicking off background operations. Also, the semantics behind an "asynchronous setter" are not at all clear.

Usually, when someone wants an "asynchronous property", what they really want is one of these:

- An asynchronous operation that returns a value.
- A value that is expensive to create, but should be cached for future use.
- A value that can be used in data-binding but which must be calculated or retrieved asynchronously.

We'll look at each of these in turn.

## Asynchronous Operations

If your "property" needs to be asynchronously evaluated every time it's accessed, then you're really talking about an asynchronous operation.The best solution is to change the property to an `async` method. Semantically, it shouldn't be a property.

It _is_ possible to have a property return a `Task<T>` just by returning the result of an `async` method:

{% highlight csharp %}
public sealed class MyClass
{
    private async Task<int> GetMyProperty()
    {
        await Task.Delay(100);
        return 13;
    }

    public Task<int> MyProperty
    {
        get { return GetMyProperty(); }
    }
}

...

public static async Task TestAsyncProperty()
{
    var t = new MyClass();
    var result = await t.MyProperty;
}
{% endhighlight %}

However, I do not recommend this approach. If every access to a property is going to kick off a new asynchronous operation, then that "property" should really be a _method_:

{% highlight csharp %}
public sealed class MyClass
{
    public async Task<int> GetMyProperty()
    {
        await Task.Delay(100);
        return 13;
    }
}

...

public static async Task TestAsyncProperty()
{
    var t = new MyClass();
    var result = await t.GetMyProperty();
}
{% endhighlight %}

Personally, I think the asynchronous method makes it clearer that a new asynchronous operation is initiated every time.

## Cached Values

In this case, you only want the asynchronous operation executed once: the first time it's requested. After the operation completes, the result of the operation should be cached and returned immediately.

The easiest solution for this is to use `AsyncLazy<T>`, [available in the AsyncEx library](http://nitoasyncex.codeplex.com/wikipage?title=AsyncLazy):

{% highlight csharp %}
public sealed class MyClass
{
    public MyClass()
    {
        MyProperty = new AsyncLazy<int>(async () =>
        {
            await Task.Delay(100);
            return 13;
        });
    }

    public AsyncLazy<int> MyProperty { get; private set; }
}

...

public static async Task TestAsyncProperty()
{
    var t = new MyClass();
    var result = await t.MyProperty;
}
{% endhighlight %}

In this case, I find the property syntax acceptable, since there's only one actual asynchronous operation and every method waiting on it will wait on the same operation.

## Data-Bound Values

Data binding requires immediate (synchronous) results, and it can only deal with a limited set of types. Data binding will not give awaitable types any special treatment, so the type of an "asynchronous property" used for data binding must be the type of the result of the asynchronous operation (e.g., `int` instead of `Task<int>`).

For this to work, the data-bound value must be initially set to some default or "unknown" value, and the type can implement `INotifyPropertyChanged` to let the data binding know when the asynchronous value has been determined.

{% highlight csharp %}
public sealed class MyClass : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChangedEventHandler handler = PropertyChanged;
        if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
    }

    private int? myProperty;
    public int? MyProperty
    {
        get { return myProperty; }
        private set
        {
            myProperty = value;
            OnPropertyChanged();
        }
    }

    public async Task InitializeAsync()
    {
        await Task.Delay(100);
        MyProperty = 13;
    }
}
{% endhighlight %}

In the example code above, I'm assuming that the code constructing a `MyClass` will call its `InitializeAsync` method. Alternatively, if this instance is contained in an enclosing data bound instance, you could wrap the construction and `InitializeAsync` into an asynchronous factory method [as we discussed last time]({% post_url 2013-01-17-async-oop-2-constructors %}).

If your property value is simply the result of a `Task<TResult>`, then you can use the [NotifyTaskCompletion type in the AsyncEx library](http://nitoasyncex.codeplex.com/wikipage?title=NotifyTaskCompletion) to make this even easier:

{% highlight csharp %}
public sealed class MyClass : INotifyPropertyChanged
{
    public INotifyTaskCompletion<int?> MyProperty { get; private set; }

    public MyClass()
    {
      MyProperty = NotifyTaskCompletion.Create(GetValueAsync());
    }

    private static async Task<int?> GetValueAsync()
    {
        await Task.Delay(100);
        return 13;
    }
}
{% endhighlight %}

In this case, you can databind to `MyProperty.Result`, which will be initialized to the default value (`null`). When the `GetValueAsync` task completes, `MyProperty.Result` will be updated to the result value (`13`). `NotifyTaskCompletion` implements `INotifyPropertyChanged`, so this change will be picked up automatically by the data binding.

<div class="alert alert-info" markdown="1">
<i class="fa fa-hand-o-right fa-2x pull-left"></i>

Update (2014-12-01): For more details, see Recipe 10.4 in my [Concurrency Cookbook]({{ '/book/' | prepend: site.url_www }}){:.alert-link}.
</div>
