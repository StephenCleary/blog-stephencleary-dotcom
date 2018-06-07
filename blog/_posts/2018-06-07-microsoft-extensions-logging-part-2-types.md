---
layout: post
title: "Microsoft.Extensions.Logging, Part 2: Types"
series: "Microsoft.Extensions.Logging"
seriesTitle: "Types"
description: "ILoggerFactory, ILoggerProvider, and ILogger."
---

## Factory Provider Repository Creators

Yeah, naming is hard.

In `Microsoft.Extensions.Logging`, there are two types in particular that I kept conflating: `ILoggerProvider` and `ILoggerFactory`. Even though they both can create instances of `ILogger`, they are actually completely different!

In this post, I'm going to cover the main types of `Microsoft.Extensions.Logging` and describe their intended use.

## LogLevel

Like all other logging frameworks, `Microsoft.Extensions.Logging` defines a sequence of levels for its logs. In increasing order of severity, they are `Trace`, `Debug`, `Information`, `Warning`, `Error`, and `Critical`. The [meanings of these values](https://docs.microsoft.com/en-us/dotnet/api/microsoft.extensions.logging.loglevel) are well documented, along with [advice on when to use each](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/logging/#log-level).

There is another "log level" which is not really a log level: `None`. This is technically part of the enumeration, but is used during *configuration* to indicate that no logs for that part of the system should be logged. The `None` value is not used during logging.

## ILogger

`ILogger` is a logger that your code can use to write log messages to. There are three core methods: `IsEnabled` tests whether a log level is enabled on that logger; `Log` is the core logging method that is used to write log messages; and `BeginScope` defines a logging scope.

We'll cover logging scopes later in this series. That leaves `IsEnabled` and `Log`, which are the core logging methods. There's a bunch of logging extension methods that build on that core; the common methods like `LogInformation` first call `IsEnabled` and then `Log`, with the appropriate arguments.

Internally, an `ILogger` has a "name" (also called a "category"). The idea is that each `ILogger` instance is used by a different component of the application.

`ILogger` is a base interface that provides the core logging functionality, but it is seldom used directly. There are some exceptions (e.g., Azure Functions will pass you an `ILogger`), but most of the time your code will log to an `ILogger<T>`.

## ILogger&lt;T&gt;

`ILogger<T>` is a logger that is named after a type `T`. All logs sent to an `ILogger<T>` (with the default implementation) will have a logger name/category of `typeof(T).FullName`. `ILogger<T>` is derived from `ILogger` and adds no new functionality.

If you're using dependency injection, an instance of `ILogger<T>` is usually injected into your type `T`. So, each time you have a constructor that takes an `ILogger<T>`, you are defining a "component" for your application.

Personally, I'm not a *huge* fan of this style of getting a logger, but it works. In my applications, the concept of a "component" seldomly has a 1:1 relationship with "types that log". It tends to work out best for ASP.NET Controllers, but less so for utility types used by services (where I usually want the utility type to use the service's component name). Of course, you can just pass an `ILogger<T>` (or `ILogger`) to the utility type, and that's the way this is generally resolved.

So, here's a question that you may have: if `ILogger<T>` provides no benefit over `ILogger` (other than being named after a type `T`), why does this type exist at all? The answer is logging extension methods, which we'll look at in more detail further in this series.

## ILoggerProvider

The logger provider is a type that (drum roll...) provides `ILogger` instances. But not just that; it provides `ILogger` instances *for a specific logging system*. Microsoft publishes [a few logger providers](https://github.com/aspnet/Logging/tree/dev/src) that support writing to [debugger output](https://www.nuget.org/packages/Microsoft.Extensions.Logging.Debug/), the [Console](https://www.nuget.org/packages/Microsoft.Extensions.Logging.Console/), the [Windows Event Log](https://www.nuget.org/packages/Microsoft.Extensions.Logging.EventLog/), [Event Tracing for Windows (ETW)](https://www.nuget.org/packages/Microsoft.Extensions.Logging.EventSource/), and others.

There are plenty of third-party logging providers, too. The primary purpose of a logging provider is to take log events and forward them to some logging backend. So there are logging providers for all kinds of logging backends: Serilog, Seq, log4net, etc. This allows you to write code that is independent of a logging framework (logging to an `ILogger<T>`), and the implementation at runtime hits a specific backend (or multiple ones!).

You can also create your own implementations of `ILoggerProvider`. In my [DotNetApis project](https://github.com/StephenClearyApps/DotNetApis), I have one provider that [stores logs in memory](https://github.com/StephenClearyApps/DotNetApis/blob/796f146e3027a0c470717befe33457c3dfeab50c/service/DotNetApis.Common/InMemoryLoggerProvider.cs) so they can be returned to the frontend as part of the HTTP response, another provider that [streams JSON logs to a GZIP-compressed Azure blob](https://github.com/StephenClearyApps/DotNetApis/blob/796f146e3027a0c470717befe33457c3dfeab50c/service/DotNetApis.Common/JsonLoggerProvider.cs), and several others.

Creating reusable implementations of `ILoggerProvider` is perhaps the most underdocumented part of `Microsoft.Extensions.Logging`. The providers in my DotNetApis project at this point are incomplete; there is no way I would put them in a NuGet package or anything. A proper, reusable `ILoggerProvider` is more involved; later in this series I'll look specifically at implementing `ILoggerProvider` properly, and cover all the necessary details.

`ILoggerProvider` is a way to extend `Microsoft.Extensions.Logging` by *implementation*. However, you don't ever want to *consume* a logger provider directly. Even though `ILoggerProvider.CreateLogger` creates `ILogger` instances, you never actually want to call that method to *get* a logger. To get loggers, you want to use dependency injection or `ILoggerFactory`.

## ILoggerFactory

`ILoggerFactory` is the mastermind that brings together all the types above. Conceptually, an `ILoggerFactory` has a collection of `ILoggerProvider`s, and the `ILoggerFactory` creates `ILogger<T>` instances for the application.

### Registering ILoggerProviders with ILoggerFactory

This is where the official documentation starts to fall short. In the ASP.NET Core world, ASP.NET Core itself takes care of creating an `ILoggerFactory` instance, which it then passes to your application to configure. Your application can then call `AddProvider` or a higher-level provider-specific method such as `AddDebug`, `AddConsole`, etc.

Fortunately, even without ASP.NET Core, it's not too difficult to do this yourself using the `LoggerFactory` type in `Microsoft.Extensions.Logging`:

{% highlight csharp %}
var loggerFactory = new LoggerFactory()
    .AddDebug()
    .AddConsole();
{% endhighlight %}

### Getting ILogger&lt;T&gt; Instances from ILoggerFactory

In the ASP.NET Core world, the `ILoggerFactory` is included in your Dependency Injection container, and it already knows how to get `ILogger<T>` values out of it, and everything is magical rainbows.

When you're outside of the ASP.NET Core world, you can still use `ILoggerFactory` in this way. You just have to:

1. Provide the `ILoggerFactory` instance to your DI container.
2. Configure your DI container to resolve `ILogger<T>` instances by calling `ILoggerFactory.GetLogger<T>()`.

The exact instructions on how to do this depends on your DI container of choice.

Of course, there's another option, too. You can just provide the `ILoggerFactory` instance, and your consuming types can take the `ILoggerFactory` and create their own `ILogger<T>`.

## What Are the ILoggerFactory's ILoggers?

Before closing out this post, I just want to point out that the `ILogger` instances provided by `ILoggerFactory` are *not* the same as the `ILogger` instances provided by `ILoggerProvider`. An `ILoggerProvider` `ILogger` is a logger that logs *to that specific provider*. An `ILoggerFactory` `ILogger` is a logger that logs *to all registered providers*.

In other words, the `ILoggerFactory` `ILogger`/`ILogger<T>` loggers are *composite* loggers; they forward log messages to each provider's `ILogger`.

# Review

This post has described the various logging types from the component's perspective (`ILogger`) and working out towards the application's perspective (`ILoggerFactory`). Let's briefly review, going the other way this time.

`ILoggerFactory` is a collection of `ILoggerProvider`s that creates composite `ILogger`/`ILogger<T>` loggers.

`ILoggerProvider` ia a provider for a *specific* logging system. It provides `ILogger` loggers to the `ILoggerFactory`.

Each component gets an `ILogger<T>` (or `ILogger`) from the `ILoggerFactory` that it should use for logging.