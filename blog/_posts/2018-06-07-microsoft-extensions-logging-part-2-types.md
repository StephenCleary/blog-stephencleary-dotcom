---
layout: post
title: "Microsoft.Extensions.Logging, Part 2: Types"
series: "Microsoft.Extensions.Logging"
seriesTitle: "Types"
description: "ILoggerFactory, ILoggerProvider, and ILogger."
---

## Factory Provider Creators

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

`ILogger` is a base interface that provides the core logging functionality, but it is seldom used directly. There are exceptions (e.g., Azure Functions will pass you an `ILogger`), but most of the time your code will log to an `ILogger<T>`.

## ILogger&lt;T&gt;

`ILogger<T>` is a logger that is named after a type `T`. If you're using dependency injection, 

## ILoggerProvider

## ILoggerFactory
