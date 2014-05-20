---
layout: post
title: "Q&A: If Dispose calls SuppressFinalize, is KeepAlive required?"
---
### Question: If we include a call to GC.SuppressFinalize(this) in Dispose, is the the call to GC.KeepAlive(this) still required?

### Answer: No.

## Rationale:

- As [has been established]({% post_url 2009-08-28-q-can-gcsuppressfinalizethis-be-called %}), a call to GC.SuppressFinalize anywhere will suppress the finalizer.
- The object must be live at the point GC.SuppressFinalize is called because it must be passed as an argument to that method.
- The call to GC.KeepAlive(this) was only put in Dispose to prevent the finalizer from being invoked while Dispose was still running.
- The GC.KeepAlive(this) call is not necessary because the object will be reachable until its finalizer is suppressed.

## Example requiring GC.SuppressFinalize(this)

This is almost identical to the test code used in [my last Q&A: "Is a call to GC.KeepAlive(this) required in Dispose?"]({% post_url 2009-08-28-q-is-call-to-gckeepalivethis-required %})

{% highlight csharp %}
// Do not run these tests from a Debug build or under the debugger. A standalone release build is required.
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
            Thread.Sleep(500); // Let the Dispose thread run    [1]
        }
 
        // (real code)
        myHandleHolder.Handle = 0;
 
        Console.WriteLine("Thread " + Thread.CurrentThread.ManagedThreadId +
            ": CloseHandle ending");
    }
 
    public void Dispose()
    {
        this.CloseHandle(false);
         
        // Uncomment the next line to fix the handle problems
        //GC.SuppressFinalize(this);    [2]
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
