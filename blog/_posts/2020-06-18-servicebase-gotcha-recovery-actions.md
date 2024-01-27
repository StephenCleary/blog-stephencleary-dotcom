---
layout: post
title: "Win32 Service Gotcha: Recovery Actions"
series: "BackgroundService Gotchas"
seriesTitle: "Service Recovery Actions"
description: "ServiceBase will not allow Service Control Manager (SCM) recovery actions by default."
---

## Win32 Services and Recovery Actions

This blog post doesn't have to do with `BackgroundService` specifically, but it is an issue that can come up with .NET Core workers that are run as Win32 Services. In some ways, this blog post has more to do with [managed services]({% post_url 2013-10-10-managed-services-roundup %}), but I decided to put it with the `BackgroundService` series because it is a problem with `BackgroundService`s run as Win32 services.

## Background: Recovery Actions

The Win32 Service Control Manager (SCM) is responsible for starting and stopping services on Windows machines. It's also responsible for restarting Win32 services when they fail:

{:.center}
[![Win32 Service Recovery Action Settings]({{ site_url }}/assets/win32-service-recovery.png)]({{ site_url }}/assets/win32-service-recovery.png)

However, it can be a bit confusing to think about what "fail" actually *means*.

## Background: Win32 Service Failure

It's pretty clear that if a Win32 application crashes, that indicates "failure". Normally, Win32 services communicate with the SCM and let it know what their state is. The most common states are "stopped" and "started", along with transitional states like "stopping" and "starting". So, if a Win32 application exits (or crashes) without telling the SCM it is "stopped", then the SCM treats that as a failure.

What's much less clear is how exit codes are handled.

The first thing to keep in mind is that each Win32 service has its own exit code. A single Win32 process can contain *multiple* different Win32 services within that single process, and each of those Win32 services has its *own* exit code. As far as I can tell, the exit code of the process itself is completely ignored.

What's more, if the Win32 service does report that it is "stopped" to the SCM, the SCM will ignore the Win32 service exit code, too! The SCM assumes that if the service has reported it is "stopped", then the service has stopped successfully, and there is no need to restart the service.

This means that if you have a Win32 service and it reports a non-zero exit code (either for the process exit code or the Win32 service exit code), and if that Win32 service exits cleanly after setting its non-zero exit code, then that exit code will be ignored and the service will not be restarted.

## Tip: Honoring Win32 Service Exit Codes

There is [a flag](https://docs.microsoft.com/en-us/windows/win32/api/winsvc/ns-winsvc-service_failure_actions_flag?WT.mc_id=DT-MVP-5000058) you can set that will cause SCM to honor the Win32 service exit code, treating a non-zero code as a "failure" and running its recovery actions. You can turn this flag on at the command line as such:

{% highlight text %}
sc failureflag "My Service" 1
{% endhighlight %}

Setting that flag checks this checkbox, which has the rather difficult-to-understand wording of "Enable actions for stops with errors.":

{:.center}
[![Win32 Service Recovery Action Settings]({{ site_url }}/assets/win32-service-recovery-highlight.png)]({{ site_url }}/assets/win32-service-recovery-highlight.png)

## Win32 Service Exit Codes and ServiceBase

For .NET applications, the `Environment.ExitCode` property manages the exit code for the *process*. As far as I know, this value is always ignored when the process is run as a service. The Win32 service exit code is managed by the `ServiceBase.ExitCode` property. Remember, there can be multiple Win32 services in a single process.

## Win32 Service Exit Codes and WindowsServiceLifetime

In [.NET Core applications that are run as Win32 services](https://docs.microsoft.com/en-us/aspnet/core/host-and-deploy/windows-service?WT.mc_id=DT-MVP-5000058), it's normal to call `IHostBuilder.UseWindowsService()`, which installs a `WindowsServiceLifetime` as the `IHostLifetime`, instead of the default `ConsoleLifetime`.

`WindowsServiceLifetime` lets the SCM control the starting and stopping of the .NET Core application. It [derives from `ServiceBase`](https://github.com/dotnet/extensions/blob/4becf241089932aa1f1e7f3ab4155a437fd3dba1/src/Hosting/WindowsServices/src/WindowsServiceLifetime.cs#L14). Since [multiple `IHostLifetime` instances aren't supported](https://github.com/aspnet/Hosting/issues/1401), this means that .NET Core workers do not naturally support multiple Win32 services in a single process. It may be possible to support that by creating a new type that derives from `ServiceBase` and `IHostedService`, along with some kind of coordinating `IHostLifetime` implementation, but I'm not aware of anyone doing that yet. For now, all the .NET Core Win32 services I know of use `WindowsServiceLifetime`.

One important note is that `WindowsServiceLifetime` stops cleanly. If the .NET Core application is shutdown, then `WindowsServiceLifetime` reports to the SCM that the service is "stopped", and this means that the SCM will not restart the service.

You can write code that will set the Win32 service exit code on failure by accessing `ServiceBase.ExitCode` as such:

{% highlight csharp %}
public class MyBackgroundService : BackgroundService
{
  private readonly IHostLifetime _hostLifetime;
  public MyBackgroundService(IHostLifetime hostLifetime) =>
      _hostLifetime = hostLifetime;

  protected override Task ExecuteAsync(CancellationToken stoppingToken)
  {
    try
    {
      // Implementation
    }
    catch (Exception)
    {
      if (_hostLifetime is ServiceBase serviceLifetime)
        serviceLifetime.ExitCode = -1;
      else
        Environment.ExitCode = -1;
    }
  }
}
{% endhighlight %}

However, as noted above, you have to also set the `failureflag` on the service, or else the SCM will ignore the non-zero exit code.

If desired, you can write a custom `WindowsServiceLifetime` that will treat non-zero `Environment.ExitCode` values as non-zero Win32 service exit codes:

{% highlight csharp %}
public class MyWindowsServiceLifetime : WindowsServiceLifetime
{
  public MyWindowsServiceLifetime(IHostApplicationLifetime hostApplicationLifetime, IHostEnvironment environment, ILoggerFactory loggerFactory, IOptions<HostOptions> options)
      : base(environment, hostApplicationLifetime, loggerFactory, options)
  {
  }

  protected override void OnStop()
  {
    // Take the process ExitCode if there isn't one for our Win32 service
    if (ExitCode == 0 && Environment.ExitCode != 0)
      ExitCode = Environment.ExitCode;
    
    base.OnStop();
  }
}
{% endhighlight %}

This can be installed by adding this service (`services.AddSingleton<IHostLifetime, MyWindowsServiceLifetime>()`) after calling `UseWindowsService`; the .NET Core dependency injection will just take the last registered `IHostLifetime`.

<div class="alert alert-danger" markdown="1">
<i class="fa fa-hand-o-right fa-2x pull-left"></i>

Be sure to only override the `IHostLifetime` service _if the application is actually running as a Win32 service!_ I.e., use code like `if (WindowsServiceHelpers.IsWindowsService()) { services.AddSingleton<IHostLifetime, MyWindowsServiceLifetime>(); }`

H/t to David Hopkins in the comments!
</div>

## Crashing WindowsServiceLifetime

It is also possible to create a custom derived `WindowsServiceLifetime` that can detect application failures and will prevent `ServiceBase` from sending the "stopped" message to the SCM. That way, your service will be restarted regardless of the `failureflag` setting, because any failures will cause the process to crash.

We don't want to crash the process immediately; instead, we want to shut down the .NET Core host and then terminate, so that the SCM knows the process failed. The `WindowsServiceLifetime` type has [some similar behavior](https://github.com/dotnet/extensions/blob/4becf241089932aa1f1e7f3ab4155a437fd3dba1/src/Hosting/WindowsServices/src/WindowsServiceLifetime.cs#L105): if the SCM requests the service to stop, then it will shut down the .NET Core host and wait for it to stop. We can do the same thing in our custom lifetime type:

{% highlight csharp %}
public class MyWindowsServiceLifetime : WindowsServiceLifetime
{
  private readonly IHostApplicationLifetime _hostApplicationLifetime;
  private readonly ManualResetEventSlim _shutdownComplete;
  private readonly CancellationTokenRegistration _applicationStoppedRegistration;

  public MyWindowsServiceLifetime(IHostApplicationLifetime hostApplicationLifetime, IHostEnvironment environment, ILoggerFactory loggerFactory, IOptions<HostOptions> options)
      : base(environment, hostApplicationLifetime, loggerFactory, options)
  {
    _hostApplicationLifetime = hostApplicationLifetime;
    _shutdownComplete = new ManualResetEventSlim();
    _applicationStoppedRegistration = hostApplicationLifetime.ApplicationStopped.Register(() => _shutdownComplete.Set());
  }

  protected override void OnStop()
  {
    // Take the process ExitCode if there isn't one for our Win32 service
    if (ExitCode == 0 && Environment.ExitCode != 0)
      ExitCode = Environment.ExitCode;
    
    if (ExitCode != 0)
    {
      // Wait for application to shut down.
      _hostApplicationLifetime.StopApplication();
      _shutdownComplete.Wait();

      // Terminate app. Do not call base.OnStop().
      Environment.Exit(ExitCode);
    }

    // If we're exiting normally, just let WindowsServiceLifetime do its job.
    base.OnStop();
  }

  protected override void Dispose(bool disposing)
  {
    if (disposing)
    {
      _shutdownComplete.Set();
      _applicationStoppedRegistration.Dispose();
    }

    base.Dispose(disposing);
  }
}
{% endhighlight %}

This kind of `MyWindowsServiceLifetime` implementation will work whether or not you set the `failureflag` on your service.