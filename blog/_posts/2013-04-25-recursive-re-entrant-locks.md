---
layout: post
title: "Recursive (Re-entrant) Locks"
---
Time for another potentially-controversial subject: recursive locks, also known as re-entrant locks or recursive mutexes.



## Recursive Locks: Definition

A "recursive" lock is one that, when locked, will first determine _whether it is already held_, and if it is, it will simply allow the code to acquire it recursively. Recursive locks usually use a reference counting mechanism so that they can be acquired several times and then released several times, but the lock is only actually _unlocked_ when it has been released as many times as it has been acquired.



Traditionally, recursive locks have been the default on Microsoft platforms. The `lock` statement, `Monitor`, `Mutex`, and `ReaderWriterLock` are all recursive. However, newer types are starting to change this; `SpinLock` is not recursive, and `ReaderWriterLockSlim` is not recursive by default (it does provide recursion as an option).



If you Google around for opinions, you'll find some programmers are adamantly in favor of recursive locks, and other programmers are just as adamantly opposed to them. It's actually kind of funny to see how heated the arguments can get, since this is a topic that's hard to explain to "outsiders"; it's hard for a non-techie spouse to understand how strongly we geeks feel about this topic.



<!--

<h4>Background</h4>

<p>Regardless of where you stand on the recursion issue, I hope everyone can agree on the following fundamental principles of locks:</p>

<ol>
<li>The purpose of a "lock" is to read or update a well-defined set of shared data in a way that appears atomic to the rest of the program. There are other coordination primitives that serve other purposes (e.g., "signals" for notifying the rest of the program of an event), but the core purpose of a <i>lock</i> is to protect mutable shared state.</li>
<li>You should hold a lock only as long as you need it.</li>
<li>Don't block while holding a lock.</li>
<li>Never, ever, <i>ever</i> call end-user code while holding a lock. This includes delegate invocation, raising events, dynamic invocation, virtual method invocation, etc. Since end-user code is outside your control, it can cause a deadlock by waiting for some other thread that is attempting to take the lock you're holding.</li>
<li>If you ever have a situation where you need to hold more than one lock at a time, those locks should <i>always</i> be acquired in the same order everywhere in your program. In other words, define and document your lock hierarchy.</li>
</ol>

<p>Most programmers with a few years of multithreading experience tend to gravitate towards these principles. You can find many similar lists via Google.</p>

-->

## Joining the Controversy

Recursive locks are bad.



Oh yeah, I _did_ just go there. I'll lay out my reasoning in the remainder of this blog post. But before I go further, I should point out where I'm coming from. A lot of the pro-recursive crowd claims that the anti-recursionists are just "theorists" and that recursive locks make for easier coding. I am an anti-recursionist who is definitely not a theorist: I've written many, many real-world multithreaded programs and have used recursive locks and have _experienced their shortcomings_. So my anti-recursive stand is the direct result of being in the trenches, not an ivory tower.



There are several reasons why I think recursive locks are a poor choice in general, and only one situation where I think they could possibly be helpful (I've never been in that situation, but they could _theoretically_ be useful there).



## Inconsistent Invariants

The number one argument against recursive locks is one of _inconsistency_. The entire purpose of a "lock" is to protect shared mutable state so that reads and writes appear atomic. In other words, the state is _consistent_ whenever the lock is unlocked; while the lock is locked (held) by a section of code, the state may be _inconsistent_. Another way of saying this is that while the lock is held, it's possible that invariants will not hold; acquiring a lock temporarily suspends all contractural invariants until the time the lock is released.



{% highlight csharp %}public void A()
{
  lock (_mutex)
  {
    ...
  }
}

public void B()
{
  lock (_mutex)
  {
    ...
    A();
    ...
  }
}
{% endhighlight %}

Consider a simple example that is the most common argument for recursive locks: you have an existing method `A` that takes a lock, performs some operations, and then releases the lock. Now you need to write a method `B` that takes the lock, performs some operations (including the operations done by `A`), and then releases the lock. It's natural from a code-reuse perspective to simply have `B` call `A`. Recursive locks permit this kind of code reuse.



And that's a mistake.



When you are reading code, lock acquisition and lock release are semantic barriers. It's natural to assume that invariants hold when the lock is released, but that assumption fails when you are dealing with recursive locks. If `B` calls `A`, then `A` can no longer be sure if the invariants hold when it acquires its lock, and it also cannot be sure if the invariants hold when it releases its lock.



The non-recursive approach would first refactor `A` into a new private method `C` that is clearly documented (often with a naming convention as well) to assume that it is called while under lock. Then both `A` and `B` call `C` while holding the lock.



{% highlight csharp %}public void A()
{
  lock (_mutex)
  {
    C_UnderLock();
  }
}

public void B()
{
  lock (_mutex)
  {
    ...
    C_UnderLock();
    ...
  }
}

private void C_UnderLock()
{
  ...
}
{% endhighlight %}

## Escalating Dependencies

The argument I'm making sounds silly for really simple examples, but it starts to hit home as soon as your program gets more complex. This is because you can no longer fully understand a method in isolation; you have to also consider all the other methods using the same lock that may call it or that it calls (usually all the other methods in the class). In order to ensure the semantics are correct, you now have to hold an entire class in your head instead of a single method.



You end up with "escalating dependencies": every method now depends on the _internals_ of other methods. Each time another method is added, the inter-dependencies have to be considered for _all_ existing methods. The overall complexity grows quadratically.



Consider the `A` and `B` scenario again, this time from a maintenance perspective. If someone is working on `A`, they have to be very careful which invariants they violate while holding their lock, because `B` is depending on some subset of those invariants. If someone is working on `B`, they have to be very careful which invariants they depend on, because any number of methods may be calling that method with only a subset of the invariants holding.



We end up with a problem: each method is no longer responsible for its own correctness. Methods with recursive locks are depending on the internals of other methods. If you use non-recursive locks (by refactoring into `C`), then you still have some method interdependence but it's much reduced. Any method that calls `C` does of course have to depend on its internals (which should be thoroughly documented), but that's all. When you use recursive locking it's a free-for-all: every method depends on the internals of all other methods that have access to the lock. When method `C` needs to change, it's a simple matter to verify that all its callers are still correct. With recursive locking, whenever any method changes, you have to verify all methods it calls (and all the methods they call, etc), as well as all methods that call it (and all the methods that call them, etc).



So, by using a recursive lock, a developer may save himself from writing a method and about 7 lines of code. But in return, he inherits a maintenance problem. Totally not a good tradeoff.



## Schizophrenic Code

Let's try to make our recursive-locking methods all good citizens; that is, any of them can be called while their lock is not held _or_ while it is held. This quickly leads to schizophrenic code in all but the most trivial examples.



Schizophrenic code is code that _does not know_ (until runtime) how it's executing. It adjusts its behavior at runtime depending on whether or not it is already synchronized. The problem is that it's extremely difficult to verify that both behaviors are simultaneously correct. This kind of code is the fastest path to mental instability.



One lesson I have learned through years of multithreaded debugging is this: you should know (at compile-time) where each line of your code is going to execute. This is similar to the old `ISynchronizeInvoke.InvokeRequired` / `Dispatcher.CheckAccess` / `CoreDispatcher.HasThreadAccess` properties: **these should never be used!** Your code should _already know_ where it's executing. It's far easier to maintain and debug code that is not schizophrenic.



## Uncertainty about Lock State

Another problem with recursive locks is that a method can never be sure whether its lock is unlocked. It may already be locked before the method acquires it, and it may still be locked after the method releases it.



One example that is brought up to support recursive locks is a synchronized collection, where the `AddRange` method can call the `Add` method directly. To understand the problem with _uncertainty_, consider the methods under maintenance; we're going to add support for `INotifyCollectionChanged`.



{% highlight csharp %}public void Add(T item)
{
  lock (_mutex)
  {
    ... // add the item
  }
}

public void AddRange(IEnumerable<T> items)
{
  lock (_mutex)
  {
    foreach (var item in items)
      Add(item);
  }
}
{% endhighlight %}

It seems straightforward: `AddRange` will still acquire the lock and add its items (by calling `Add`). `Add` will acquire the lock, add its item, release the lock, and then invoke `CollectionChanged`. There's now a deadlock issue where there wasn't one before - a kind of deadlock that will not be found by unit testing (or most any other kind of testing). Can you see it?



{% highlight csharp %}public void Add(T item)
{
  lock (_mutex)
  {
    ... // add the item
  }
  RaiseNotifyCollectionChanged(item);
}

public void AddRange(IEnumerable<T> items)
{
  lock (_mutex)
  {
    foreach (var item in items)
      Add(item);
  }
}
{% endhighlight %}

The problem? Raising events like `CollectionChanged` is one way to invoke end-user code, and this should never, ever be done while holding a lock. In this case, the `Add` method releases its lock before raising the event, but it _can't be sure_ that the lock is actually _unlocked_. It may be, or it may be not (grrr schizophrenic locks!). And in fact, `AddRange` is in the same boat; it has no idea if it's being called by _another_ method while the lock is held (grrr escalating dependencies!).



So here's a question for you: is the maintainer of the code going to catch this?



The fix is clear: refactor to make your locks non-recursive, but that's only an option **if** the problem is even noticed in the first place. The big question is whether anyone would catch that this change introduced a deadlock condition. Maintenance is often done "a long way away" from the original code (in terms of time and people), _and_ you've got the escalating dependencies issue; both of these together greatly reduce the odds that recursively-locking code will remain correct over time.



## Recursive Other-Things

Another problem with recursive locks is that it doesn't translate well to other coordination primitives that are conceptually related. Take a semaphore for example; semaphores can be used in various ways, but for our example we'll just use them as locks that permit a _certain number_ of acquisitions instead of just one.



The problem with our "multi-lock" is that semaphores don't natively support recursion. If one method acquires the semaphore and calls another method that also acquires the semaphore, then _two_ locks are held rather than one. This may work for a while (while the number of acquisitions on the call stack is short) and then deadlock unexpectedly. Should a "multi-lock" support recursion?



How about recursive reader-writer locks? If there's a limit to the number of simultaneous reader locks, should a recursive reader lock count as multiple reader locks or a single reference-counted reader lock? If a reader lock is acquired and then a writer lock, should it be permitted or should it throw an exception?



Once you start looking at other types of coordination primitives, the semantics of "recursion" become a lot less clear.



## A Fun Case for Condition Variables

Condition variables are not directly exposed in the .NET BCL, unfortunately, so many readers of this blog will not be familiar with the term. Essentially, a condition variable is a coordination primitive that enables a method to first acquire a lock, do some processing, then wait for a _condition_ (releasing the lock while waiting, and re-acquiring it when the waiting is done), do some more processing, and then release the lock.



The .NET `Monitor` class is essentially a lock with a single condition variable, and Wikipedia has a [good description of monitors and condition variables](http://en.wikipedia.org/wiki/Monitor_(synchronization)). Condition variables are also useful on their own; the classic example is a [bounded producer/consumer queue, which is built from one lock and two condition variables]({% post_url 2012-12-20-async-producer-consumer-queue-2-more %}). But for the purposes of this discussion, `Monitor` will do fine.



`Monitor` is a recursive lock, so let's consider the scenario where `B` calls `A`. This time, `A` is going to wait for a condition (e.g., `Monitor.Wait`).



{% highlight csharp %}public void A()
{
  lock (_mutex)
  {
    ...
    Monitor.Wait(_mutex);
    ...
  }
}

public void B()
{
  lock (_mutex)
  {
    ...
    A();
    ...
  }
}
{% endhighlight %}

Now, what happens in that wait? The monitor (condition variable) unlocks the lock during the wait. It can't just _release_ the lock because it's a recursive lock, so it has to unlock it _all the way_. Then, after the wait completes, it re-acquires the lock that many times. This is all documented behavior, and it's the only reasonable way a recursive lock can work with a condition variable.



But what about `B`? As far as it's concerned, it acquires the lock, calls `A`, and then releases the lock. It doesn't even know that when it called `A`, its lock was temporarily released and then re-acquired. _Any other code_ could have run in that time! So that means that `B` has to ensure that all invariants are ready for the lock to be released before it calls `A`, and when `A` returns, it has to re-check _everything_ since any of the state could have changed (not just the state affected by `A`). Even worse, `B` has to know the internals of `A` just to _find out_ that it has to do this!



Blech.



Yeah, that pretty much sums up how I feel about recursive locks: blech.



## The One Use Case

I have to admit that recursive locks get a bad reputation because people use them where they _shouldn't be used_. All the examples so far have been cases where the code is easier to _write_ using recursive locks, but there are _verification_ and _maintenance_ issues that can overwhelm you later. However, there is one use case where recursive locks are OK. In fact, it's the reason recursive locks were invented in the first place.



Recursive locks are useful in recursive algorithms.



Allow me to rephrase that: recursive locks are useful in recursive algorithms with parallel characteristics where fine-grained locking of a shared data structure is required for performance reasons.



In other words: hardly ever.



In my own experience, I've never needed them.



## But Recursive Locks Just Make Sense!

Let's step back and reconsider the definition of recursive locks. At the beginning of this blog post, I stated:



> A "recursive" lock is one that, when locked, will first determine _whether it is already held_


Already held... _by what?_



I purposely left out part of that definition, because I wanted to cover this section last.



One way of thinking is that a lock can be held by a _thread_. Developers use locks to exclude other _threads_. A _thread_ uses locks to keep other _threads_ from interfering with the shared state. All the _threads_ see the shared state mutate atomically.



With this perspective, a recursive lock is perfectly natural; if a _thread_ already has access, then _of course_ it should be able to re-acquire the same lock!



This perspective comes from the many definitions of "lock" that are all written from an operating system perspective. To the OS, that's exactly what the lock does: it blocks other _threads_.



But an experienced multithreaded programmer does not have this perspective at all. We embrace an alternate way of thinking: that a lock can be held by a _block of code_ (at a given level of abstraction). Developers use locks to exclude other _blocks of code_. One _block of code_ uses locks to keep other _blocks of code_ from interfering with the shared state. Etc.



With this perspective, a recursive lock makes no sense at all. The fact that you're considering a recursive lock indicates that you're using the same lock at two different levels of abstraction in your code (or what _should be_ two different levels of abstraction).



Like many developers, I was taught the classical (thread) definition in school, and my first multithreaded programs used coarse-grained locking to coordinate threads. After a few years of minimizing the code under lock and restricting lock visibility, I just sort of gravitated to the alternate (block of code) definition.



In fact, you _have_ to embrace the alternate definition in order to consider new types of locks such as [asynchronous locks](http://blogs.msdn.com/b/pfxteam/archive/2012/02/12/10266988.aspx), which _cannot_ be tied to a thread.



## Conclusion

Well, I originally meant this all to be a lead-in to asynchronous recursive locks, but I ended up too long-winded. I'll have to talk about asynchronous recursive locks another time. :)

