---
layout: post
title: "A Cross-Domain Singleton"
---
In my current position, I've had to learn a lot more details about two big aspects of the .NET runtime: AppDomains and COM interop. Until about a year ago, I had learned exactly enough of those technologies to pass the Microsoft certification exams, and that was it! I had never used them in production and never intended doing so. At my current job, however, I have no choice, so I've been learning quite a bit about AppDomains and COM interop over the last few months.

On a side note: blech. I wish I could have remained ignorant. :)

Anyway, the way our product uses AppDomains, it would benefit from a "cross-AppDomain singleton" for certain lookup operations (including cache). I spent some time playing with this idea over Christmas break, and Googled up several implementations. None of them were quite complete, though; many punted on thread safety, which was an absolute necessity for my scenario.

So, I wrote my own. Initially I borrowed heavily from other AppDomain-aware singletons, until I had a minor epiphany. Since this was a true application-level singleton, it would never be destroyed, and the correct place to initialize it is _in the default AppDomain_. The "default AppDomain" is the first one in a process, and it [can never be unloaded](http://blogs.msdn.com/b/cbrumme/archive/2003/06/01/51466.aspx). All non-default AppDomains then request the instance from the default AppDomain. Those other AppDomains can come and go, but the default AppDomain (including all cross-AppDomain singletons) would remain.

Once I decided to assign all singletons to the default AppDomain, the implementation simplified significantly. The algorithm is different based on whether an instance is requested from the default or a non-default AppDomain.

When an instance is requested on a non-default AppDomain, it will first check to see if there is a local, cached copy in the current AppDomain. If there is, then it is returned immediately. Otherwise, it will attempt to get the instance from an AppDomain value stored on the default AppDomain. If that value is not found, then it invokes a method on the default AppDomain that just requests the instance.

When an instance is requested on the default AppDomain, it will first check to see if the instance has been created, and return it immediately if so. Otherwise, it will create a new instance and set that instance as an AppDomain value on the default AppDomain, and then return the instance.

This implementation is fully threadsafe, using Lazy\<T> for all lazy construction. The only drawback to this solution is that it does use a tiny bit of COM interop to a deprecated interface (ICorRuntimeHost); if anyone knows of a better way to get the default AppDomain, I'm all ears!

Also, I cheated just a little bit to simplify lifetime management. By default, remote proxies will time out if you don't use them for 10 minutes, and this is no good since my singleton type caches the proxies locally for each AppDomain. So, my singleton actually creates a _wrapper_ around the instance, and caches proxies to that wrapper (and the wrapper proxies never expire). However, this means that each time the code accesses the singleton instance, a new proxy is actually created and returned - so it's ideal for the occasional-access scenario but not so much for the constant-access scenario. If the proxy creation slows you down too much, then you can use the CachedInstance property instead, which will cache the actual (unwrapped) instance; and in that case the responsibility falls back on you to properly handle proxy lifetimes.

The full public API is quite simple:

{% highlight csharp %}
namespace DomainAwareSingleton
{
    // A domain-aware singleton. Only one instance of T will exist, belonging to the default AppDomain. All members of this type are threadsafe.
    public static class Singleton<T> where T : MarshalByRefObject, new()
    {
        // Gets the process-wide instance.
        // If the current domain is not the default AppDomain, this property returns a new proxy to the actual instance.
        public static T Instance { get; }

        // Gets the process-wide instance.
        // If the current domain is not the default AppDomain, this property returns a cached proxy to the actual instance.
        // It is your responsibility to ensure that the cached proxy does not time out; if you don't know what this means, use Instance instead.
        public static T CachedInstance { get; }
    }
}
{% endhighlight %}

And the [code is on GitHub](https://github.com/StephenCleary/CrossDomainSingleton/blob/master/Source/DomainAwareSingleton/Singleton.cs).

