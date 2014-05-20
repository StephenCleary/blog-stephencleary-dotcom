---
layout: post
title: "Generic Methods with Dynamic Implementations"
---
Dynamic binding is one of the many new features included in .NET 4.0. I've been doing some testing today with mixing dynamic and generics, and came across a slightly obscure scenario that may or may not prove useful in the future.

When a dynamic object performs method resolution at runtime, the result is cached by the DLR in a "call site". The next time this code is run, the call site is only recomputed if there is a cache miss (i.e., different types of arguments are passed to the method).

We can use generics to turn one call site into many call sites, all cached independently, thereby improving performance when invoked later:

static dynamic SingleCallSite(dynamic arg1, dynamic arg2)
{
    return arg1 + arg2;
}

static T MultipleCallSites<T>(T arg1, T arg2)
{
    dynamic darg1 = arg1;
    return darg1 + arg2;
}

Even though the **SingleCallSite** method only has one call site (for the **+** operator) and the **MultipleCallSites** method has two call sites (one for the **+** operator, and one for converting the dynamic result to **T**), **MultipleCallSites** reliably runs faster, as this remarkably unscientific test code demonstrates:

static void Main(string[] args)
{
    try
    {
        int count = 1000000;
        Stopwatch sw = Stopwatch.StartNew();
        for (int i = 0; i != count; ++i)
        {
            MultipleCallSites(13, 17);
            MultipleCallSites("test1", "merged");
            MultipleCallSites(30.5, 23.5);
        }
        sw.Stop();
        Console.WriteLine("Multiple call sites: " + sw.Elapsed);
        sw.Restart();
        for (int i = 0; i != count; ++i)
        {
            SingleCallSite(13, 17);
            SingleCallSite("test1", "merged");
            SingleCallSite(30.5, 23.5);
        }
        sw.Stop();
        Console.WriteLine("Single call site: " + sw.Elapsed);
    }
    catch (Exception ex)
    {
        Console.WriteLine("Error: [" + ex.GetType().Name + "]: " + ex.Message);
    }

    Console.ReadKey();
}

This idea came from Luca Bolognese's blog post [Simulating INumeric with dynamic in C# 4.0](http://blogs.msdn.com/lucabol/archive/2009/02/05/simulating-inumeric-with-dynamic-in-c-4-0.aspx), where he states that with a generic signature "you get a different call site with each combination of type arguments and, since they are separate, the binding caches should stay small."

Final note: this is only an implementation detail of the DLR. These performance characteristics may change in a .NET service pack or future version.

