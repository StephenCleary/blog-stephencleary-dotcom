---
layout: post
title: "C# Advent: The Joy of Immutable Update Patterns"
description: "Reviews several changes to the language over the years that (taken together) provide joyful techniques to update immutable data."
---

This is my first-ever post that is part of [C# Advent](https://csadvent.christmas/) organized by [@mgroves](https://x.com/mgroves). This year there's a [video](https://www.youtube.com/watch?v=D4udjhRjW4o), too, including yours-truly singing while wearing my favorite Christmas shirt!

## Joy to the World!

<blockquote class="blockquote" markdown="1">
Glory to God in the highest, and on earth peace, good will toward men.

<footer class="blockquote-footer text-right"><cite>Luke 2:14</cite></footer>
</blockquote>

I love Christmas! It's easily my favorite holiday.

In spite of difficulties and upheaval in the world (it is 2023 right now), Christmas still stands as a time of refection and remembrance and expectation.

I do approach Christmas from a Christian perspective, and I enjoy meditating on Jesus' birth during this time. In particular this year, I've been focusing on _peace_ and _joy_, two words commonly associated with Christmas and the coming of the Christ.

So, when I was considering the topic for my C# Advent article, I particularly wanted one that invoked Peace or Joy. And, when working with C#, there is one aspect of the language that truly does cause feelings of joy whenever I use it. It's not a single language feature, but rather a collection of language features that all work together in a beatiful way.

Hence the title of this blog post: The Joy of Immutable Update Patterns. Wow, that sounds nerdy...

## Immutability

An immutable type is one whose value can't change. Immutable types have several advantages, not the least of which is that they're just easier to reason about. Some languages push immutability very strongly; C# takes a relatively pragmatic approach.

Immutability varies across the C# ecosystem (and it's currently seeing a gradual rise in popularity). Most value types are usually immutable (e.g., `int`, `decimal`, and `Guid`); most reference types are not immutable (e.g., `List<int>`). However, there are lots of exceptions to that general rule; mutable value types are common in performance-sensitive scenarios, and some reference types such as `string` are immutable.

Modern code has a few additional options for immutable types: you can use `record class` (C# 9) for immutable reference types and `readonly record struct` (C# 10) for immutable value types. There's also the collections in `System.Collections.Immutable` for more complex data structures such as stacks, queues, and dictionaries. Of course, with your own immutable types and collections, their members/elements must be immutable in order for the composite value to be immutable.

## Updating Immutable Data

Immutable data makes local functions easier to reason about - you _know_ the data can't change - but of course every program has to model modifications in some way. One approach is to have a _variable_ that is mutable, referring to some _data_ that is immutable. To change the immutable data, you can write code that transitions one immutable value to another.

It is in this area that C# has been slowly adding enhancements over many years, and is now approaching beautiful code. Code that makes me smile when I write it!

### `switch` Expressions

`switch` expressions (C# 8) are at the core of immutable update patterns. At their simplest, they provide a mapping from one constant to another:

{% highlight csharp %}
static Category Map(AdventThing t) => t switch
{
  AdventThing.Mary => Category.Person,
  AdventThing.Sheep => Category.Animal,
  AdventThing.Camel => Category.Animal,
  AdventThing.Bethlehem => Category.Place,
  _ => throw new ArgumentOutOfRangeException(nameof(t)),
};
{% endhighlight %}

`switch` expressions are built on [pattern matching](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/operators/patterns?WT.mc_id=DT-MVP-5000058), which started in [C# 8](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/proposals/csharp-8.0/patterns?WT.mc_id=DT-MVP-5000058) (alongside the `switch` expression) and have received enhancements in [C# 9](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/proposals/csharp-9.0/patterns3?WT.mc_id=DT-MVP-5000058), [C# 10](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/proposals/csharp-10.0/extended-property-patterns?WT.mc_id=DT-MVP-5000058), and [C# 11](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/proposals/csharp-11.0/list-patterns?WT.mc_id=DT-MVP-5000058). They're practically a separate mini-language at this point!

Switch expressions on their own are powerful, but they're especially useful alongside `with` expressions.

### `with` Expressions

`with` expressions are a shorthand way of copying a composite value (i.e., a `record class` or `readonly record struct`) and changing only the specified properties. A simple example should suffice:

{% highlight csharp %}
record class Inn(string Name, int RoomsAvailable);
Inn Full(Inn inn) => inn with
{
  RoomsAvailable = 0,
};

Inn myInn = new("Bethlehem Getaway", 50);
Inn fullInn = Full(myInn);
{% endhighlight %}

In the example above, `Full` does not modify the (immutable) inn; it returns a _new_ inn that is full.

Combining `switch` expressions with `with` expressions is where you start to see the beauty of this kind of immutable update pattern:

{% highlight csharp %}
Inn ReserveRoom(Inn inn) => inn switch
{
  { RoomsAvailable: >0 } => inn with { RoomsAvailable = inn.RoomsAvailable - 1 },
  _ => throw new InvalidOperationException("No rooms available."),
};
{% endhighlight %}

Here's a method that decrements the available rooms in an `Inn`. It is a _pure_ method; it depends only on its inputs and produces only its outputs, with no mutation. This is the point at which we're starting to do immutable state transitions.

### Collections

I tend to use `System.Collections.Immutable` whenever I need something stack- or queue- or dictionary-like in an immutable context. These types all have methods like `Add` that _return_ a new collection rather than modifying one in place. Internally, the immutable collections share internal data structures, so this isn't as inefficient as copying the entire collection; immutable collections can never be as memory-efficient as immutable collections, but they're usually efficient enough to not be an issue. I find immutable collections satisfy my needs quite well.

However, I would be remiss if I didn't mention that C# has added a new way to create collections (including `ImmutableArray<T>`). It's very reminiscient of JavaScript's [spread operator](https://developer.mozilla.org/en-US/docs/Web/JavaScript/Reference/Operators/Spread_syntax):

{% highlight csharp %}
ImmutableArray<int> list = ImmutableArray.Create(3, 5, 6, 11, 13);
int index = 2;

ImmutableArray<int> result = [..list[..index], 7, ..list[^index..]];
// result: [3, 5, 7, 11, 13]
// Equivalent to:
//   ImmutableArray<int> result = list.SetItem(index, 7);
{% endhighlight %}

As of this writing, though, the implementation iterates over all the elements and builds an entirely new collection. This is quite inefficient for immutable collections, so I do not use C# 12's [collection expressions](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/operators/collection-expressions?WT.mc_id=DT-MVP-5000058) when working with immutable data. (But for mutable code, they rock!)

## Application: Unidirectional Data Flow

Let's build this up into something a bit more complex! We can give each inn an actual collection of rooms, and just acquire an available one when requested.

{% highlight csharp %}
record class Room(int Id, bool Available);
record class Inn(string Name, ImmutableHashSet<Room> Rooms);

Inn ReserveRoom(Inn inn)
{
  var room = inn.Rooms.FirstOrDefault(r => r.Available)
      ?? throw new InvalidOperationException("No rooms available.");
  return inn with
  {
    Rooms = inn.Rooms.Remove(room).Add(room with { Available = false }),
  };
}

Inn myInn = new("Bethlehem Getaway",
    Enumerable.Range(0, 50)
    .Select(x => new Room(x + 100, Available: true))
    .ToImmutableHashSet());
Inn resultInn = ReserveRoom(myInn);
{% endhighlight %}

Now we have some more complex state, and our `ReserveRoom` method now finds a room, reserves it, and returns the new composite state of the inn. I often find it useful to have these modifier methods also return some indication of what they did - in this case, it can return the room that was reserved. Tuples are convenient for multiple return values:

{% highlight csharp %}
(Inn Inn, int RoomId) ReserveRoom(Inn inn)
{
  var room = inn.Rooms.FirstOrDefault(r => r.Available)
      ?? throw new InvalidOperationException("No rooms available.");
  return (
      inn with
      {
        Rooms = inn.Rooms.Remove(room).Add(room with { Available = false }),
      },
      room.Id
  );
}
{% endhighlight %}

If you squint a bit, you can see `ReserveRoom` as being like a Redux reducer (for a single action: reserving a room). Many years ago, React and Redux took the world by storm. Although Redux has fallen out of favor in some circles, it made some ideas popular, and those continue to live on.

Specifically, the idea of Unidirectional Data Flow is one that has taken hold, particularly in UI applications. The core idea is that the application has a single instance of composite immutable state, and that this state is only changed by applying pure functions to it (commonly called "reducers"). Other parts of the application (including the UI) listen for and respond to state changes. UDF is an architecture that is overkill for extremely simple applications, but is an absolute lifesaver when there is significant complexity.

Unidirectional Data Flow (UDF) can go by several names. Model-View-Intent (MVI) is an architecture common on mobile platforms that is based on UDF. Another fairly common architecture name is The Elm Architecture (TEA, or sometimes just Elm). Today most C# UI applications still use a basic MVVM style of architecture, but I expect with the language changes that better support immutable update patterns, we'll start to see more adoption of UDF architectures in C#. At least, I hope so!

## Conclusion

I hope this post has been interesting to you! Personally, I do enjoy writing code combining `switch` and `with` expressions. I think the resulting code is really elegant, and I hope you got some joy out of this! Merry Christmas!

<!--
## Application: Building Asynchronous Primitives

And now I'm going to completely switch gears. Because immutable state updates are great for application state, but they're also great for doing threadsafe code.

One advantage of immutable data is that - since it is immutable - it can be safely shared among any threads! There's usually just one variable that _refers_ to the immutable state, and that variable is the only thing that needs actual multithreaded protection. So, let's use this aspect to improve on a well-known primitive.

I have an "Async Masterclass" talk that I've given a few times, and in that talk one of the topics I cover is building your own asynchronous synchronization primitives. A simple example is an `AsyncManualResetEvent`, which in my (current) slides ends up looking like this:

{% highlight csharp %}
sealed class AsyncManualResetEvent
{
	private object _mutex;
	private TaskCompletionSource _tcs;
	
	public AsyncManualResetEvent()
	{
		_mutex = new();
		_tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
	}

	public Task WaitUntilSetAsync()
	{
		lock (_mutex)
		{
			return _tcs.Task;
		}
	}

	public void Set()
	{
		lock (_mutex)
		{
			_tcs.TrySetResult();
		}
	}
	
	public void Reset()
	{
		lock (_mutex)
		{
			if (_tcs.Task.IsCompleted)
				_tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
		}
	}
}
{% endhighlight %}

And, sure, there's nothing really _wrong_ with this, but there's some parts that aren't clear to many developers. How can I use `lock` in this asynchronous primitive? And `RunContinuationsAsynchronously` is necessary to avoid a particularly tricky deadlock situation. It's just not code that is 

-->