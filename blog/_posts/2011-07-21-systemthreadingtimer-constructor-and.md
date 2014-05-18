---
layout: post
title: "System.Threading.Timer Constructor and Garbage Collection"
tags: [".NET", "callbacks"]
---


This week, we take a break from the option parsing posts to bring you an interesting corner case from the BCL.





The [System.Threading.Timer constructor](http://msdn.microsoft.com/en-us/library/1k93acx8.aspx) has several overloads; all except one take a _state_ parameter which is passed to the _TimerCallback_ delegate when the timer fires.





It turns out that this _state_ parameter (and the _TimerCallback_ delegate) have an interesting effect on garbage collection: if neither of them reference the System.Threading.Timer object, it may be garbage collected, causing it to stop. This is because both the _TimerCallback_ delegate and the _state_ parameter are wrapped into a **GCHandle**. If neither of them reference the timer object, it may be eligible for GC, freeing the **GCHandle** from its finalizer.





The single-parameter constructor does not suffer from this problem, because it passes **this** for the _state_ (not **null**). Most real-world usage of System.Threading.Timer either references the timer from the callback or uses the timer for the _state_, so this interesting garbage collection behavior will probably not be noticed.





This blog post was prompted by [my own question on Stack Overflow](http://stackoverflow.com/questions/4962172/why-does-a-system-timers-timer-survive-gc-but-not-system-threading-timer).

