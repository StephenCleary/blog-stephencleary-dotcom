---
layout: post
title: "Unmanaged Blocking"
tags: ["Threading", "P/Invoke", ".NET"]
---


Managed code should never block in an unmanaged function, if it can possibly help it. As a general rule, wait functions (such as **WaitForMultipleObjects**) should never be p/Invoked from managed code.





The MSDN document [Reliability Best Practices](http://msdn.microsoft.com/en-us/library/ms228970.aspx) ([webcite](http://www.webcitation.org/5yHvHIG97)) states "do not block indefinitely in unmanaged code." Specifically, "blocking using a Win32 synchronization primitive is a clear example of something we cannot allow" because "a blocked thread prevents the CLR from unloading the AppDomain, at least without doing some extremely unsafe operations." As a general rule, they suggest that any function blocking more than 10 seconds will _require special CLR support!_ In other words, if you're doing unmanaged blocking for that long, you'll have to write your own .NET runtime host.





The legendary Chris Brumme has a good blog entry on [Managed Blocking](http://blogs.msdn.com/b/cbrumme/archive/2003/04/17/51361.aspx) ([webcite](http://www.webcitation.org/5yHvfrmgy)). He enumerates several reasons why unmanaged blocking is inappropriate:




- The CLR loses control of the thread. This is the same reason covered in the MSDN article above.
- Managed blocking will do message pumping (in the right way) while blocked. This is necessary for STA threads (including UI threads as well as threads doing STA COM interop). Mr. Blumme has another classic classic blog entry: [Apartments and Pumping in the CLR](http://blogs.msdn.com/b/cbrumme/archive/2004/02/02/66219.aspx) ([webcite](http://www.webcitation.org/5yHvxNIih)) that delves in-depth into this issue, and is probably the most complex blog post in existence.
- The CLR collects information about managed threads, including how often and how long they block; this information is used (among other things) for making the ThreadPool more efficient. Unmanaged blocking prevents the CLR from gathering this information.
- (The fourth reason from the blog post - hiding of platform differences - is no longer applicable, since the Windows 9x line is no longer supported by modern .NET applications).




Joe Duffy, in a very interesting post on [Hooking CLR Calls with SynchronizationContext](http://www.bluebytesoftware.com/blog/PermaLink,guid,710e6ba3-60e9-4f5e-a5a7-d878015c7a16.aspx) ([webcite](http://www.webcitation.org/5yHvuG9w1)), talks about using a custom **SynchronizationContext** implementation to receive notifications about managed blocking. These types of notifications simply won't work if a managed thread does unmanaged blocking.





Closely related to Joe Duffy's post above is the fact that some CLR hosts take special action when managed threads block. In particular, SQL Server makes use of that information. Any host that is based on fibers instead of threads would also require that information (AutoCAD is the only such host that I'm aware of). Again, unmanaged blocking would prevent these hosts from working as expected.





[**WaitHandle**](http://msdn.microsoft.com/en-us/library/system.threading.waithandle.aspx) is the key to unusual managed blocking situations. You can wait on any or all of a series of handles, or even derive from the **WaitHandle** class itself if you have a Win32 synchronization primitive not already wrapped by the BCL.





The bottom line is: avoid unmanaged blocking.

