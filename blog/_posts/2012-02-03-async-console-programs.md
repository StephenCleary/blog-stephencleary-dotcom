---
layout: post
title: "Async Console Programs"
---
Once you start using asynchronous code, it kind of "grows" through your codebase. It's easier for asynchronous code to work with other asynchronous code, so it's natural to start making everything asynchronous.

If you're writing a console program, you may end up wanting an asynchronous main method, like this:

    class Program
    {
      static async void Main(string[] args)
      {
        ...
      }
    }

Unfortunately, that doesn't work (and in fact, the Visual Studio 11 compiler will reject an async Main method). Remember [from our intro post]({% post_url 2012-02-02-async-and-await %}) that an async method will _return_ to its caller before it is complete. This works perfectly in UI applications (the method just returns to the UI event loop) and ASP.NET applications (the method returns off the thread but keeps the request alive). It doesn't work out so well for Console programs: Main returns to the OS - so your program exits.

You can work around this by providing your own async-compatible context. [AsyncContext](http://nitoasyncex.codeplex.com/wikipage?title=AsyncContext) is a general-purpose context that can be used to enable an asynchronous MainAsync:

    class Program
    {
      static int Main(string[] args)
      {
        try
        {
          return AsyncContext.Run(() => MainAsync(args));
        }
        catch (Exception ex)
        {
          Console.Error.WriteLine(ex);
          return -1;
        }
      }
    
      static async Task<int> MainAsync(string[] args)
      {
        ...
      }
    }

That's all for today; next week we'll start looking at asynchronous unit tests, which suffer from a similar problem.

