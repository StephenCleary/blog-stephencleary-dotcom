---
layout: post
title: "Modern C# Techniques, Part 3: Generic Code Generation"
series: "Modern C# Techniques"
seriesTitle: "Generic Code Generation"
description: "Using structs as generic arguments with interface constraints to force code generation. Part of a series looking at modern C# code techniques."
---

This technique really excites me! We're in for a good ride today...

## C++ Templates and Code Generation

Before we take a look at C# generics, first I'll take a brief look at how C++ templates are used for code generation.

Templates (and generics) are both forms of polymorphic functions; that is, you define one type (or method) that takes a type parameter, and the generic type/method is able to change its behavior based on the type passed in.

C++ templates are purely a compile-time construct; they instruct the compiler how to generate the code for the template type/method. Specifically, the compiler performs a transformation called [monomorphization](https://en.wikipedia.org/wiki/Monomorphization); for each of the template arguments actually passed to the type/method, the compiler generates a new copy of the type/method specifically for that template argument.

Monomorphization is what enables C++ templates to be used as code generators.

## C# Generics

C# generics are a run-time construct; the compiler actually outputs the generic type/method itself into the IL (intermediate language). At runtime, the implementation of a generic type/method is shared between the generic arguments.

In other words, C# generics do *not* undergo monomorphization... except...

### Generics and Value Types

...except when used with value types!

C# generics do *not* undergo monomorphization for reference types; there's only one copy of the type/method implementation that is shared between all reference types. However, C# generics *do* undergo monomorphization for value types!

This makes sense; if a method `Something<T>` defines a local variable `T value;`, the compiler needs to know how big that `T` is. The size of a reference is the same regardless of the type being referred to, but the size of value type values can vary.

So, it turns out that C# generics *do* have monomorphization. They just don't do it for *all* generic arguments, only the ones that are value types. And monomorphization isn't done by the C# compiler; it's done by the JIT compiler (at runtime).

### Generics and Constrained Value Types

Monomorphization is fine for `List<T>` and friends, which don't actually *do* anything with the `T`.

For code generation, though, monomorphization is most useful if you also constrain the generics to a specific interface. A simple (and rather silly) example will make this more clear:

```C#
interface ISample
{
  int Setting { get; }
}

void Function<T>()
    where T : struct, ISample
{
  if (default(T).Setting == 13)
    Console.WriteLine("Ah, my favorite number!");
  else
    Console.WriteLine($"You passed {default(T).Setting}.");
}

readonly struct Sample7 : ISample
{
  public int Setting => 7;
}

readonly struct Sample13 : ISample
{
  public int Setting => 13;
}

Function<Sample7>();
Function<Sample13>();
```

The C# compiler just treats `Function` like an ordinary generic function. The JIT compiler will create *two separate copies* of `Function`; because `Sample7` and `Sample13` are both value types, monomorphization occurs and the JIT compiler generates *two* copies of the method. In both copies, the `default(T).Setting` code is emmitted as a constrained virtual call.

Then, each copy of the method has a high likelihood of being optimized. After all, the compiler knows the type of `T` for each copy. When it optimizes `Function<Sample7>`, it *knows* that the `default(T).Setting` is calling the `ISample.get_Setting` method on the `Sample7` type. The `Sample7` implementation of `ISample.Setting` is trivial and is likely going to be inlined, which means that the `if` branch can be precomputed. It is extremely likely that both copies of `Function<T>` only end up having a single `Console.WriteLine` call, without any `if` statement at all!

At this point, we have real code generation using C# generics!

### Generics and Constrained Value Types with Static Abstract Interface Methods

Static abstract interface methods allow us to clean this up even a bit more. Instead of defining `Setting` as an instance method, it can now be a static method, as such:

```C#
interface ISample
{
  static abstract int Setting { get; }
}

void Function<T>()
    where T : struct, ISample
{
  if (T.Setting == 13)
    Console.WriteLine("Ah, my favorite number!");
  else
    Console.WriteLine($"You passed {default(T).Setting}.");
}

readonly struct Sample7 : ISample
{
  public static int Setting => 7;
}

readonly struct Sample13 : ISample
{
  public static int Setting => 13;
}

Function<Sample7>();
Function<Sample13>();
```

Now there's no need for a `default(T)` value inside `Function<T>`.

## Warnings and Limitations

By using C# generics with value types, we can ensure monomorphization takes place; however, the rest of the behavior is not guaranteed.

### No Guarantees

The JIT compiler doesn't actually *guarantee* that any particular methods are inlined, or that any kind of optimization takes place (e.g., removing the `if` statement in our example). It's reasonable to assume that *some* optimization will take place, and with modern tiered optimization, you can also expect that the method will become *more* optimized if it is called a lot.

With C++ templates (and other compile-time-only monomorphization systems), you can *know* that the resulting code will be fully optimized. With C# generics, the optimization happens at runtime, so the runtime has to balance between executing the code *now* and speeding it up for *later*. At the end of the day, the best you can do is hope.

### Limited to Interfaces

When doing this kind of code generation, you're limited to only what can be expressed as interface members. You can define methods and properties, but not `const` values or nested types. It's not a *complete* code generation solution like C++ templates, but it's certainly useful nonetheless.

## Applications

### More Efficient General Algortihms

There are some algorithms that take parameters that often don't change for a given program. To take an example I'm familiar with, CRC32 hashes are actually a *class* of hash algorithms with different values for polynomials, initializers, and a few other parameters. Usually, a program that uses a CRC32 hash only uses *one* of these algorithms (one specific polynomial with a specific initializer value, etc). If the CRC32 implementation uses code generation, that will allow the JIT compiler to optimize just for that specific CRC32 hash algorithm.

### More General Algortihms

It's also possible to make one algorithm even more generic, especially due to the static interface methods. For a similar example to the above, CRC16 is *another* class of hash algorithms that is practically the same as CRC32 except it uses 16-bit integers instead of 32-bit integers. Static interface methods would allow a single unified "CRC" algorithm that can handle [any numeric type with generic math constraints](https://learn.microsoft.com/en-us/dotnet/csharp/whats-new/tutorials/static-virtual-interface-members?WT.mc_id=DT-MVP-5000058#generic-math). So our CRC32 and CRC16 implementations can be combined.

### Replacing Constant Arguments

There are a few situations in code where certain method arguments are always constant values. This is usually an indication that the method should be split into two methods, but sometimes there are maintenance concerns that are sufficiently strong, keeping the methods as a single implementation.

The example that I'm most familiar with here is the [boolean argument hack](https://learn.microsoft.com/en-us/archive/msdn-magazine/2015/july/async-programming-brownfield-async-development?WT.mc_id=DT-MVP-5000058#the-flag-argument-hack) for providing both synchronous and asynchronous versions of a method, which looks like this:

```C#
private Task<string> GetCoreAsync(bool sync)
{
  if (sync)
    Thread.Sleep(TimeSpan.FromSeconds(1));
  else
    await Task.Delay(TimeSpan.FromSeconds(1));
  return "Hi!";
}

public string Get() => GetCoreAsync(sync: true).GetAwaiter().GetResult();
public Task<string> GetAsync() => GetCoreAsync(sync: false);
```

In the code above, `GetCoreAsync` has a `sync` argument that is *always* a constant. Really, it *should* be two different methods, but if we pretend that `GetCoreAsync` is much longer and more complex, then making it two different methods does cause a maintenance burden.

So, let's use generic code generation to *generate* two different methods!

First, we'd extract the code differences (`Thread.Sleep` vs `Task.Delay`). These are going to need a definition in our interface, and they'll be implemented by each value type. Since we're talking about code that may be synchronous or asynchronous, we'll use value tasks as the return type. (Reminder: any time you have a method whose implementation *may* be asynchronous, then it should have an asynchronous signature). Then, `GetCoreAsync` can just invoke those interface methods. We end up with something like this:

```C#
private interface IDelay
{
  static abstract ValueTask DelayAsync(TimeSpan delay);
}

private readonly struct SynchronousDelay : IDelay
{
  static ValueTask DelayAsync(TimeSpan delay)
  {
    Thread.Sleep(delay);
    return new();
  }
}

private readonly struct AsynchronousDelay : IDelay
{
  static async ValueTask DelayAsync(TimeSpan delay) => await Task.Delay(delay);
}

private Task<string> GetCoreAsync<TDelay>()
    where TDelay: struct, IDelay
{
  await TDelay.DelayAsync(TimeSpan.FromSeconds(1));
  return "Hi!";
}

public string Get() => GetCoreAsync<SynchronousDelay>().GetAwaiter().GetResult();
public Task<string> GetAsync() => GetCoreAsync<AsynchronousDelay>();
```

The core implementation (`GetCoreAsync`) is simplified and is more obviously correct. The public interface (`Get` and `GetAsync`) didn't change at all. And at runtime, if only one path is used, then only one path will be JITted. If both paths are used, then two copies of `GetCoreAsync` are created by the JITter, each one optimized for its own situation (asynchronous or synchronous). This is a particularly useful technique for libraries, which may need to provide both forms of methods, but have a high likelihood of only one of them being used.

Stephen Toub discusses how the BCL uses this technique [in a recent blog post](https://devblogs.microsoft.com/dotnet/performance_improvements_in_net_7/?WT.mc_id=DT-MVP-5000058#:~:text=One%20final%20change%20related%20to%20reading%20and%20writing%20performance).

## Summary

Generic code generation provides a limited form of code generation in C#, because value types cause monomorphization. Static interface members provide an even nicer way to do generic code generation.
