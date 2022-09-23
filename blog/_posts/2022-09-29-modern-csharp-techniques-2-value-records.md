---
layout: post
title: "Modern C# Techniques, Part 2: Value Records"
series: "Modern C# Techniques"
seriesTitle: "Value Records"
description: "Using value records (i.e., value objects) to overcome primitive obsession. Part of a series looking at modern C# code techniques."
---

Today I'll cover another technique for modern C# that isn't exactly *new*, but which the language supports much better than it used to.

I'm calling today's technique Value Records, which are a form of the more general "value object" pattern that is specific to modern C#. But before we dive into the solution, let's look at the problem.

## Primitive Obsession

The name of the antipattern we're trying to remove is Primitive Obsession. This is a highly searchable term with some great descriptions out there.

Essentially, Primitive Obsession is when a developer (over)uses primitives (`string`, `int`, `Guid`, `decimal`, etc) to represent business or domain concepts. One classic example is entity ids. There are a couple of problems with using primitives like this:
1. The primitives are not type-safe. In other words, it's easy to accidentally pass a `customerId` to a method expecting a `resourceId`. Or, say, if a method needs *both* a `customerId` and `resourceId`, it's easy to pass the parameters in the wrong order.
2. Primitives support operations that don't make sense. For example, if a `customerId` is an `int`, the compiler will happily let you divide it by 2, but that makes no business sense at all. A related problem is when you have *units*, such as `distanceInFeet` and `distanceInMeters`; the lack of units in the type system allow these values to be (incorrectly) added together.

## Solving Primitive Obsesesion with Value Objects

Value Objects are the general solution to Primitive Obsession. The idea is that you define a (simple) domain object that wraps the primitive type inside a type-safe wrapper type. In some cases, this wrapper type may have some limited domain behavior (such as validation), but in many cases Value Objects are so-called "anemic domain models", and that's OK. Value Objects inhabit a middle ground between primitives and full-blown domain objects.

Value Objects tend to behave similarly to the primitives they replace:
- Value Objects are usually immutable.
- Value Objects usually have value semantics.
- If a primitive has a useful operation (e.g., string concatenation, integer addition, or even `ToString` or `GetHashCode` support) that also makes sense for the domain object, then Value Objects usually support those operations.

Following these patterns allows Value Objects to essentially be type-safe replacements for primitives. Occasionally some business rules are added (e.g., validation), but just plain old Value Objects work quite well on their own, too.

## C# and Value Records

The modern C# technique for Value Objects is what I call Value Records, and looks like this:

```C#
public readonly struct record CustomerId(string Value);
```

Yes, that's the whole type.

Breaking it down:
- Records provide value semantics, complete with equality, hash code, and `ToString` support.
- Struct records provide a value-type wrapper, avoiding heap allocation (the size of the wrapper *is* the size of the wrapped value).
- Readonly struct records provide immutability.

I prefer to use a single property named `Value`, which is similar to `Nullable<T>.Value`, except a Value Record *always* has a valid `Value`. Since the type definition is just a single line, it seems silly to have them follow the one-type-per-file rule; I tend to collect Value Record types and include them in a single source file, usually called `Primitives.cs`.

## Guidelines for Use

### Avoid Sharp Edges

  - Recommendation: don't try to include serialization in the value record. Ignore Newtonsoft.Json / System.Text.Json / Xml, ASP.NET parameter binding / output formatting, WPF bindings, Entity Framework value conversions, serialization, etc.

### Choosing Primitives to Replace

E.g., validated email.

## Misuse

Overuse again. Complexity of code; every developer knows what `string` is, but if they see a `CustomerId` type, then they need to look that up.

"Primitive Obsession Obsession"

    - `string CustomerName` is probably just fine. Doesn't have any real validation and is unlikely to be confused with something else. Unless your domain has a ton of "names", in which case maybe it *can* be easily confused with another kind of name and *should* be a value record.

## History

Until records were added to the language, immutable objects were always a bit of a pain, with some going so far as to [create libraries just to help write immutable types](https://github.com/AArnott/ImmutableObjectGraph). The Value Object pattern was used only when absolutely necessary, since it resulted in much more complex code (with a naturally higher probability of containing bugs).

Record types brought immutability as a first-class citizen. The initial record types were class records (reference types), which works great for many immutable types, but tends to be a heavyweight solution for Value Objects. E.g., when wrapping `int` values, a `class record` wrapper essentially acts as an always-boxed `int`.

These days we have readonly struct records, which are immutable *and* lightweight, and a perfect fit for the Value Object pattern.

## Summary
