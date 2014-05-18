---
layout: post
title: "The First Rule of Implementing IDisposable and Finalizers"
tags: [".NET", "IDisposable/Finalizers"]
---


This post is part of [How to Implement IDisposable and Finalizers: 3 Easy Rules](http://blog.stephencleary.com/2009/08/how-to-implement-idisposable-and.html).



## Don't do it (unless you need to).



IDisposable is not a destructor. Remember that .NET has a garbage collector that works just fine without requiring you to set member variables to null.





There are only two situations when IDisposable does need to be implemented; apply these tests to a class to determine if IDisposable is needed:



- The class owns unmanaged resources.
- The class owns managed (IDisposable) resources.




Note that only classes that _own_ resources should free them. In particular, a class may have a reference to a shared resource; in this case, it should not free the resource because other classes may still be using it.





Here's a code example similar to what many beginner C# programmers write:



{% highlight csharp %}// This is an example of an incorrect IDisposable implementation.
public sealed class ErrorList : IDisposable
{
    private string category;
    private List<string> errors;

    public ErrorList(string category)
    {
        this.category = category;
        this.errors = new List<string>();
    }

    // (other methods go here to add/display error messages)

    // Completely unnecessary...
    public void Dispose()
    {
        if (this.errors != null)
        {
            this.errors.Clear();
            this.errors = null;
        }
    }
}
{% endhighlight %}



Some programmers (especially with C++ backgrounds) even go a step further and add a finalizer:



{% highlight csharp %}// This is an example of an incorrect and buggy IDisposable implementation.
public sealed class ErrorList : IDisposable
{
    private string category;
    private List<string> errors;

    public ErrorList(string category)
    {
        this.category = category;
        this.errors = new List<string>();
    }

    // (other methods go here to add/display error messages)

    // Completely unnecessary...
    public void Dispose()
    {
        if (this.errors != null)
        {
            this.errors.Clear();
            this.errors = null;
        }
    }

    ~ErrorList()
    {
        // Very bad!
        // This can cause an exception in the finalizer thread, crashing the application!
        this.Dispose();
    }
}
{% endhighlight %}



The correct implementation of IDisposable for this type is here:



{% highlight csharp %}// This is an example of a correct IDisposable implementation.
public sealed class ErrorList
{
    private string category;
    private List<string> errors;

    public ErrorList(string category)
    {
        this.category = category;
        this.errors = new List<string>();
    }
}
{% endhighlight %}



That's right, folks. The correct IDisposable implementation for this class is to _not_ implement IDisposable! When an ErrorList instance becomes unreachable, the garbage collector will automatically reclaim all of its memory and resources.





Remember the two tests to determine if IDisposable is needed (owning unmanaged resources and owning managed resources). A simple checklist can be done as follows:



 1. Does the ErrorList class own unmanaged resources? No, it does not.
 1. Does the ErrorList class own managed resources? Remember, "managed resources" are any classes implementing IDisposable. So, check each owned member type:

  1. Does string implement IDisposable? No, it does not.
  1. Does List<string> implement IDisposable? No, it does not.
  1. Since none of the owned members implement IDisposable, the ErrorList class does not own any managed resources.

  1. Since there are no unmanaged resources and no managed resources owned by ErrorList, it does not need to implement IDisposable.




This post is part of [How to Implement IDisposable and Finalizers: 3 Easy Rules](http://blog.stephencleary.com/2009/08/how-to-implement-idisposable-and.html).

