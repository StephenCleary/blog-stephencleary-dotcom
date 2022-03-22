---
layout: post
title: "ConditionalWeakTable"
---
I prefer to find bugs as early as possible. Starting unit tests early-on is a big helper towards that goal; unfortunately, I do find myself sometimes banging out code on tight time constraints, and I skip my unit tests. (Oh, how embarassing! I just admitted that right out in public and everything!)

Back in the pre-unit-testing world, when dinosaurs roamed the earth, there was another approach to finding bugs early: static typing. This is why I had (and - to be honest - still have) a preference for statically typed languages. If the compiler finds a bug, then that's one less bug I have to run into at runtime.

That said, dynamic languages are also awesome. I love the ability to do runtime binding with ease, and modify the structure of existing objects after they've been constructed. I do enough work in JavaScript to stand in awe of the language, and I've stated several times that Python is one of the best-designed languages in the world.

.NET 4.0 added the dynamic language runtime, allowing dynamic .NET languages to _truly_ exist for the first time. C# received much of the power of dynamic languages by adding the **dynamic** keyword. I was one of the ones cheering when this (finally) took place.

Now, I'm still a fan of statically-typed languages. In fact, I wish C# would add something equivalent to the extreme power and flexibility of C++ templates (supporting at least static polymorphism and implicit invokation of type generators). However, there are times when it's wonderful to just side-step the static typing and do something a bit "out of the box," and **dynamic** is just the ticket. Late binding, for example, or Reflection code that doesn't cause your fingers to fall off, or duck typing, or embedding a scripting engine for your end-users, or using an "expando" object with a runtime-defined structure.

The last of those examples is what [ConditionalWeakTable](http://msdn.microsoft.com/en-us/library/dd287757.aspx?WT.mc_id=DT-MVP-5000058) is all about. Most programmers are aware of [ExpandoObject](http://msdn.microsoft.com/en-us/library/system.dynamic.expandoobject.aspx?WT.mc_id=DT-MVP-5000058); however, many are not aware that ConditionalWeakTable allows them to "attach" additional information to existing, non-dynamic CLR objects.

Somehow I missed that class when the .NET 4.0 changes were announced, but Jeffrey Richter gave an example of it in his ".NET Nuggets" talk last week as part of [Wintellect's T.E.N.](http://www.wintellect.com/ten) event. Essentially, you can use ConditionalWeakTable to define a (threadsafe) mapping from an object _instance_ to any type of value you need. This allows you to treat any object as an "expando" object, "attaching" information to it. When the object instance is garbage collected, any attached values are automatically cleaned up as well.

This is a powerful concept, and it was the primary motivation behind my (pre-release) [Nito.Weakness](http://nitoweakness.codeplex.com/) library. According to Mr. Richter, ConditionalWeakTable is notified of object collection by the garbage collector rather than using a polling thread, which is good. There are a couple of caveats, though, when using CWT.

<div class="alert alert-danger" markdown="1">
<i class="fa fa-exclamation-triangle fa-2x pull-left"></i>

**Update 2011-01-22:** The Nito.Weakness library has been postponed indefinitely. Instead, I've released the ConnectedProperties library on both [CodePlex](http://connectedproperties.codeplex.com/){:.alert-link} and [NuGet](http://nuget.org/Packages/Packages/Details/Connected-Properties-(by-Nito-Programs)-1-0-0){:.alert-link}. ConnectedProperties is a straightforward wrapper for ConditionalWeakTable.
</div>

## Caveat 1: Restrictions on TKey

Be careful what type you specify for **TKey**. I stronly recommend that you only use types that use reference equality. This means that I _don't_ recommend you use **string** like Mr. Richter did during his demo (and in his example source code). It's well and good for the author of [CLR via C#](http://www.amazon.com/gp/product/0735621632?ie=UTF8&tag=stepheclearys-20&linkCode=as2&camp=1789&creative=390957&creativeASIN=0735621632) to use **TKey = System.String**, but mere mortals like you and I should steer clear. **string** not only uses value equality, but also has a complex interning feature. Remember, ConditionalWeakTable tracks object _instances_, not object _values_.

Nito.Weakness contains [some code (IsReferenceEquatable)](http://nitoweakness.codeplex.com/SourceControl/changeset/view/b85303561fd1#Source%2f_internal%2fExtensions.cs) to determine if a type uses reference or value equality, and refuses to track object instances that use value equality. Perhaps this is a bit strong, but I'm planning to add this requirement in any generic ConditionalWeakTable wrappers that I use in my own code.

## Caveat 2: IDisposable is ignored on TValue

ConditionalWeakTable will not dispose any **IDisposable** values attached to object instances. They will (eventally) be finalized, but the standard restrictions on finalizers apply. Mr. Richter does have an example in his downloaded code, using this as an "object-collection callback." However, I don't believe that would be usable in real code, simply because all permissible finalizer actions belong in the original class anyway (specifically, the disposing of unmanaged resources).

  

Even with these caveats, ConditionalWeakTable promises to be quite useful! It allows better "separation of concerns" in code, with a bit of an "aspect-oriented programming" feel.

