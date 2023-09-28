---
layout: post
title: "Q&A: Should I Set Variables to Null to Assist Garbage Collection?"
series: "IDisposable and Finalizers"
seriesOrder: 8
seriesTitle: "Q&A: Seting Variables to Null"
---
This is a common question with rather complex reasoning behind the answer.

First off, setting a variable to null _to assist garbage collection_ is different than setting a variable to null _to indicate state_. It's always proper to use "null" as a state indicator (e.g., the [CallbackContext]({% post_url 2009-04-24-asynchronous-callback-contexts %}) class has a field which is set to null to indicate the context is invalid).

Secondly, the "variable" being set to null may be either a _field_ (possibly via a property) or a _local variable_. Local variables include _method parameters_. A field may be a _static field_ or an _instance field_.

This blog entry is only concerned with the question "Should I set variables _to null to assist garbage collection?"_ and will consider each type of "variable".

## The Short Answer, for the Impatient

Yes, if the variable is a static field, or if you are writing an enumerable method (using **yield return**) or an asynchronous method (using **async** and **await**). Otherwise, no.

This means that in regular methods (non-enumerable and non-asynchronous), you do not set local variables, method parameters, or instance fields to null.

(_Even if you're implementing IDisposable.Dispose_, you _still_ should not set variables to null).

## The Longer Answer

- Static fields should be set to null when they are no longer needed - unless the process is shutting down, in which case setting static fields to null is unnecessary.
- Local variables hardly ever need to be set to null. There is only one exception:

 - It _may_ be beneficial to set local variables to null if running on a non-Microsoft CLR.

 - Instance fields hardly ever need to be set to null. There is only one exception:

  - An instance field may be set to null if the referencing object is expected to outlive the referenced object.
  - [Note that the semi-common practice of setting instance fields to null in IDisposable.Dispose does _not_ meet this test, and should not be encouraged].

There is a special consideration for enumerable and asynchronous methods. When compiling these methods, the compiler transforms the method into its own object. As a result, all local variables (including method parameters) are actually instance fields. If the "method object" is expected to outlive any of the objects referred to by those variables, then they _should_ be set to null.

In conclusion: generally speaking, setting variables to null to help the garbage collector is not recommended. If it is deemed necessary, then an unusual condition exists and it should be carefully documented in the code.

The rest of this post deals with the reasoning behind this recommendation.

## Required Reading

Most of this post relies heavily on Jeffrey Richter's awesome book [CLR via C#](http://www.amazon.com/gp/product/0735627045?ie=UTF8&tag=stepheclearys-20&linkCode=as2&camp=1789&creative=390957&creativeASIN=0735627045){:rel="nofollow"}. Unfortunately, even though the 3rd edition is out, I only have the 2nd; so all page numbers in this blog post are for the 2nd edition. The section "The Garbage Collection Algorithm" (pg 461) covers GC in general, and the section "Garbage Collections and Debugging" (pg 465) is particularly useful when considering this question.

## Determining Root Objects

The garbage collector is based on a "mark and sweep" design, starting from a set of root objects and walking any nested references to determine which objects are still in use. Any objects not marked are declared unused and become eligible for garbage collection [this is a simplification, but it's the general idea]. Logically, all the marked objects form a "graph" of live objects.

The idea behind "setting variables to null" is that it would help the garbage collector to detect that the referenced object is no longer used. Before we can determine if this truly is helpful or not, we must first determine what constitutes a "root object".

First: any static field is a root object. That's the easy part (we'll handle static fields in more detail later).

Instance fields are used to build the graph of referenced objects, so it's possible that setting an instance field to null may "trim" objects from the graph (we'll handle instance fields in more detail later, too).

Method-local variables (including parameters and the implicit "this" reference) are much tricker: they are _sometimes_ root objects.

As described in [CLR via C#](http://www.amazon.com/gp/product/0735627045?ie=UTF8&tag=stepheclearys-20&linkCode=as2&camp=1789&creative=390957&creativeASIN=0735627045){:rel="nofollow"}, the JIT compiler for a method will determine which native code blocks reference which variables by building a "root table" for the method. It's important to note that this table is quite accurate (though not 100% accurate - it _may_ "hold onto" references slightly longer than necessary if it simplifies the table). Examine the simple code below:

{% highlight csharp %}

static object CheckType(object a, Type b)
{
  Type t = a.GetType();
  // The object referenced by "a" may be eligible for GC here
  if (t == b)
  {
    Console.WriteLine("match!");
    return b;
  }
  else
  {
    Console.WriteLine("no match...");
    return null;
  }
}
{% endhighlight %}

The object referenced by "a" may be garbage collected as noted by the comments in this method (if it is not referenced elsewhere, of course). This is because the method's root table would declare that this method uses the "a" variable just for the code doing the "a.GetType()".

## When the JIT Compiler Behaves Differently (Debug)

There are two situations where the JIT compiler will _artificially extend_ the lifetime of local variables to the end of the method. The first is when the code is **compiled without optimizations and running under the debugger**. The second is when the code is **compiled with full debug information**.

If either of these situations is detected, the JIT compiler will change how it builds the root table so that in our example above, the object referenced by "a" cannot be eligible for garbage collection at least until the method returns.

By default, VS includes full debug information in "Debug" configuration builds but only includes pdb debug information in "Release" configuration builds. This means that the garbage collector does work differently when running "Debug" configuration code, even when run outside the debugger.

## When the JIT Compiler Behaves Differently (Release)

An interesting behavior of the JIT compiler is that when optimizations are enabled (by default in "Release" configurations), one of the optimizations it performs is _removing code that sets a local variable to null_.

It is rather ironic that some people religiously scatter "a = null;" throughout their methods, only to have them completely removed by the runtime.

By this point, it should be obvious that setting local variables to null (with the goal of helping the GC) is not beneficial. This practice only complicates the code and provides no help to the GC since it is removed anyway.

## Other CLRs and JIT Compilers

The above description of JIT compiler behavior is only applicable to the current Microsoft implementation. [Mono](http://www.mono-project.com/Compacting_GC), in particular, does _not_ build a root table when JIT-compiling a method (it treats all local variables as referenced until the end of the method).

Because of this different implementation, it may be useful to set local variables to null if the code will be running on Mono.

## Static Fields

Static fields are always root objects, so they are always considered "alive" by the garbage collector. If a static field references an object that is no longer needed, it _should_ be set to null so that the garbage collector will treat it as eligible for collection.

Setting static fields to null is meaningless if the entire process is shutting down. The entire heap is about to be garbage collected at that point, including all the root objects.

## Instance Fields

An instance field is how one object references another object. The garbage collector uses instance fields to build its graph of objects that are referenced (and thus uneligible for garbage collection).

Usually, when one object becomes eligible for garbage collection, it simultaneously makes all of its owned objects eligible for garbage collection as well. This happens perfectly naturally, without the need to set any instance fields to null.

Setting instance fields to null does not help the garbage collector in this case, since it marks the _referenced_ objects. The fact that one unreferenced object no longer references another unreferenced object has absolutely no bearing on how the GC builds its graph.

However, there is one case where setting an instance field to null _would_ help the garbage collector: if the owned (child) object is no longer necessary but the owning (parent) object will still be referenced for some time. In this case, setting the parent object's instance field to null would make the child object eligible for garbage collection. Note that this is a rare situation.

In particular, setting instance fields to null in an IDisposable.Dispose implementation is unnecessary. The parent object is being disposed; it cannot expect to be referenced much longer, and so it will not significantly outlive its child object(s).

## Conclusion

Static fields; that's about it. Anything else is a waste of time.

