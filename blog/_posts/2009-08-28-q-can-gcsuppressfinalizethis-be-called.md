---
layout: post
title: "Q&A: Can GC.SuppressFinalize(this) be called at any time?"
---
Today I was asked by a colleague about some of my blog posts yesterday regarding IDisposable and Finalize. So here's the first post in a series of Q&A regarding finalizers. Enjoy!

### Question: Does GC.SuppressFinalize(this) work if the object is not already on the finalize queue?

### Answer: Yes, it does.

## Additional Information

There's some confusion on what exactly GC.SuppressFinalize does, because the [.NET 1.1 docs](http://www.webcitation.org/5wPLddgo3) state "The method removes _obj_ from the set of objects that require finalization." However, since .NET 2.0, the [docs](http://www.webcitation.org/5wPLgw9IJ) have been updated to read "This method sets a bit in the object header, which the system checks when calling finalizers." This clarifies GC.SuppressFinalize semantics nicely.

Because of the old docs, there is some FUD regarding GC.SuppressFinalize floating around in old forum and newsgroup posts. Some people insist that it must be the last thing done by a Dispose() implementation. However, the truth is that it is safe to call at any time. In fact, [some BCL classes even call GC.SuppressFinalize in their constructors](http://www.webcitation.org/5wPLXTCHO)!

There is an argument that [can be made](http://www.webcitation.org/5wPLnd2En) for calling GC.SuppressFinalize as the last statement in a Dispose method: it ensures that the finalizer is only suppressed if Dispose does not throw. However, it is very poor practice to have a Dispose method that throws, so this argument has little merit.

## Test Code:

{% highlight csharp %}
// Do not run these tests from a Debug build or under the debugger. A standalone release build is required.
using System;
using System.Threading;
using System.Diagnostics;
 
public class Test : IDisposable
{
    public static Test test;
 
    ~Test()
    {
        Console.WriteLine("Finalizer called");
    }
 
    public void RemoveMyFinalizer()
    {
        GC.SuppressFinalize(this);
    }
 
    public void Dispose()
    {
        Console.WriteLine("Dispose called");
    }
 
    public void Report()
    {
        Console.WriteLine("Test object is still alive!");
    }
 
    static void Main()
    {
        // We use a static object here to ensure it is reachable
        Test.test = new Test();
        // Since the test object is reachable, it cannot be in the finalizer queue at this point
        Test.test.RemoveMyFinalizer();
        Test.test.Report();
        Console.WriteLine("Returning from Main");
    }
}
{% endhighlight %}

## Output of Test Code:

    Test object is still alive!
    Returning from Main
