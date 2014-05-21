---
layout: post
title: "(Pre)Announcement of Nito.Linq!"
---
I know that it's not actually released yet, but I just couldn't keep quiet any longer!

[Nito.Linq](http://nitolinq.codeplex.com/) is a library that helps "fill in the gaps" in the existing LINQ system. It will be compatible with .NET 3.5 SP1, .NET 4.0, and Silverlight 3 (all with or without [Rx](http://msdn.microsoft.com/en-us/devlabs/ee794896.aspx)).

The primary focus of this library is the development of operators around random-access sequences (IList<T>). Secondary focii include sorted sequence and list operators; a translation of all C++ STL algorithms into sequence/list operators (finally!); compatibility shims for working between .NET 3.5, .NET 4.0, and Rx; and a handful of little "extra" items such as LexicographicalComparer, CircularBuffer, and Deque.

The source code is currently available to play with. These classes are actually being used in production code (at my day job), and most of them have thorough unit tests as well. The implementation is actually quite stable at this point, though the interface may change before the first release.

To whet your appetite, here's a few things that one can do with Nito.Linq:

    int[] a = { 1, 2, 3 };
    int[] b = { 4, 5, 6 };
    IList<int> result = a.Concat(b);

In the code above, _result_ is a list that contains { 1, 2, 3, 4, 5, 6 }. Nothing too surprising there, except that _result_ is actually a concatenated view of the original lists. In other words, _result_ uses delayed execution, just like LINQ.

    int[] a = { 1, 2, 3, 4, 5, 6 };
    IList<int> result = a.Skip(2);

In the code above, _result_ contains the elements you'd expect from using the LINQ Skip operator. However, its type is not IEnumerable<T>; it is IList<T>, which means that it knows how many elements are in it and provides O(1) random-access element retrieval.

    int[] a = { 1, 2, 3 };
    int[] b = { 4, 5, 6 };
    IList<int> result = a.Zip(b, (x, y) => x + y);

In the code above, _result_ contains the elements { 5, 7, 9 }. This is identical to how the Rx Zip operator works, except that Rx's Zip only performs on sequences; _result_ is an IList<T>. Not only does this provide efficient random access to the resulting elements, it also delays execution of the zip delegate until a resulting element is accessed.

    var a = new[] { 1, 2, 3 }.AsSorted();
    int i = a.IndexOf(2);

In the code above, _IndexOf_ is actually implemented using a binary search, rather than a simple linear search.

If you're interested at all, get the source and play around with it (check out the unit tests for simple examples). Let me know what you think!

