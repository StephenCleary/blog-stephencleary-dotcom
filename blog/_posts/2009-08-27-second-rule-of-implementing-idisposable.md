---
layout: post
title: "The Second Rule of Implementing IDisposable and Finalizers"
---
This post is part of [How to Implement IDisposable and Finalizers: 3 Easy Rules]({% post_url 2009-08-27-how-to-implement-idisposable-and %}).



## For a class owning managed resources, implement IDisposable (but not a finalizer)

IDisposable only has one method: Dispose. This method has one important guarantee: it must be safe to call multiple times.



An implementation of Dispose may assume that it is not called from a finalizer thread, that its instance is not being garbage collected, and that a constructor for its instance has completed successfully. These assumptions makes it safe to access other managed objects.



One mistake is to place a finalizer on a class that only has managed resources; this example code can result in an exception on the finalizer thread, which would crash the application:



{% highlight csharp %}// This is an example of an incorrect and buggy finalizer.
public sealed class SingleApplicationInstance
{
    private Mutex namedMutex;
    private bool namedMutexCreatedNew;
 
    public SingleApplicationInstance(string applicationName)
    {
        this.namedMutex = new Mutex(false, applicationName, out namedMutexCreatedNew);
    }
 
    public bool AlreadyExisted
    {
        get { return !this.namedMutexCreatedNew; }
    }
 
    ~SingleApplicationInstance()
    {
        // Bad, bad, bad!!!
        this.namedMutex.Close();
    }
}
{% endhighlight %}

Whether or not SingleApplicationInstance implements IDisposable, the fact that it's accessing a managed object in its finalizer is a recipe for disaster.



Here's an exmple of a class that removes the finalizer and then implements IDisposable in a correct but needlessly complex way:



{% highlight csharp %}// This is an example of a needlessly complex IDisposable implementation.
public sealed class SingleApplicationInstance : IDisposable
{
    private Mutex namedMutex;
    private bool namedMutexCreatedNew;
 
    public SingleApplicationInstance(string applicationName)
    {
        this.namedMutex = new Mutex(false, applicationName, out namedMutexCreatedNew);
    }
 
    public bool AlreadyExisted
    {
        get { return !this.namedMutexCreatedNew; }
    }
 
    // Needlessly complex
    public void Dispose()
    {
        if (namedMutex != null)
        {
            namedMutex.Close();
            namedMutex = null;
        }
    }
}
{% endhighlight %}

When a class owns managed resources, it may forward its Dispose call on to them. No other code is necessary. Remember that some classes rename "Dispose" to "Close", so a Dispose implementation may consist entirely of calls to Dispose and Close methods.



An equivalent - and simpler - implementation is here:



{% highlight csharp %}// This is an example of a correct IDisposable implementation.
public sealed class SingleApplicationInstance : IDisposable
{
    private Mutex namedMutex;
    private bool namedMutexCreatedNew;
 
    public SingleApplicationInstance(string applicationName)
    {
        this.namedMutex = new Mutex(false, applicationName, out namedMutexCreatedNew);
    }
 
    public bool AlreadyExisted
    {
        get { return !this.namedMutexCreatedNew; }
    }
 
    public void Dispose()
    {
        namedMutex.Close();
    }
}
{% endhighlight %}

This IDisposable.Dispose implementation is perfectly safe. It can be safely called multiple times, because each of the IDisposable implementations it invokes can be safely called multiple times. This _transitive property_ of IDisposable should be used to write simple Dispose implementations like this one.



This post is part of [How to Implement IDisposable and Finalizers: 3 Easy Rules]({% post_url 2009-08-27-how-to-implement-idisposable-and %}).

