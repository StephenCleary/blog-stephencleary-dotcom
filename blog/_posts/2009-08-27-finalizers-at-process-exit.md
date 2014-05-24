---
layout: post
title: "Finalizers at Process Exit"
series: "IDisposable and Finalizers"
seriesTitle: "Finalizers During Shutdown"
---
I spent too long investigating a problem in a colleague's code today; the bug was something I knew about but had forgotten:

<div class="alert alert-danger" markdown="1">
<i class="fa fa-exclamation-triangle fa-2x pull-left"></i>

During process shutdown, finalizers are given a strict timeout. If they overrun their timeout, the process is terminated.
</div>

This can easily happen if, say, someone you work with writes a program that leaves cleaning up database objects to the finalizers. The program works fine with their small test databases, but then seems to have no effect when used with larger databases (database engines tend to get excited and roll back operations when their client programs suddenly terminate).

I can't believe that I spent over an hour today tracking this down (sigh). So, I researched it out in some depth, and collected wisdom from several others below.

## Supporting Statements

<blockquote>"Finalization during process termination will eventually timeout." <footer>Chris Brumme, <a href="http://blogs.msdn.com/cbrumme/archive/2004/02/20/77460.aspx">Finalization</a></footer></blockquote>

<blockquote>"We run most of the above shutdown under the protection of a watchdog thread.  By this I mean that the shutdown thread signals the finalizer thread to perform most of the above steps.  Then the shutdown thread enters a wait with a timeout.  If the timeout triggers before the finalizer thread has completed the next stage of the managed shutdown, the shutdown thread wakes up and skips the rest of the managed part of the shutdown." <footer>Chris Brumme, <a href="http://blogs.msdn.com/cbrumme/archive/2003/08/20/51504.aspx">Startup, Shutdown and related matters</a></footer></blockquote>

<blockquote>"[When a process is gracefully terminating], each Finalize method is given approximately 2 seconds to return. If a Finalize method doesn't return within 2 seconds, the CLR just kills the process - no more Finalize methods are called. Also, if it takes more than 40 seconds to call all objects' Finalize methods, then again, the CLR just kills the process. Note: These timeout values were correct at the time I wrote this text, but Microsoft might change them in the future." <footer>Jeffrey Richter, <a href="http://www.amazon.com/gp/product/0735614229?ie=UTF8&tag=stepheclearys-20&linkCode=as2&camp=1789&creative=390957&creativeASIN=0735614229">Applied Microsoft .NET Framework Programming</a>, pg 467; and <a href="http://www.amazon.com/gp/product/0735621632?ie=UTF8&tag=stepheclearys-20&linkCode=as2&camp=1789&creative=390957&creativeASIN=0735621632">CLR via C#, 2nd ed</a>, pg 478</footer></blockquote>

