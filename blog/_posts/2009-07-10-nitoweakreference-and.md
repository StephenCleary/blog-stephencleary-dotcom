---
layout: post
title: "Nito.WeakReference and Nito.WeakCollection"
---
## A Replacement for System.WeakReference

There are two minor problems with the [System.WeakReference](http://msdn.microsoft.com/en-us/library/system.weakreference.aspx?WT.mc_id=DT-MVP-5000058) class: type safety and garbage collection.

The first one is a simple problem; `System.WeakReference` only deals with instances of type `object`. However, it is not difficult to write a type-safe wrapper for `System.WeakReference` that provides type safety.

The second problem is a little more subtle. A weak reference is a wrapper around a [`System.Runtime.InteropServices.GCHandle`](http://msdn.microsoft.com/en-us/library/system.runtime.interopservices.gchandle.aspx?WT.mc_id=DT-MVP-5000058). The core `GCHandle` type is both powerful and dangerous; it is halfway unmanaged. `System.WeakReference` provides a safer class that only uses `GCHandle` as a weak reference.

The problem is that a `GCHandle` actually represents an entry in the runtime's GCHandle table. This table interacts with the garbage collector, preventing some objects from being GC'ed or moved. It is also used to create and track weak references. Logically, allocated GCHandles are unmanaged resources, and `System.WeakReference` does clean up its GCHandle correctly in its finalizer.

However, `System.WeakReference` does not implement [`System.IDisposable`](http://msdn.microsoft.com/en-us/library/system.idisposable.aspx?WT.mc_id=DT-MVP-5000058). This is not the end of the world, but it does place additional pressure on the garbage collector. It forces every `System.WeakReference` into an extra generation, holds the GCHandle table entry longer than necessary (possibly causing extra work for the garbage collector, since it must update the GCHandle table entry when that object is collected), and requires the finalizer thread to actually release the resource.

[`Nito.WeakReference<T>`](http://nitomvvm.codeplex.com/sourcecontrol/changeset/view/27265?projectName=NitoMVVM#453686) is a strongly-typed, disposable replacement for `System.WeakReference`. [Note, however, that `Nito.WeakReference<T>` does not support resurrection, which is a complex use case that is only required in very rare situations.]

## A Simple Weak Collection

A weak collection (or more properly, a collection of weak references) at any given time may contain references to both live (referenced) and dead (garbage collected) objects. Removing all the dead objects is called a "purge". It's convenient to be able to enumerate the collection both with and without a purge. The [`Nito.IWeakCollection<T>`](http://nitomvvm.codeplex.com/sourcecontrol/changeset/view/27265?projectName=NitoMVVM#453683) defines an interface for enumerable collections of weak references, and includes methods for enumerating or counting the collection both with and without purging. Some of its members include:

- `CompleteList` - Gets a complete sequence of objects from the collection (null entries represent dead objects).
- `LiveList` - Gets a sequence of live objects from the collection, causing a purge.
- `CompleteCount` - Gets the number of live and dead entries in the collection. O(1).
- `LiveCount` - Gets the number of live entries in the collection, causing a purge. O(n).

[`Nito.WeakCollection<T>`](http://nitomvvm.codeplex.com/sourcecontrol/changeset/view/27265?projectName=NitoMVVM#453683) is a `List`-based implementation of a weak collection. It is used by the Nito MVVM library to track weak events, but it should be useful in other situations as well.

