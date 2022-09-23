---
layout: post
title: "Modern C# Techniques, Part 1: Curiously Recurring Generic Pattern"
series: "Modern C# Techniques"
seriesTitle: "Curiously Recurring Generic Pattern"
description: "The curiously recurring generic pattern, where a base type or interface has its derived type as a generic parameter. Part of a series looking at modern C# code techniques."
---

I'm starting a new series today looking at some modern C# techniques. Part of what I like about C# is that the language is always improving, and those improvements bring newer code patterns with them.

Today's topic is not actually *new*, but many developers haven't seen it before, so it's worth taking a look at. As with many of the techniques I'll be discussing, I'm not sure if this one has a name, so I am just calling it whatever I call it in my head.

## The Curiously Recurring Generic Pattern

<div class="alert alert-info" markdown="1">
<i class="fa fa-hand-o-right fa-2x pull-left"></i>

The "curious" name for this pattern comes from the C++ world, where it was called the Curiously Recurring Template Pattern. So in C# I just call it the Curiously Recurring Generic Pattern, since it's essentially the same thing but with generics instead of templates. [According to Wikipedia](https://en.wikipedia.org/wiki/Curiously_recurring_template_pattern){:.alert-link}, it's actually "F-bound polymorphism", but I'm not going to remember that.
</div>

The Curiously Recurring Generic Pattern is when an interface (or base type) takes a generic parameter that is its own derived type. A simple example looks like this:

```C#
interface IExample<TDerived>
{
}

class MyExample : IExample<MyExample>
{
}
```

## But Why Tho?

It essentially comes down to typing. If an interface (or base type) wants to use the *full, derived type* as a method parameter or return value, then it can define those methods itself without putting any burdern on the derived type.

Consider a familiar example from the .NET BCL: `IEquatable<T>`. `IEquatable<T>` [is defined](https://learn.microsoft.com/en-us/dotnet/api/system.iequatable-1?view=net-6.0&WT.mc_id=DT-MVP-5000058) as thus:

```C#
public interface IEquatable<T>
{
  bool Equals(T? other);
}
```

And it is used as such:

```C#
sealed class MyEquatable : IEquatable<MyEquatable>
{
  public bool Equals(MyEquatable? other) { ... }
}
```

The thing to note here is that `MyEquatable.Equals` implements `IEquatable<T>.Equals` with a *strongly-typed* `MyEquatable` argument. If the Curiously Recurring Generic Pattern wasn't used, then `IEquatable<T>` would just be `IEquatable` (taking an `object` argument), losing type safety and efficiency.

## Adding a Generic Constraint

The interface (or base type) may also use itself as a generic constraint. It doesn't *have* to (the examples above don't), but sometimes it's useful, particularly for base types. The Curiously Recurring Generic Pattern with generic constraints looks like this:

```C#
abstract class ExampleBase<TDerived>
    where TDerived : ExampleBase<TDerived>
{
  // Methods in here can use `(TDerived)this` freely.
  // This is particularly useful if this interface wants to *return* a value of TDerived.
  public virtual TDerived Something() => (TDerived)this;
}

class AnotherExample : ExampleBase<AnotherExample>
{
  // Implicitly has `public AnotherExample Something();` defined.
  // The base class method already has the correct return type.
  // (Can still override if desired).
}
```

As noted in the comments above, this approach is useful if `TDerived` is used as a *return* type. As one example, this is common with fluent APIs.

More generally, the generic constraint is needed in either of these situations:

1. The base type needs to treat a `ExampleBase<TDerived>` instance (e.g., `this`) as its derived type (i.e., `(TDerived)this`). This can also come up when passing `this` to other methods.
2. The base type needs to treat a `TDerived` as a `ExampleBase<TDerived>`, e.g., calling private base methods on an instance of type `TDerived` *other* than `this`. In this case no explicit cast is necessary.

## CRGP and Default Interface Methods

Similar to regular interface methods, the Curiously Recurring Generic Pattern can enhance the type safety of [default interface methods](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/proposals/csharp-8.0/default-interface-methods?WT.mc_id=DT-MVP-5000058) if necessary. This is similar to using CRGP with base types, except interfaces cannot have state. Put another way, this enables strongly-typed traits, but falls short of mixins.

## CRGP and Static Interface Methods (and Operators)

One possibility for CRGP with [static interface methods](https://learn.microsoft.com/en-us/dotnet/csharp/whats-new/tutorials/static-virtual-interface-members?WT.mc_id=DT-MVP-5000058) is to define operators (or other static methods) with the proper type signatures. Previously, CRGP required a base type to define operators (e.g., [`EquatableBaseWithOperators<TDerived>` in my Nito.Comparers library](https://github.com/StephenCleary/Comparers/blob/48cd202db5d7ea7209cc4248bf6a531d3752f170/src/Nito.Comparers.Core/EquatableBaseWithOperators.cs)), but using CRGP with static interface methods allows strong typing for operator signatures (e.g., [`IUnaryNegationOperators<TSelf, TResult>`](https://learn.microsoft.com/en-us/dotnet/api/system.numerics.iunarynegationoperators-2?view=net-7.0&WT.mc_id=DT-MVP-5000058) is an interface that defines `operator-` with the proper type signature).

## Misuse

Like other code patterns, the CRGP can be misused. IMO the most common misuse of this pattern is *overuse*. Bear in mind CRGP isn't powerful enough to provide mixins - even with default interface methods (which provide traits, not mixins).

Also, CRGP tends to make the code more complex. There's a tradeoff there, and you need to keep maintainability in mind.

## Summary

The Curiously Recurring Generic Pattern isn't actually new, and it isn't often necessary, but it's a nice tool to have when you do need it.
