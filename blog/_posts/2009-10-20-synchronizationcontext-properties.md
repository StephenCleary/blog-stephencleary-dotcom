---
layout: post
title: "SynchronizationContext Properties Summary"
---
A few of my posts recently have dealt with surprises that I've found when interacting with different implementations of [SynchronizationContext](http://msdn.microsoft.com/en-us/library/system.threading.synchronizationcontext.aspx). This post is a summary of my findings.

<div class="panel panel-default" markdown="1">
  <div class="panel-heading">SynchronizationContext Implementation Properties</div>

{:.table .table-striped}
||Specific Associated Thread|Synchronized Execution|Sequential Execution|Reentrant Send|Reentrant Post|Supports Equality Comparision|
|-
|Windows Forms|Yes|Yes|Yes|Sometimes [1]|Never|Yes|
|Windows Presentation Foundation and Silverlight|Yes|Yes|Yes|Sometimes [1]|Never|No [3]|
|Nito|Yes|Yes|Yes|Never [2]|Never|No [4]|
|Default|No|No|No|Always|Never|N/A [5]|
|ASP.NET|No|Yes|No|Always|Always [6]|N/A [5]|

</div>

## Notes

1. Send is reentrant when invoked from the same GUI thread; it is not reentrant when invoked from other threads.
1. Invoking Send from the thread associated with a Nito.Async.ActionDispatcherSynchronizationContext is not allowed.
1. As of .NET 3.5 SP1, WPF will create a separate DispatcherSynchronizationContext for each window, even if both windows are on the same thread.
1. It is possible to create two different Nito.Async.ActionDispatcherSynchronizationContext instances that refer to the same underlying Nito.Async.ActionDispatcher.
1. The "Supports Equality Comparision" property is meaningless because this SynchronizationContext type does not have a Specific Associated Thread.
1. The rationale behind this surprising reentrancy is that the AspNetSynchronizationContext is not associated with any threads at all, so it borrows the thread from its caller.

## SynchronizationContext Implementations

The "Windows Forms" entry refers to the [System.Windows.Forms.WindowsFormsSynchronizationContext](http://msdn.microsoft.com/en-us/library/system.windows.forms.windowsformssynchronizationcontext.aspx), which is used by the GUI thread(s) in Windows Forms applications. Other threads in the same application may use different SynchronizationContext implementations.

The "Windows Presentation Foundation and Silverlight" entry refers to the [System.Windows.Threading.DispatcherSynchronizationContext](http://msdn.microsoft.com/en-us/library/system.windows.threading.dispatchersynchronizationcontext.aspx), which is used by the GUI thread(s) in Windows Presentation Foundation and Silverlight applications. Other threads in the same application may use different SynchronizationContext implementations.

The "Nito" entry refers to the Nito.Async.ActionDispatcherSynchronizationContext from the [Nito.Async](http://nitoasync.codeplex.com/) library. This includes Nito.Async.ActionThread threads.

The "Default" entry refers the default implementation of [System.Threading.SynchronizationContext](http://msdn.microsoft.com/en-us/library/system.threading.synchronizationcontext.aspx). This includes ThreadPool and Thread class threads, Windows Services, and Console applications, unless that thread replaces the default with a different SynchronizationContext.

The "ASP.NET" entry refers to the System.Web.dll:System.Web.AspNetSynchronizationContext, which is used by threads running in an application hosted by the ASP.NET runtime.

## SynchronizationContext Properties

The "Specific Associated Thread" property means that the SynchronizationContext refers to a single, specific thread, and that queueing work to the SynchronizationContext will queue work to that thread. Note that multiple SynchronizationContexts may still refer to the same thread, even if this property is true.

The "Synchronized Execution" property means that all work queued to the SynchronizationContext will execute one at a time.

The "Sequential Execution" property means that all work queued to the SynchronizationContext will execute in order. If a SynchronizationContext supports Sequential Execution, then it also supports Synchronized Execution.

The "Reentrant Send" property means that the implementation of SynchronizationContext.Send will directly invoke its delegate on the current thread.

The "Reentrant Post" property means that the implementation of SynchronizationContext.Post will directly invoke its delegate on the current thread.

The "Supports Equality Comparision" property means that instances of that SynchronizationContext type may be compared for equality, and that equality implies that they refer to the same Specific Associated Thread.

## Further Reading and a Useful Library

For more details, see the previous SynchronizationContext-related posts [Gotchas from SynchronizationContext!]({% post_url 2009-08-14-gotchas-from-synchronizationcontext %}) and [Another SynchronizationContext Gotcha: InvokeRequired?]({% post_url 2009-09-22-another-synchronizationcontext-gotcha %})

[The Nito.Async library](http://nitoasync.codeplex.com/) contains a Nito.Async.SynchronizationContextRegister that can be used by a program to query properties of a SynchronizationContext implementation (except the "Supports Equality Comparision" property). This is useful when writing .NET multi-host-compatible asynchronous components.

