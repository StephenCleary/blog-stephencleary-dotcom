---
layout: post
title: "Asynchronous Callback Contexts"
---
One major - and often overlooked - issue when designing asynchronous components is the difficulty of cancellation, particularly during object disposal.

## The Problem

End-users do not expect components to raise events after they have been disposed. It is natural to assume that after an object has been disposed of, it will not raise an event some time in the future. Likewise, if the component has repeating events and supports cancellation, it is reasonable to assume that the events will stop firing after the component has been "cancelled". However, implementing this expected behavior takes some forethought.

When asynchronous components raise events, these events are generally either queued to an "originating" thread or queued to be executed by the ThreadPool. Usually, it is a simple matter to cancel an upcoming event as long as it is not yet queued; the trick comes in how to handle cancelled events that have already been queued.

## The Solution

The answer is to define some sort of "context". When the component queues an event, it copies the current value of the context, and when the event is actually processed, it first checks its value of the context against the component's current context value. If they match, then the event knows it is safe to continue; if they don't match, then the event knows it has been cancelled. The component then just changes its context value whenever it is cancelled or disposed.

The .NET framework provides an excellent choice for contexts: object. Objects are compared using fast reference equality and they are fast to allocate and deallocate. A component includes an object reference as its context, and allocates a new one when it is cancelled or disposed. Earlier versions of Nito.Async asynchronous components used exactly this method.

## Callback Contexts in the Real World

Fire up Reflector and take a look at System.Timers.Timer in System.dll (2.0.0.0). It has a private field of type object named "cookie". When the timer is enabled, it allocates a new object, saves it into "cookie", and passes it as the state object to the underlying System.Threading.Timer callback. The underlying timer callback (in MyTimerCallback) compares the state object to the current value of cookie, and doesn't proceed with the event if they don't match.

System.Timers.Timer does not change "cookie" when it is disposed because the underlying System.Threading.Timer will not invoke its TimerCallback after it has been disposed. At first glance this appears correct, but this is actually a race condition bug because there is a possibility of System.Timers.Timer.Elapsed being invoked while another thread is executing System.Timers.Timer.Close. If SynchronizingObject is non-null, MyTimerCallback may queue the callback to the thread executing Dispose, resulting in a situation where an event (Elapsed) is fired after the object has been disposed.

## Reusable CallbackContext Type

One of the new classes in version 1.2 of the [Nito Async](http://nitoasync.codeplex.com/) library is a reusable CallbackContext type. This class encapsulates all the semantics necessary, and introduces a few new terms:

- A delegate may be _bound_ to a CallbackContext. Binding a delegate results in a new delegate (the _bound delegate_) - which wraps the original delegate.
- Every bound delegate is either _valid_ or _invalid_. When a valid delegate executes, it will execute its wrapped delegate; when an invalid delegate executes, it will do nothing.

Delegates are bound to the CallbackContext by calling CallbackContext.Bind; delegates are valid when they are bound. A CallbackContext will invalidate all of its bound delegates when CallbackContext.Reset is called.

To use CallbackContext from an asynchronous component, bind each delegate that needs to check the context. Then call CallbackContext.Reset when the operation is cancelled. CallbackContext also derives from IDisposable and implements Dispose (as a synonym for Reset) to remind users to call CallbackContext.Dispose when the asynchronous component is disposed.

The only other note regarding CallbackContext is that the delegates should be synchronized (using SynchronizingObject or SynchronizationContext) before the bound delegate is invoked. We are considering adding overloads to CallbackContext to allow for synchronization and binding in a single step, ensuring the correct order; if they are added, they will be included in [Nito.Async](http://nitoasync.codeplex.com/) version 1.3.

