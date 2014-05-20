---
layout: post
title: "SynchronizationContext Odds and Ends"
---
During my many adventures with SynchronizationContext, I ran into two rather interesting corner cases. Originally, these appeared as footnotes in [my article](http://msdn.microsoft.com/en-us/magazine/gg598924.aspx), but they were among the first things that I cut.



Both of these corner cases deal with a "missing" SynchronizationContext; that is, **SynchronizationContext.Current** is **null** when it shouldn't be. In this case, the default SynchronizationContext is used, which invokes all of its callbacks on the ThreadPool thread. One common symptom of this problem is that **BackgroundWorker.RunWorkerCompleted** gets a cross-thread exception.



## Missing SynchronizationContext in Office Add-Ins

I ran into this issue on the [MSDN forums](http://www.webcitation.org/5wdDTMTu4). Apparently, Microsoft Office add-ins do not have a SynchronizationContext installed when they are invoked. This appears to be a simple oversight, and is fixed by calling **SynchronizationContext.SetSynchronizationContext**, passing a **new WindowsFormsSynchronizationContext()**.



The MSDN forums have several other threads dealing with the same issue, phrased several different ways.



## Missing SynchronizationContext in (old versions of) Windows Forms before Show

[Less than a year ago](http://www.webcitation.org/5wdE1qbIg), Windows Forms would only install the WindowsFormsSynchronizationContext when the first Win32 window handle for that thread was created. In particular, **SynchronizationContext.Current** was **null** through the main form's constructor _and_ Load event. It would be set, however, by the time the Show event was invoked. One common workaround was to force the creation of the Win32 window handle (by reading the Handle property), which installed the proper SynchronizationContext as a side-effect.



Fortunately, that hack is no longer necessary. Sometime in the last year, Microsoft released an update that fixes that issue all the way back to .NET 2.0 Windows Forms projects. I'm not sure which update that was.

