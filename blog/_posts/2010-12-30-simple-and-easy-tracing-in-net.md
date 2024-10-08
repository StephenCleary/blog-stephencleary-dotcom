---
layout: post
title: "Simple and Easy Tracing in .NET"
---
.NET includes a fairly complete tracing framework built right in, but there isn't much in the way of "getting started" tutorials that provide best practices. So, here's a simple guide to the built-in .NET tracing support, skipping all the hard stuff. :)

## Define the TraceSource

One [TraceSource](http://msdn.microsoft.com/en-us/library/system.diagnostics.tracesource.aspx?WT.mc_id=DT-MVP-5000058) should be defined per "component." The "component" is up to you; it's a group of trace statements that can be enabled or disabled together.

A [TraceSource](http://msdn.microsoft.com/en-us/library/system.diagnostics.tracesource.aspx?WT.mc_id=DT-MVP-5000058) needs a name, and that name should be globally unique. Something like "MyCompany.MyProduct.MyComponent" should suffice.

You usually want to define the [TraceSource](http://msdn.microsoft.com/en-us/library/system.diagnostics.tracesource.aspx?WT.mc_id=DT-MVP-5000058) as a **static** member of a class being traced. The example code below uses a **private static** [TraceSource](http://msdn.microsoft.com/en-us/library/system.diagnostics.tracesource.aspx?WT.mc_id=DT-MVP-5000058), but if other classes use the same source, you may prefer **internal static**:

    public class MyComponentClass
    {
      private readonly static TraceSource Tracer = new TraceSource("MyCompany.MyProduct.MyComponent");
      ...
    }

## Use the TraceSource

Add calls to [TraceSource](http://msdn.microsoft.com/en-us/library/system.diagnostics.tracesource.aspx?WT.mc_id=DT-MVP-5000058) methods where you want tracing to take place:

{% highlight csharp %}

public void Frob(string arg)
{
  Tracer.TraceInformation("Frobbing " + arg);
  ...
}
{% endhighlight %}

The [TraceSource](http://msdn.microsoft.com/en-us/library/system.diagnostics.tracesource.aspx?WT.mc_id=DT-MVP-5000058) class provides several tracing methods; the most common are [TraceInformation](http://msdn.microsoft.com/en-us/library/system.diagnostics.tracesource.traceinformation.aspx?WT.mc_id=DT-MVP-5000058) (used for informational message) and [TraceEvent](http://msdn.microsoft.com/en-us/library/system.diagnostics.tracesource.traceevent.aspx?WT.mc_id=DT-MVP-5000058) (used for any type of message).

The types of messages [include](http://msdn.microsoft.com/en-us/library/system.diagnostics.traceeventtype.aspx?WT.mc_id=DT-MVP-5000058) (in increasing order of severity): Verbose, Information, Warning, Error, and Critical.

## Enable the TraceSource

Defining and using the TraceSource is all the changes that need to be made to the code. However, running the code above will not actually cause any tracing to be done at runtime, because the TraceSource is not enabled.

To enable the TraceSource, you'll need to merge the following with your app.config or web.config (and restart the application):

    <configuration>
      <system.diagnostics>
        <sources>
          <source name="MyCompany.MyProduct.MyComponent" switchValue="All" />
        </sources>
      </system.diagnostics>
    </configuration>

The switchValue attribute [may be set to](http://msdn.microsoft.com/en-us/library/system.diagnostics.sourcelevels.aspx?WT.mc_id=DT-MVP-5000058) Off, Critical, Error, Warning, Information, Verbose, or All. This setting interacts with the TraceEvent message types exactly as you'd expect:

<div class="panel panel-default" markdown="1">

{:.table .table-striped}
||Verbose|Information|Warning|Error|Critical|
|-
|Off|<i class="fa fa-times fa-lg no"></i>|<i class="fa fa-times fa-lg no"></i>|<i class="fa fa-times fa-lg no"></i>|<i class="fa fa-times fa-lg no"></i>|<i class="fa fa-times fa-lg no"></i>|
|Critical|<i class="fa fa-times fa-lg no"></i>|<i class="fa fa-times fa-lg no"></i>|<i class="fa fa-times fa-lg no"></i>|<i class="fa fa-times fa-lg no"></i>|<i class="fa fa-check fa-lg yes"></i>|
|Error|<i class="fa fa-times fa-lg no"></i>|<i class="fa fa-times fa-lg no"></i>|<i class="fa fa-times fa-lg no"></i>|<i class="fa fa-check fa-lg yes"></i>|<i class="fa fa-check fa-lg yes"></i>|
|Warning|<i class="fa fa-times fa-lg no"></i>|<i class="fa fa-times fa-lg no"></i>|<i class="fa fa-check fa-lg yes"></i>|<i class="fa fa-check fa-lg yes"></i>|<i class="fa fa-check fa-lg yes"></i>|
|Information|<i class="fa fa-times fa-lg no"></i>|<i class="fa fa-check fa-lg yes"></i>|<i class="fa fa-check fa-lg yes"></i>|<i class="fa fa-check fa-lg yes"></i>|<i class="fa fa-check fa-lg yes"></i>|
|Verbose|<i class="fa fa-check fa-lg yes"></i>|<i class="fa fa-check fa-lg yes"></i>|<i class="fa fa-check fa-lg yes"></i>|<i class="fa fa-check fa-lg yes"></i>|<i class="fa fa-check fa-lg yes"></i>|
|All|<i class="fa fa-check fa-lg yes"></i>|<i class="fa fa-check fa-lg yes"></i>|<i class="fa fa-check fa-lg yes"></i>|<i class="fa fa-check fa-lg yes"></i>|<i class="fa fa-check fa-lg yes"></i>|

</div>

Tip: you can leave the **source** element defined in your app.config / web.config when you deploy to production. As long as its switchValue is set to Off, it won't actually trace but it's easy to find and turn on.

Another tip: TraceSource works the same in both Debug and Release builds, so it's great for instrumenting an application heading to production where you want to enable or disable the tracing without recompiling.

## Observe the Trace Messages

Simple tracing can be observed in one of two ways:

- If you run the application in the Visual Studio Debugger, all enabled trace output is written to the Output window.
- If you're running in production (or for some other reason can't use the debugger), a free Microsoft tool called [DebugView](http://technet.microsoft.com/en-us/sysinternals/bb896647?WT.mc_id=DT-MVP-5000058) may be used to view the traces in real-time (and optionally log them to a file). Tip: uncheck "Options" "Force Carriage Returns" to make the trace output line up correctly.

## More Power!

This blog post just scratched the surface of the complexity of the built-in .NET tracing system. It is extensible in many ways:

 - You can add "trace listeners". The simple tracing above just uses the Default trace listener. .NET also includes listeners for logging to the Event Log, a text file, an XML file, or the Console. In addition, you can write your own trace listener (e.g., a rollover log so that the file doesn't get too large).
 - You can add trace switches. Our simple tracing just sets a single switchValue for each TraceSource. It's possible to add trace switches that are shared between multiple trace sources. In addition, you can define your own _type_ of trace switch.
 - You can add trace filters. For example, it's possible to put in a trace filter that sends some messages to one trace listener and other messages to another trace listener. You can also define your own trace filter type, if the built-in ones aren't sufficient.
 - There are additional types of messages that fall under "ActivityTracing": Start, Stop, etc. These can be used in addition to the existing Verbose to Critical hierarchy.
 - There's a [Trace.CorrelationManager](http://msdn.microsoft.com/en-us/library/system.diagnostics.trace.correlationmanager.aspx?WT.mc_id=DT-MVP-5000058) class that enables "grouping" of traces into logical operations. This is used in conjection with the ActivityTracing messages to relate traces together that would otherwise get intermixed with other traces.

For more information, see [this SO answer](http://stackoverflow.com/questions/576185/logging-best-practices/939944#939944) and the [MSDN documentation](http://msdn.microsoft.com/en-us/library/zs6s4h68.aspx?WT.mc_id=DT-MVP-5000058).

## Other TraceSources

Many people aren't aware that a lot of the .NET built-in libraries have already been instrumented with their own TraceSources. [Network activity](http://msdn.microsoft.com/en-us/library/ty48b824(v=VS.100).aspx?WT.mc_id=DT-MVP-5000058), for example, or [tons of tracing for WPF](http://msdn.microsoft.com/en-us/library/system.diagnostics.presentationtracesources.aspx?WT.mc_id=DT-MVP-5000058). The two that I've found most useful are **System.Net.Sockets** for TCP/IP sockets and **System.Windows.Data** for debugging data binding errors. There are probably many more out there...

