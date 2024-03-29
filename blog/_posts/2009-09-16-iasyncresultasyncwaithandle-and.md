---
layout: post
title: "IAsyncResult.AsyncWaitHandle and ThreadPool.RegisterWaitForSingleObject"
---
The other day, I was working on a project that had several layers of abstraction, each one exposing a purely asynchronous API (using [IAsyncResult](https://docs.microsoft.com/en-us/dotnet/api/system.iasyncresult?WT.mc_id=DT-MVP-5000058)) to the next higher layer. At one point, I wanted to add an "additional handler" to an [IAsyncResult.AsyncWaitHandle](https://docs.microsoft.com/en-us/dotnet/api/system.iasyncresult.asyncwaithandle?WT.mc_id=DT-MVP-5000058) from a lower layer. The regular handler would take care of getting the results (calling _End*_), and the additional handler would perform an additional action when the operation completed.

Well, that's what [ThreadPool.RegisterWaitForSingleObject](https://docs.microsoft.com/en-us/dotnet/api/system.threading.threadpool.registerwaitforsingleobject?WT.mc_id=DT-MVP-5000058) is for, right? I coded it up, but there was a little nagging feeling in the back of my mind...

The catch is that IAsyncResult-based APIs use _Begin*_ methods to construct IAsyncResult objects, and _End*_ methods are used in place of IDisposable. So, when the asynchronous operation completes, the normal handler passes it to _End*_, which may include the semantics of IDisposable for that IAsyncResult object.

The problem: What happens if the IAsyncResult object is disposed while the thread pool is still waiting on its AsyncWaitHandle? More generally, could the handle be set to an unsignalled state, causing the thread pool to wait forever?

The handle in question was to a [ManualResetEvent](https://docs.microsoft.com/en-us/dotnet/api/system.threading.manualresetevent?WT.mc_id=DT-MVP-5000058), which is a common implementation of IAsyncResult.AsyncWaitHandle (but not the only possible implementation). In my particular case, though, I thought that multiple threads waiting on a single ManualResetEvent should all get triggered if the event is set. Still, it seemed like this wasn't quite correct.

Indeed, it turns out to be wrong in the general case. [Concurrent Programming on Windows (pg 231)](http://tinyurl.com/ConcurrentProgrammingOnWindows) clarified the issue nicely: when a thread is waiting on a handle, it may be interrupted by a kernel-mode asynchronous procedure call. [Many programmers are surprised that device drivers may "steal" their threads, but it happens all the time; most device drivers do not have a thread of their own, and just borrow whatever user-mode thread happens to be running when they need to do some processing]. This implies, among other things:

- [PulseEvent](https://docs.microsoft.com/en-us/windows/win32/api/winbase/nf-winbase-pulseevent?WT.mc_id=DT-MVP-5000058) is [completely broken](https://devblogs.microsoft.com/oldnewthing/20050105-00/?p=36803&WT.mc_id=DT-MVP-5000058).
- Multiple threads waiting on a manual reset event may cause a missed signal (resulting in an infinite wait) when one of the waiters resets or closes the event.

The situation where multiple threads are waiting on a manual reset event and one of the waiters _resets_ the event is fundamentally broken. It suffers from the same problem as PulseEvent.

On the other hand, it turns out that ThreadPool.RegisterWaitForSingleObject does actually have a workaround for the situation where multiple threads are waiting on a manual reset event and one of the waiters _closes_ the event. Internally, it increments the reference count for the [WaitHandle](https://docs.microsoft.com/en-us/dotnet/api/system.threading.waithandle?WT.mc_id=DT-MVP-5000058)'s [SafeWaitHandle](https://docs.microsoft.com/en-us/dotnet/api/microsoft.win32.safehandles.safewaithandle?WT.mc_id=DT-MVP-5000058). Then, the decrementing of the reference count is treated as an unmanaged resource wrapped in the [RegisteredWaitHandle](https://docs.microsoft.com/en-us/dotnet/api/system.threading.registeredwaithandle?WT.mc_id=DT-MVP-5000058) object.

This is why we do need to try to Dispose all RegisteredWaitHandle objects: to decrement the reference count of the waitable handles passed to ThreadPool.RegisterWaitForSingleObject. I had always wondered why that was recommended "even if you specify true for executeOnlyOnce".

Note: there is one other caveat for passing IAsyncResult.AsyncWaitHandle to ThreadPool.RegisterWaitForSingleObject: the same handle value can't be passed twice to that method. My particular situation never exposed the IAsyncResult to the end-user, but if it did, I'd have to [DuplicateHandle](https://docs.microsoft.com/en-us/windows/win32/api/handleapi/nf-handleapi-duplicatehandle?WT.mc_id=DT-MVP-5000058) the waitable handle first.

It's interesting to consider that once you bring DuplicateHandle into the mix, you actually end up with reference counted reference counts...

