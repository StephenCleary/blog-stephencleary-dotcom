---
layout: post
title: "If PusleEvent is broken, what about Monitor.Pulse?"
---
In a post not too long ago, I mentioned that [PulseEvent was broken]({% post_url 2009-09-16-iasyncresultasyncwaithandle-and %}). That got me to thinking: [Monitor](http://msdn.microsoft.com/en-us/library/system.threading.monitor.aspx) has Pulse/PulseAll methods; are they broken, too?



Many years ago, when [Cygwin](http://www.cygwin.com/) was young, I recall reading several articles and mailing list discussions about the difficulty of implementing a monitor (a.k.a. condition variable) on Windows platforms. It turns out that the built-in manual-reset and auto-reset events are very different than a monitor, though they appear similar at first glance. Many wrong monitor implementations have been written around a simple mutex/event pairing (sometimes with a second event), using PulseEvent.



Unfortunately, a true monitor implementation cannot be implemented simply on Windows. It is actually necessary to manage the wait queues manually in order to implement it correctly.



Fortunately, Microsoft did implement their monitor correctly. This is good, because it means I won't have to do it. ;)



I did Google around for details of Microsoft's monitor implementation, but wasn't able to find anything specifically addressing the PulseEvent problem. So, I downloaded [Rotor](http://www.microsoft.com/downloads/details.aspx?FamilyId=8C09FD61-3F26-4555-AE17-3121B4F51D4D) and verified it myself. I did not verify the entire Monitor implementation - that would take a lot of time that I don't have at the moment. However, I did verify that it is not based on PulseEvent and manintains its own wait queues.

