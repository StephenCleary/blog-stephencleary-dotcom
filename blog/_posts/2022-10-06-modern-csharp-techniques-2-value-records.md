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
public readonly record struct CustomerId(string Value);
```

Yes, that's the whole type.

Breaking it down:
- Records provide value semantics, complete with equality, hash code, and `ToString` support.
- Struct records provide a value-type wrapper, avoiding heap allocation (the size of the wrapper *is* the size of the wrapped value).
- Readonly struct records provide immutability.

I prefer to use a single property named `Value`, which is similar to `Nullable<T>.Value`, except a Value Record *always* has a valid `Value`. Since the type definition is just a single line, it seems silly to have them follow the one-type-per-file rule; I tend to collect Value Record types and include them in a single source file, usually called `Primitives.cs`.

## Guidelines for Use

### Avoid Sharp Edges

I've used Value Records in a few projects now, and I think they work best as internal types. I don't mean `internal`; I mean as types that are created and unwrapped by your own code at the "edges" of your app. Then the core of your app *only* deals with the Value Records.

This way you don't have to deal with any kind of serialization, which can be a real headache. I recommend you completely ignore Newtonsoft.Json / System.Text.Json / Xml, ASP.NET parameter binding / output formatting, WPF bindings, Entity Framework value conversions, and all other forms of serialization. Instead of trying to automatically support these, just have your own code create the Value Record wrappers when reading the values from an external source (e.g., `CustomerId customerId = new(customerIdIntValue)`), and unwrap them when writing values to an external source (e.g., `int customerIdIntValue = customerId.Value;`).

### Choosing Primitives to Replace

Not all primitives *need* to be Value Records. Choosing which primitives *should* be Value Records is a skill, one which everyone is developing right now, and there are no masters of this skill yet AFAIK.

In my experience, I would say to use primitives by default, but use Value Records in the following situations:
1. Use Value Records whenever you have similar value types that are used together.
   - "Similar" here can mean *conceptually* similar or just having a similar *name*.
2. Use Value Records whenever you have to *guarantee* that a critical value type *must* be used only in a particular way.

The classic example of the first situation (similar types) is using Value Records for entity identifiers (`CustomerId` and friends). Entity identifiers are conceptually similar *and* have similar names, and usually there are multiple methods that deal with different kinds of identifiers at the same time.

The second situation (critical types) is more of a judgement call. I can at least give an example, though: one of my projects sends emails to users, but also has to deal with users that type in an email address that may or may not be theirs. And there are some severe repercussions for sending emails to people who haven't asked for them; there's actual laws in my country about that kind of behavior. So, I created a `ValidatedEmail` Value Record for this critical type. In my model, a user has a `ValidatedEmail` (whose value may be `null`), and the code component that sends emails *only* accepts a `ValidatedEmail`. It's comforting to know that the type system itself is enforcing the business rule "only send emails to validated email addresses".

## Misuse

Similar to last week's technique, the primary misuse of the Value Record technique is *overuse*. Once you start feeling the benefits of the stronger type safety from Value Records, you'll start wanting it everywhere. You may end up temporarily suffering from "Primitive Obsession Obsession", if you will.

There's a maintenance tradeoff with using Value Records: Value Records increase the code complexity. Speficially, Value Records increase the mental burden when reading the code. Put simply: every developer knows what a `string` is, but if they see a `CustomerId` type, then they need to look that up. Remember, techniques like Value Records are easy to write because they're in *your* head *now*, but the resulting code is more difficult to maintain (whether maintained by *others* or yourself *later*).

After all, it's not like primitives are evil or anything like that. Primitives are a perfectly fine solution for a lot of data. `string CustomerName` is probably just fine; it wouldn't have any real validation and is unlikely to be confused with something else. Well, that's true unless your domain has a ton of "names", in which case maybe it *can* be easily confused with another kind of name and *should* be a Value Record.

## History

Until records were added to the language, immutable objects were always a bit of a pain, with some going so far as to [create libraries just to help write immutable types](https://github.com/AArnott/ImmutableObjectGraph). The Value Object pattern was used only when absolutely necessary, since it resulted in much more complex code (with a naturally higher probability of containing bugs).

If you haven't read it yet, [Andrew Lock has a great blog series on using strongly-typed entity IDs to avoid primitive obsession](https://andrewlock.net/series/using-strongly-typed-entity-ids-to-avoid-primitive-obsession/). I think overall it's a good series conceptually, but do note that the implementation details are quite dated at this point. Also note how much the complexity is increased by trying to handle only a couple forms of serialization.

Record types brought immutability to C# as a first-class citizen. The initial record types were class records (reference types), which works great for many immutable types, but tends to be a heavyweight solution for Value Objects. E.g., when wrapping `int` values, a `class record` wrapper essentially acts as an always-boxed `int`.

These days we have readonly struct records, which are immutable *and* lightweight, and a perfect fit for the Value Object pattern.

## Summary

Carefully replacing some primitive types with Value Records will increase the type safety, correctness, and maintainability of your code.
