---
layout: post
title: "Async OOP 1: Inheritance and Interfaces"
series: "Async OOP"
seriesTitle: "Inheritance and Interfaces"
---
Before we dive all the way into "asynchronous OOP", let's address one fairly common question: how does one deal with inheritance of asynchronous methods? What about an "asynchronous interface"?

Fortunately, `async` does work well with inheritance (and interfaces). Remember that `async` is an implementation detail, so interfaces can't be defined with `async`. To define an asynchronous method in an interface, you just need to define a method with the same signature, minus the `async` keyword:

{% highlight csharp %}
interface IMyInterface
{
  Task MyMethodAsync();
}
{% endhighlight %}

You can then implement it using `async`:

{% highlight csharp %}
sealed class MyClass : IMyInterface
{
  public async Task MyMethodAsync()
  {
    ...
  }
}
{% endhighlight %}

If you have an implementation that _isn't_ `async`, you can use `TaskCompletionSource<T>` or one of its shorthand forms such as `Task.FromResult` to implement the asynchronous method signature synchronously.

Similarly, if you have a base class method that returns `Task` or `Task<T>` (which may be asynchronous or synchronous), you can override it with an asynchronous or synchronous method.

Next time, we'll take a look at asynchronous constructors.

