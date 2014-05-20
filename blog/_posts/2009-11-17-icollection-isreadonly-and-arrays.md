---
layout: post
title: "ICollection.IsReadOnly (and Arrays)"
---
Today I had a simple question that ended up having a bit of a complex answer: how does one implement ICollection<T>.IsReadOnly?

The fundamental problem is that there's more than one definition of "read-only". Various collection types permit different types of updates. Generally, updates fall into one of two categories:

1. An update that changes the value of an element already in the collection, and does not change the number of elements in the collection. e.g., the index setter.
1. An update that changes the number of values in the collection, but does not change any of the values of the elements in the collection. e.g., Add(), Clear(), etc.

I Googled for the proper semantics to use, and was able to find three decent sources of information: a [StackOverflow question on the Contract of ICollection<T>.IsReadOnly](http://stackoverflow.com/questions/1073522/contract-of-icollectiont-isreadonly), a [blog post by Peter Golde titled "IList, ICollection, and IsReadOnly"](http://www.wintellect.com/CS/blogs/pgolde/archive/2005/05/12/ilist-icollection-and-isreadonly.aspx), and a [blog post by Krzysztof Cwalina on "Generic interfaces, IsReadOnly, IsFixedSize, and array"](http://blogs.msdn.com/kcwalina/archive/2005/05/18/419203.aspx). From the (older) blog posts and some quick tests on array behavior, I've reached the conclusions below regarding the history and current state of the IsReadOnly property.

## The Traditional Interpretation

The value of IsReadOnly is false if _either_ type of update is allowed. It is only set to true if _both_ types of updates are _not_ allowed.

The built-in array type (which only allows one type of update) honors this interpretation by returning false for IsReadOnly:

{% highlight csharp %}
[TestMethod]
public void Array_IsNotReadOnly()
{
    int[] array = new[] { 1, 2, 3, 4 };
    Assert.AreEqual("Int32[]", array.GetType().Name);
    bool arrayIsReadOnly = array.IsReadOnly;
    Assert.IsFalse(arrayIsReadOnly);
 
    System.Collections.IList arrayAsIList = array;
    Assert.AreEqual("Int32[]", arrayAsIList.GetType().Name);
    bool arrayAsIListIsReadOnly = arrayAsIList.IsReadOnly;
    Assert.IsFalse(arrayAsIListIsReadOnly);
}
{% endhighlight %}

## The Modern Interpretation

It appears that with .NET 2.0, the meaning of IsReadOnly has changed. It should now be true if _either_ type of update is _not_ allowed. It is only set to false if _both_ types of updates are allowed.

Interestingly, the built-in array type honors this interpretation as well. It returns true for IsReadOnly (but only if accessed through a generic interface):

{% highlight csharp %}
[TestMethod]
public void Array_IsReadOnly()
{
    int[] array = new[] { 1, 2, 3, 4 };
 
    IList<int> arrayAsIListOfT = array;
    Assert.AreEqual("Int32[]", arrayAsIListOfT.GetType().Name);
    bool arrayAsIListOfTIsReadOnly = arrayAsIListOfT.IsReadOnly;
    Assert.IsTrue(arrayAsIListOfTIsReadOnly);
}
{% endhighlight %}

Presumably, any new list types that implement IList as well as IList<T> may need to return different values for IList.IsReadOnly and IList<T>.IsReadOnly. This is confusing, to say the least.

## Identical Documentation; Confusing Behavior

As of the time of blog post, the Microsoft documentation for IList.IsReadOnly and ICollection<T>.IsReadOnly are nearly identical, ignoring the fact that the semantics are quite different:

 1. [IList.IsReadOnly](http://msdn.microsoft.com/en-us/library/system.collections.ilist.isreadonly.aspx): "A collection that is read-only does not allow the addition, removal, or modification of elements after the collection is created."
 1. [ICollection<T>.IsReadOnly](http://msdn.microsoft.com/en-us/library/0cfatk9t.aspx): "A collection that is read-only does not allow the addition, removal, or modification of elements after the collection is created."

Furthermore, the behavior of the common array class is confusing, especially in light of the current Microsoft documentation for [Array.IsReadOnly](http://msdn.microsoft.com/en-us/library/system.array.isreadonly.aspx): "This property is always false for all arrays."

## Conclusion: Does Anyone Care?

This blog post has been an attempt to sort out the proper way of implementing IsReadOnly. However, due to the complexity of the semantics, it seems unlikely that any client code is actually using it correctly.

For future code, I recommend only implementing IList<T> with the modern interpretation, and not implementing IList. If one does need IList, however (e.g., for binding purposes), then they must implement both interpretations.

