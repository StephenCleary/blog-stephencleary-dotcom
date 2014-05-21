---
layout: post
title: "A Few Words on Task.Id (and TaskScheduler.Id)"
---
There are some `Id` properties in TPL types (notably `Task.Id` and `TaskScheduler.Id`); these "identifiers" follow the same pattern. I believe their primary use case is for [ETW events](http://msdn.microsoft.com/en-us/library/ee517329.aspx), though they may have other uses.

## Generated On-Demand

Identifiers are generated on-demand. So if you don't read the properties and don't have ETW tracing on, then your tasks (and task schedulers) won't actually _have_ identifiers. They generate them right when they need them.

## Invalid/Unassigned Value

The value `0` is never used. This is technically undocumented, but it's pretty safe to assume. The ETW events all produce plain `int`s for task and task scheduler identifiers, and [some ETW events](http://msdn.microsoft.com/en-us/library/ee517329.aspx) need a value for "none" (e.g., `OriginatingTaskId` needs to support a value meaning "there was no originating task").

This means you'll never actually see a zero value as an identifier. A task (or task scheduler) can internally have a zero identifier (meaning "unassigned") but will generate an actual identifier if that value is ever read.

Incidentally, `Task.CurrentId` is a bit different than `TaskScheduler.Current.Id`. `Task.CurrentId` will return `null` when there is no task currently executing. `TaskScheduler.Current.Id` will return the (real) identifier of the "current" `TaskScheduler`; if there's no task executing, the "current" scheduler is the default (thread pool) scheduler.

But either way, you won't see a zero value.

## Per-Type

Identifiers have meaning only for a particular type. For example, the first assigned `Task` identifier is one, and the first assigned `TaskScheduler` identifier is one. So the identifier "one" has no meaning by itself; the identifiers are not allocated from a shared pool or anything like that.

## Not Quite Unique

The "identifiers" are not unique. They're pretty close (they'll repeat very rarely), but they're not actually _unique_.

> The MSDN documentation states the identifiers are unique. The MSDN documentation is wrong.

This can be easily proven with a simple test (also on [gist](https://gist.github.com/StephenCleary/5108676)) where we first create one `Task` and then repeatedly create additional `Task` instances until we find one where the identifiers are the same (though the task instances are different):

{% highlight csharp %}
using System;
using System.Threading.Tasks;

class Program
{
    static void Main(string[] args)
    {
        var task = new TaskCompletionSource<object>(null).Task;
        var taskId = task.Id;

        Task other;
        do
        {
            other = new TaskCompletionSource<object>(null).Task;
            if (other.Id == 0)
                Console.WriteLine("Saw Id of 0!");
        } while (other.Id != task.Id);

       Console.WriteLine("Id collision!");
       Console.WriteLine("  task.Id == other.Id: " + (task.Id == other.Id));
       Console.WriteLine("  task == other: " + (task == other));
       Console.ReadKey();
    }
}
{% endhighlight %}

This program takes about 3 minutes on my machine to observe an identifier collision. The output is:

    Id collision!
      task.Id == other.Id: True
      task == other: False

Note that `Saw Id of 0!` is _not_ in the output; the task identifiers worked their way through all possible `int` values but skipped over zero.

Probably no one will write a program that has four billion `Task` instances simultaneously, but it's not uncommon for a few `Task` instances to be long-lived and most of them short-lived. So if you have a long-lived `Task` instance in a long-running program, be aware that its identifier may be reused while the long-lived task is still alive. Note that this _is_ the common case! The example program above illustrates this: it only has one long-lived task; all the other tasks are eligible for garbage collection shortly after they're created.

So, be aware that identifiers are not strictly unique. Some developers have attempted to "attach" data to a task using a concurrent dictionary with task identifiers as the key. But this will not work for most long-running programs.

> When developers try to attach data to tasks, they're usually trying to figure out some kind of "ambient context" for asynchronous operations. We'll cover the correct way to do that in a few weeks. If you really, seriously do need to attach data to tasks and you can't derive from `Task` for whatever reason, you can use [Connected Properties](http://connectedproperties.codeplex.com/).

## Identifiers in Nito.AsyncEx

I have a number of types in my [AsyncEx library](http://nitoasyncex.codeplex.com) where I need a similar sort of semi-unique identifier (primarily for logging purposes). So I follow the same pattern as the built-in framework identifiers: generated on demand, zero as an invalid/unassigned value, and allocated by-type. I use a [helper class called IdManager](http://nitoasyncex.codeplex.com/SourceControl/changeset/view/f74db7311ea1#Source/Nito.AsyncEx (NET4, Win8, SL4, WP75)/Internal/IdManager.cs) (not exposed in the public API) to satisfy this pattern.

You're welcome to use this type in your own code if you need to. The design may appear a little unusual to .NET developers because it uses a _generic tag type_. Conceptually, `IdManager<Tag>` actually defines a _set_ of types, each with their own "namespace" for identifiers. The generic parameter `Tag` is completely unused by `IdManager<Tag>`; its only purpose is to partition the static members.

This is a common code pattern in C++, a language which has much greater support for generic programming than C#. This kind of pattern is not at all common in the .NET world, and in fact StyleCop will complain about this class. But it makes perfect sense if you think about generic arguments as actual _arguments_ that you pass to the _type_.

