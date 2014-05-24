---
layout: post
title: "Sharp Corner: Value Types Are Never Reference-Equal"
---
There are two types of equality testing in the .NET framework: reference equality and value equality (if the type being tested supports value equality). There are numerous ways to test for equality (object.Equals, operator ==, IEqualityComparer\<T>, etc), but at the end every one of them resolves to either value equality or reference equality.

Conceptually, two objects are "reference equal" iff they are actually the same object. For example, two strings may have the same value (and thus be "value equal"), but they may be two different objects (and thus not "reference equal").

Eventually, one hits a corner:

{% highlight csharp %}

[TestMethod]
public void ValueTypes_AreNeverReferenceEqual()
{
    var num = 13;
    
    Assert.IsFalse(object.ReferenceEquals(num, num));
}
{% endhighlight %}

Of course, people rarely wish to test value types for reference equality; this corner is more likely to be found while testing instances of a generic type for reference equality. This result is often surprising; if everything in C# is an object (including a value of type Int32, which derives from ValueType, which derives from object), then why can't they be compared for reference equality?

The reason that this does not work is because **unboxed value types are not objects**. They are a "special case" in the C#/.NET world, given special treatment for efficiency reasons. They are _convertible_ to an object (via a boxing conversion), but they are not actually objects themselves. C# really goes far to _pretend_ that they are objects (e.g., "7.ToString()"), but it can't cover every corner.

In the example above, the value instance is implicitly _converted_ to an object - twice - and then these objects are compared. Naturally, they refer to different objects, so they are not reference-equal.

Boxed value types are real objects (though they lose their compile-time type information). They may be compared for reference equality:

{% highlight csharp %}

[TestMethod]
public void BoxedValueTypes_CanBeReferenceEqual()
{
    var num = (object)13;

    Assert.IsTrue(object.ReferenceEquals(num, num));
}
{% endhighlight %}

Conclusion: contrary to popular opinion, not everything in C# is an object.

