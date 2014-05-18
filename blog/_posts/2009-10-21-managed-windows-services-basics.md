---
layout: post
title: "Managed Windows Services - The Basics"
tags: ["Threading", ".NET", "Windows Services"]
---


Managed (.NET) Windows Services suffer from a lack of sufficient information in the .NET MSDN documentation. Earlier this year, the BCL team put a post on their blog that fills in the gaps: [How .NET Managed Services Interact with the Service Control Manager](http://blogs.msdn.com/bclteam/archive/2009/02/19/in-depth-how-net-managed-services-interact-with-the-servicecontrolmanager-scm-kim-hamilton.aspx). The Service Control Manager (SCM) is the part of Windows that controls starting and stopping Windows Services.



## Services and the .NET ServiceBase Class



In a nutshell, the static [ServiceBase.Run](http://msdn.microsoft.com/en-us/library/system.serviceprocess.servicebase.run.aspx) method provides a main loop for services, giving the service's main thread to the SCM. Once control has been passed off, [ServiceBase](http://msdn.microsoft.com/en-us/library/system.serviceprocess.servicebase.aspx) will invoke the service entry points such as [ServiceBase.OnStart](http://msdn.microsoft.com/en-us/library/system.serviceprocess.servicebase.onstart.aspx) and [ServiceBase.OnStop](http://msdn.microsoft.com/en-us/library/system.serviceprocess.servicebase.onstop.aspx) as a response to SCM requests.



## Properly Implementing ServiceBase.OnStart and ServiceBase.OnStop



The service enters the "starting" state before [ServiceBase.OnStart](http://msdn.microsoft.com/en-us/library/system.serviceprocess.servicebase.onstart.aspx) is called, and only enters the "started" state when OnStart returns. So, a service that is always "starting" and never "started" is a pretty good indication that OnStart isn't returning.





OnStart cannot be a "main loop" for a service. Many services work just fine without a main loop, but if one is required, then OnStart should start a thread and then return, letting the thread run the actual main loop. If OnStart will take more than 30 seconds to return, then it should call [ServiceBase.RequestAdditionalTime](http://msdn.microsoft.com/en-us/library/system.serviceprocess.servicebase.requestadditionaltime.aspx).





Similarly, the service enters the "stopping" state before [ServiceBase.OnStop](http://msdn.microsoft.com/en-us/library/system.serviceprocess.servicebase.onstop.aspx) is called, and enters the "stopped" state when OnStop returns. If OnStop will take more than 20 seconds, then it should call [ServiceBase.RequestAdditionalTime](http://msdn.microsoft.com/en-us/library/system.serviceprocess.servicebase.requestadditionaltime.aspx).



## The Current Directory



Services do not start with their current directory set to where their executable is. They usually end up running with their current directory set to the Windows or Windows System folder. It's not unusual for Windows Services to set their current directory near the beginning of their Main method, before calling [ServiceBase.Run](http://msdn.microsoft.com/en-us/library/system.serviceprocess.servicebase.run.aspx):



{% highlight csharp %}Environment.CurrentDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
{% endhighlight %}

## Services and Threading



Deep within the bowels of the OS, Windows Services are treated as a special sort of Console application. A Console application has a single thread by default and exits when that thread returns from Main; a Windows Service starts as a Console application and then passes ownership of its thread to the SCM by calling ServiceBase.Run. When the SCM decides to exit the service process (after all its services have been stopped), it will return control back to Main, which is expected to immediately exit.





The ServiceBase events (such as OnStart and OnStop) execute within the context of a worker thread. Therefore, the default synchronization context for .NET services is unsynchronized (e.g., [SynchronizationContext.Current](http://msdn.microsoft.com/en-us/library/system.threading.synchronizationcontext.current.aspx) is null). Windows Services usually employ one of two threading models:



1. Create a "main loop" thread within OnStart, and have this thread respond to events (including the OnStop event).
1. Start at least one asynchronous operation (such as a Timer, listening socket, or FileSystemWatcher), and have the completion handlers take the appropriate actions.




Note that both of these models return from OnStart after a short period of time (either starting the main thread or starting an asynchronous operation).





A reminder about garbage collection is in order: if the only reference to an object is in a completion routine, then that object is eligible for garbage collection. This is true for any type of .NET process, but most often causes problems with services that choose to use the second threading model described above.





Even if a service uses a "main loop" thread, the default SynchronizationContext is still in effect, resulting in free-threaded completion routines even for EBAP components (EBAP: [Event-Based Asynchronous Pattern](http://msdn.microsoft.com/en-us/library/wewwczdw.aspx)). This means that EBAP components such as [BackgroundWorker](http://msdn.microsoft.com/en-us/library/system.componentmodel.backgroundworker.aspx) may not perform as expected. The [Nito.Async](http://nitoasync.codeplex.com/) library contains an ActionThread that is ideal for the "main loop" thread of a Windows Service; see the Nito.Async documentation for details and examples.

