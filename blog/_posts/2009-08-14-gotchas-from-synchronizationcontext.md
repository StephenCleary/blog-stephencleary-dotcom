---
layout: post
title: "Gotchas from SynchronizationContext!"
---
This week, I've been designing a [SynchronizationContext](http://msdn.microsoft.com/en-us/library/system.threading.synchronizationcontext.aspx?WT.mc_id=DT-MVP-5000058) equivalent for the Compact Framework as groundwork for sharing a layer of asynchronous service objects between desktop and mobile applications. I ran into two difficulties with its semantics. I've encountered both of these before, but now that I'm designing something nearly equivalent, I'm trying to fix them before they cause problems for others.

## Gotcha #1: Reentrancy

Did you know that [SynchronizationContext.Send](http://msdn.microsoft.com/en-us/library/system.threading.synchronizationcontext.send.aspx?WT.mc_id=DT-MVP-5000058) can directly invoke the delegate argument? You did? Ah, yes... the default implementation, mscorlib.dll:System.Threading.SynchronizationContext.Send does indeed invoke its delegate argument directly. This is a slightly obscure but not unheard-of fact.

Now for the one that surprised me this week: [SynchronizationContext.Post](http://msdn.microsoft.com/en-us/library/system.threading.synchronizationcontext.post.aspx?WT.mc_id=DT-MVP-5000058) can _also_ directly invoke its delegate argument! The evidence is in System.Web.dll:System.Web.AspNetSynchronizationContext.Post, which invokes its delegate argument directly.

A careful reading of the SynchronizationContext documentation leads me to conclude that both Send and Post may result in reentrant behavior. Previously, I had assumed that Post (at least) would not be reentrant.

If code for an asynchronous component needs to prevent reentrancy from a generic SynchronizationContext, it may easily do so by using the [ThreadPool](http://msdn.microsoft.com/en-us/library/system.threading.threadpool.aspx?WT.mc_id=DT-MVP-5000058):

{% highlight csharp %}
/// <summary>
/// Provides extension methods for <see cref="SynchronizationContext"/>.
/// </summary>
public static class SynchronizationContextExtensions
{
    /// <summary>
    /// Synchronously invokes a delegate by passing it to a <see cref="SynchronizationContext"/>, waiting for it to complete.
    /// </summary>
    /// <param name="synchronizationContext">The <see cref="SynchronizationContext"/> to pass the delegate to. May not be null.</param>
    /// <param name="action">The delegate to invoke. May not be null.</param>
    /// <remarks>
    /// <para>This method is guaranteed to not be reentrant.</para>
    /// </remarks>
    public static void SafeSend(this SynchronizationContext synchronizationContext, Action action)
    {
        // The semantics of SynchronizationContext.Send allow it to invoke the delegate directly, but we can't allow that.
        Action forwardDelegate = () => synchronizationContext.Send((state) => action(), null);
        IAsyncResult result = forwardDelegate.BeginInvoke(null, null);
        result.AsyncWaitHandle.WaitOne();
    }

    /// <summary>
    /// Asynchronously invokes a delegate by passing it to a <see cref="SynchronizationContext"/>, returning immediately.
    /// </summary>
    /// <param name="synchronizationContext">The <see cref="SynchronizationContext"/> to pass the delegate to. May not be null.</param>
    /// <param name="action">The delegate to invoke. May not be null.</param>
    /// <remarks>
    /// <para>This method is guaranteed to not be reentrant.</para>
    /// </remarks>
    public static void SafePost(this SynchronizationContext synchronizationContext, Action action)
    {
        // The semantics of SynchronizationContext.Post allow it to invoke the delegate directly, but we can't allow that.
        ThreadPool.QueueUserWorkItem((state) => synchronizationContext.Post((state2) => action(), null));
    }
}
{% endhighlight %}

This code or something similar may go into the next release of [Nito.Async](https://github.com/StephenClearyArchive/Nito.Asynchronous). However, the code as-is will cause a deadlock when used with synchronization contexts that are sometimes reentrant (e.g., WindowsFormsSynchronizationContext.Send or DispatcherSynchronizationContext.Send if called from the specific thread associated with that synchronization context).

## Gotcha #2: Non-exclusive execution

SynchronizationContext does not guarantee that delegates queued to it will be executed exclusively (one at a time). This is obvious; the default implementation (using the ThreadPool) will simply queue them to the ThreadPool, which will execute them in parallel.

However, this brings up concerns when designing APIs for asynchronous components. In particular, cancellation becomes problematic.

Some SynchronizationContext instances do execute exclusively: the [WindowsFormsSynchronizationContext](http://msdn.microsoft.com/en-us/library/system.windows.forms.windowsformssynchronizationcontext.aspx?WT.mc_id=DT-MVP-5000058), [DispatcherSynchronizationContext](http://msdn.microsoft.com/en-us/library/system.windows.threading.dispatchersynchronizationcontext.aspx?WT.mc_id=DT-MVP-5000058) and [Nito.Async.ActionDispatcherSynchronizationContext](https://github.com/StephenClearyArchive/Nito.Asynchronous) all operate on some type of queue internally, which has a single thread processing requests one at a time.

Most asynchronous components that use the [event-based asynchronous pattern (EBAP)](http://msdn.microsoft.com/en-us/library/wewwczdw.aspx?WT.mc_id=DT-MVP-5000058) - including Nito.Async classes - assume that SynchronizationContext will actually synchronize the delegates with some notion of an "originating thread". However, this is only true for SynchronizationContext instances that execute exclusively. So it works in most cases (Windows Forms, WPF, and explicit ActionDispatcher queues), but would fail in other cases (most notably ASP.NET and in free-threaded/ThreadPool contexts).

There does not appear to be an EBAP solution that is Clean (keeping the EBAP design), Generic (working with any SynchronizationContext), and Safe (preventing event callbacks after cancellation). In fact, this is another manifestation of the ["thread-safe" events problem]({% post_url 2009-06-19-threadsafe-events %}).

One approach is to break the EBAP design by passing callbacks to the _Operation_Async method; a callback delegate can be made cancelable, returning an "ICancelable" to the caller. By enclosing the callback in a cancelable object, we can introduce a lock scope just for that single callback and its canceller. A (recursive) lock could be held during the delegate invokation and requested by ICancelable.Cancel; normally, holding locks during callbacks is A Fast Road To Pain, but with careful implementation it would work in this instance. This would be a Generic and Safe solution (works with any SynchronizationContext and guarantees not to invoke a callback after ICancelable.Cancel returns). Further development down this design path would yield something very similar to the [Task](http://msdn.microsoft.com/en-us/library/system.threading.tasks.task(VS.100).aspx?WT.mc_id=DT-MVP-5000058) class expected to be in .NET 4.0.

A second approach is to break thread safety by using ["thread-safe events"]({% post_url 2009-06-19-threadsafe-events %}), wrong solution #2: the EBAP component can queue a delegate that executes the _Operation_Completed event from the component after copying it into a local variable. Cancelling the notification is possible by clearing the event, but the race condition means that the notification may be invoked after _Operation_Cancel returns. This solution is Clean and Generic (keeps the _Operation_Completed event on the EBAP class and works with any SynchronizationContext). Also, this solution is Safe as long as the SynchronizationContext executes exclusively.

The third approach is to only support SynchronizationContext instances that execute exclusively. This approach is the one currently taken by Nito.Async EBAP components: each delegate is assumed to be synchronized when it is queued to the SynchronizationContext, and uses a [callback context]({% post_url 2009-04-24-asynchronous-callback-contexts %}) to determine if it has been cancelled. This solution is Clean and Safe (keeping the _Operation_Completed event on the EBAP class and guarantees not to invoke it after _Operation_Cancel returns).

## The future

I'm a big fan of quiet cancellation over noisy cancellation. If I call "Cancel", then I don't need an event to tell me that I just called "Cancel". The entire Nito.Async library works on the same (quiet cancellation) principle. However, if noisy cancellation is embraced, then the Safe issue goes away (because noisy cancellation EBAP components cannot be Safe, by definition).

Perhaps - just perhaps - the next version of Nito.Async will change to use noisy cancellation semantics. I'm still exploring alternatives. :)

