---
layout: post
title: "The Third Rule of Implementing IDisposable and Finalizers"
series: "IDisposable and Finalizers"
seriesOrder: 4
seriesTitle: "Rule 3: Unmanaged Resource"
---
## For a class owning a single unmanaged resource, implement IDisposable and a finalizer

A class that owns a single unmanaged resource should not be responsible for anything else. It should _only_ be responsible for _closing_ that resource.

No classes should be responsible for multiple unmanaged resources. It's hard enough to properly free a single resource; writing a class that handles multiple unmanaged resources is much more difficult.

No classes should be responsible for both managed and unmanaged resources. It's _possible_ to write a class that handles both unmanaged and managed resource, but this is extremely difficult to code correctly. Trust me; don't go there. Even if the class is bug-free, it's a maintenance nightmare. Microsoft re-wrote a _lot_ of core classes in the BCL when .NET 2.0 came out, specifically so they could divide the classes with unmanaged resources from the classes with managed resources.

<div class="alert alert-info" markdown="1">
<i class="fa fa-hand-o-right fa-2x pull-left"></i>

Note: a lot of the really overly-complex Microsoft IDisposable documentation is because they assume your class will want to handle both managed and unmanaged resources. This is a holdover from .NET 1.0, and it's kept only for backwards compatibility. Take a clue from Microsoft: their own classes don't even follow that old pattern (they were changed in .NET 2.0 to follow the pattern described in this blog post). FxCop will yell at you because you need to [implement IDisposable "correctly"](http://msdn.microsoft.com/en-us/library/ms244737.aspx){:.alert-link} (i.e., using the old pattern); ignore it - FxCop is wrong.
</div>

The class should look something like this:

{% highlight csharp %}
// This is an example of a correct IDisposable implementation.
// It is not ideal, however, because it does not inherit from SafeHandle
public sealed class WindowStationHandle : IDisposable
{
    public WindowStationHandle(IntPtr handle)
    {
        this.Handle = handle;
    }
 
    public WindowStationHandle()
        : this(IntPtr.Zero)
    {
    }
 
    public bool IsInvalid
    {
        get { return (this.Handle == IntPtr.Zero); }
    }
 
    public IntPtr Handle { get; set; }
 
    private void CloseHandle()
    {
        // Do nothing if the handle is invalid
        if (this.IsInvalid)
        {
            return;
        }
 
        // Close the handle, logging but otherwise ignoring errors
        if (!NativeMethods.CloseWindowStation(this.Handle))
        {
            Trace.WriteLine("CloseWindowStation: " + new Win32Exception().Message);
        }
 
        // Set the handle to an invalid value
        this.Handle = IntPtr.Zero;
    }
 
    public void Dispose()
    {
        this.CloseHandle();
        GC.SuppressFinalize(this);
    }
 
    ~WindowStationHandle()
    {
        this.CloseHandle();
    }
}
 
internal static partial class NativeMethods
{
    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool CloseWindowStation(IntPtr hWinSta);
}
{% endhighlight %}

IDisposable.Dispose ends with a call to [GC.SuppressFinalize(this)](http://msdn.microsoft.com/en-us/library/system.gc.suppressfinalize.aspx). This ensures that the object will remain live until after its finalizer has been suppressed.

If Dispose is not explicitly invoked, then the finalizer will eventually be invoked, which calls CloseHandle directly.

CloseHandle first checks if the handle is invalid. Then it closes the handle, being careful _not_ to throw exceptions; CloseHandle may be called from the finalizer, and an exception at that point would crash the process. CloseHandle finishes by marking the handle as invalid, making it safe to invoke this method multiple times; this, in turn, makes it safe to invoke Dispose multiple times. It is possible to move this "validity check" into the Dispose method, but placing it in CloseHandle also allows invalid handles to be passed into the constructor or set in the Handle property.

The only reason that SuppressFinalize is called _after_ CloseHandle is because this allows the finalizer to run if the Dispose's CloseHandle fails (by throwing an exception). This is [discussed in detail on Joe Duffy's blog](http://www.bluebytesoftware.com/blog/2005/04/08/DGUpdateDisposeFinalizationAndResourceManagement.aspx), but is a relatively weak argument; the only way this would really make a difference is if the CloseHandle method closed the handle differently when invoked by the finalizer. While it is possible to write code like this, it is certainly not recommended.

**Important!** The WindowStationHandle class does _not_ obtain a window station handle; it knows nothing of creating or opening window stations. That responsibility (along with all the other window station-related methods) belongs in another class (presumably named "WindowStation"). This helps create a correct implementation, because every finalizer must be able to execute without error on a partially-constructed object if the constructor throws; in practice, this is very difficult, and this is another reason why a wrapper class should be split into a "handle closer" class and a "proper wrapper" class.

Note: this is the simplest possible solution, and it does have some very obscure resource leaks (e.g., if a thread is aborted immediately after returning from a resource allocation function). If you're developing on the full framework, and are wrapping an IntPtr handle (such as the window station example above), then it is better to derive from [SafeHandle](http://msdn.microsoft.com/en-us/library/system.runtime.interopservices.safehandle.aspx). If you need to go a step further and support reliable resource deallocation, things get [very complex very quickly!](http://www.codeproject.com/KB/dotnet/idisposable.aspx)
