---
layout: post
title: "Generic Generics and Method Overloads"
---
I was happily coding along this week, adding more `IList<T>` extension methods to my general utility library, when I came across an annoying problem. The following code works fine:

{% highlight csharp %}
int test1<T>(IList<T> x) { return 0; }
int test1<T>(IEnumerable<T> x) { return 1; }
    
[TestMethod]
public void TestMethod1()
{
    var list = new[] { 13 };
    IEnumerable<int> seq = list;
    
    Assert.AreEqual(0, test1(list));
    Assert.AreEqual(1, test1(seq));
}
{% endhighlight %}

The behavior is just as you'd expect; the correct overloaded method is chosen based on the [better conversion](http://msdn.microsoft.com/en-us/library/aa691339(VS.71).aspx?WT.mc_id=DT-MVP-5000058) of the static types of the arguments.

So far, so good. The problem that I came across is when generic generics are used:

{% highlight csharp %}
int test2<T>(IList<IList<T>> x) { return 0; }
int test2<T>(IEnumerable<IEnumerable<T>> x) { return 1; }
    
[TestMethod]
public void TestMethod2()
{
    var list = new[] { 13 };
    IList<IList<int>> list2 = new[] { list };
    var list3 = new[] { list };
    
    Assert.AreEqual(0, test2(list2));
    // The following line does not compile:
    //  "The call is ambiguous between the following methods or properties..."
    //Assert.AreEqual(0, test2(list3));
}
{% endhighlight %}

The compiler can choose the correct overload when the argument matches the specific expected type (e.g., `list2`), but fails to deduce that one overload is better than another when the argument is not as specific (e.g., `list3`).

The reasoning behind this is a bit obscure, but understandable. The compiler determines that it is able to convert the argument to either type:

{% highlight csharp %}
// These implicit conversions are why both methods are considered.
IList<IList<int>> tmp1 = list3;
IEnumerable<IEnumerable<int>> tmp2 = list3;
{% endhighlight %}

However, when determining which overload is "better", the compiler _cannot_ convert from `IList<IList<int>>` to `IEnumerable<IEnumerable<int>>`, so it decides that neither overload is better, and therefore they are ambiguous. The first example worked because there _is_ a conversion from `IList<T>` to `IEnumerable<T>`, so the `IList<T>` overload was chosen.

{% highlight csharp %}
// The lack of this implicit conversion is why the methods are ambiguous.
//tmp2 = tmp1;
{% endhighlight %}

Note also that this situation _may_ change when .NET 4 comes out. .NET 4 introduces covariance and contravariance for generics. The concepts don't apply to APIs that are both readable and writeable (e.g., `IList<T>`), but they do apply to APIs that are one or the other (e.g., `IEnumerable<T>`). It's expected that .NET 4 will have an implicit conversion from `IList<IList<int>>` to `IEnumerable<IEnumerable<int>>` (because `IList<IList<int>>` implements `IEnumerable<IList<int>>`), but it's unclear exactly how "smart" the compiler will be while resolving overload resolution.

[We live in interesting times.](http://en.wikipedia.org/wiki/May_you_live_in_interesting_times)

