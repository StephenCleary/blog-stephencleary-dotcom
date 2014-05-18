---
layout: post
title: "Q&A: Is a call to GC.KeepAlive(this) required in Dispose?"
tags: [".NET", "IDisposable/Finalizers"]
---
### Question: Is a call to GC.KeepAlive(this) required in Dispose?

### Answer: Only in pathological cases.

## Rationale:


- Dispose() must be safe to call multiple times.
- To prevent multiple disposal of unmanaged resources, there must exist some kind of object-level flag (e.g., "bool disposed") or state (e.g., an invalid handle value) to detect if the object has been disposed.
- This flag must be set by Dispose (or a method invoked by Dispose) _after_ it is checked.
- If the flag is set after disposing the unmanaged resource, then it acts as an equivalent to GC.KeepAlive(this).
- This flag must be checked by the finalizer (or a method invoked by the finalizer).
- If the flag is set before disposing the unmanaged resource, it is still set after it has been checked.
- The [concurrency rules for the Microsoft CLR](http://www.bluebytesoftware.com/blog/2007/11/10/CLR20MemoryModel.aspx) guarantee that this is safe for any reasonable type of flag (bool, IntPtr, or reference type).
- Therefore, GC.KeepAlive(this) is only required if the flag is of a very unusual type (such as a Double) OR if the flag is inside _another_ object.




This reasoning concludes that for 99% of handle objects, a call to GC.KeepAlive(this) is not required. Furthermore, for 99% of the remaining objects, [a call to GC.SuppressFinalize should be used instead of a call to GC.KeepAlive](http://blog.stephencleary.com/2009/08/q-if-dispose-calls-suppressfinalize-is.html).



## Example 1 not requiring GC.KeepAlive(this)



This example uses a "bool disposed" flag, set _before_ disposing the unmanaged resource:



{% highlight csharp %}// Do not run these tests from a Debug build or under the debugger. A standalone release build is required.
private bool disposed;
 
~Test()
{
    if (!this.disposed)
    {
        this.CloseHandle();
    }
}
 
public void Dispose()
{
    if (!this.disposed)
    {
        // At this point, if a GC occurs, the object is still reachable
 
        this.disposed = true;
 
        // This is the soonest point that a GC can occur calling this object's finalizer
        //  and this.disposed has already been set to true.
 
        this.CloseHandle();
    }
}
{% endhighlight %}

## Example 2 not requiring GC.KeepAlive(this)



This example uses a "bool disposed" flag, set _after_ disposing the unmanaged resource:



{% highlight csharp %}// Do not run these tests from a Debug build or under the debugger. A standalone release build is required.
private bool disposed;
 
~Test()
{
    if (!this.disposed)
    {
        this.CloseHandle();
    }
}
 
public void Dispose()
{
    if (!this.disposed)
    {
        this.CloseHandle();
 
        // At this point, if a GC occurs, the object is still reachable
 
        this.disposed = true;
 
        // This is the soonest point that a GC can occur calling this object's finalizer
        //  and this.disposed has already been set to true.
    }
}
{% endhighlight %}

## Example 3 not requiring GC.KeepAlive(this)



This example uses an "invalid handle" flag:



{% highlight csharp %}// Do not run these tests from a Debug build or under the debugger. A standalone release build is required.
private IntPtr handle;
 
~Test()
{
    if (this.handle != IntPtr.Zero)
    {
        this.CloseHandle();
    }
}
 
public void Dispose()
{
    if (this.handle != IntPtr.Zero)
    {
        this.CloseHandle();
 
        // At this point, if a GC occurs, the object is still reachable
 
        this.handle = IntPtr.Zero;
 
        // This is the soonest point that a GC can occur calling this object's finalizer
        //  and this.handle has already been set to IntPtr.Zero.
    }
}
{% endhighlight %}

## Example 4 - requiring GC.KeepAlive(this)



It is possible to create a more pathological case where GC.KeepAlive(this) is required; the code below requires GC.KeepAlive because it holds its actual handle value inside of another class:



{% highlight csharp %}// Do not run these tests from a Debug build or under the debugger. A standalone release build is required.
using System;
using System.Collections.Generic;
using System.Threading;
 
public class Test : IDisposable
{
    private sealed class HandleHolder
    {
        // 0 is the invalid handle value
        public int Handle { get; set; }
    }
 
    private HandleHolder handleHolder;
 
    Test()
    {
        // Set the handle to a valid value for the test
        this.handleHolder = new HandleHolder { Handle = 0x1 };
    }
 
    ~Test()
    {
        Console.WriteLine("Thread " + Thread.CurrentThread.ManagedThreadId +
            ": Finalizer called");
        // This is just a check to ensure the constructor completed
        if (this.handleHolder != null)
        {
            this.CloseHandle(true);
        }
    }
 
    // This method is pretending to be a p/Invoke function to free a handle
    static Dictionary<int, int> freedHandles = new Dictionary<int, int>();
    private static void ReleaseHandle(int handle)
    {
        Console.WriteLine("Thread " + Thread.CurrentThread.ManagedThreadId +
            ": Released handle 0x" + handle.ToString("X"));
 
        if (handle == 0)
        {
            Console.WriteLine("  ReleaseHandle released a bad handle! Bad, bad, bad!");
        }
        else
        {
            lock (freedHandles)
            {
                if (freedHandles.ContainsKey(handle))
                {
                    Console.WriteLine("  ReleaseHandle double-released a handle! Bad, bad, bad!");
                }
                else
                {
                    freedHandles.Add(handle, handle);
                }
            }
        }
    }
 
    private void CloseHandle(bool calledFromFinalizer)
    {
        Console.WriteLine("Thread " + Thread.CurrentThread.ManagedThreadId +
            ": CloseHandle starting");
 
        // (real code)
        HandleHolder myHandleHolder = this.handleHolder;
        if (myHandleHolder.Handle == 0)
        {
            // Handle is already free'd
            return;
        }
 
        // (code inserted to duplicate problems)
        if (!calledFromFinalizer)
        {
            Console.WriteLine("Thread " + Thread.CurrentThread.ManagedThreadId +
                ": Garbage collection in CloseHandle!");
            GC.Collect();
            Thread.Sleep(500); // Let the finalizer thread run
            Console.WriteLine("Thread " + Thread.CurrentThread.ManagedThreadId +
                ": CloseHandle continuing after garbage collection");
        }
 
        // (real code)
        ReleaseHandle(myHandleHolder.Handle);
 
        // (code inserted to duplicate problems)
        if (calledFromFinalizer)
        {
            // With this Thread.Sleep call, you get a double handle release
            // Without this Thread.Sleep call, you get a bad handle released
            Thread.Sleep(500); // Let the Dispose thread run     [1]
        }
 
        // (real code)
        myHandleHolder.Handle = 0;
 
        // If you uncomment the next line, then you won't get handle release errors
        //  regardless of the Thread.Sleep above.
        //GC.KeepAlive(this);     [2]
 
        Console.WriteLine("Thread " + Thread.CurrentThread.ManagedThreadId +
            ": CloseHandle ending");
    }
 
    public void Dispose()
    {
        this.CloseHandle(false);
    }
 
    static void Main()
    {
        Test t = new Test();
        t.Dispose();
        Console.WriteLine("Thread " + Thread.CurrentThread.ManagedThreadId +
            ": Returning from Main");
    }
}
{% endhighlight %}

## Output with [2] commented out (as written above):


Thread 1: CloseHandle starting
Thread 1: Garbage collection in CloseHandle!
Thread 2: Finalizer called
Thread 2: CloseHandle starting
Thread 2: Released handle 0x1
Thread 1: CloseHandle continuing after garbage collection
Thread 1: Released handle 0x1
  ReleaseHandle double-released a handle! Bad, bad, bad!
Thread 1: CloseHandle ending
Thread 1: Returning from Main
Thread 2: CloseHandle ending


## Output with [1] and [2] commented out:


Thread 1: CloseHandle starting
Thread 1: Garbage collection in CloseHandle!
Thread 2: Finalizer called
Thread 2: CloseHandle starting
Thread 2: Released handle 0x1
Thread 2: CloseHandle ending
Thread 1: CloseHandle continuing after garbage collection
Thread 1: Released handle 0x0
  ReleaseHandle released a bad handle! Bad, bad, bad!
Thread 1: CloseHandle ending
Thread 1: Returning from Main


## Output with neither line commented out, OR with just [1] commented out:


Thread 1: CloseHandle starting
Thread 1: Garbage collection in CloseHandle!
Thread 1: CloseHandle continuing after garbage collection
Thread 1: Released handle 0x1
Thread 1: CloseHandle ending
Thread 1: Returning from Main
Thread 2: Finalizer called
Thread 2: CloseHandle starting


## Closing Notes



It is cleaner and more efficient to [include a call to GC.SuppressFinalize(this) instead of a call to GC.KeepAlive(this)](http://blog.stephencleary.com/2009/08/q-if-dispose-calls-suppressfinalize-is.html). The only true reason a call to GC.KeepAlive should be required is if the disposed flag is of an unusual type (like Double).

