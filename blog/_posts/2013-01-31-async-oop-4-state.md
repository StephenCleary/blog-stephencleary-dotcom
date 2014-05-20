---
layout: post
title: "Async OOP 4: State"
series: "Async OOP"
seriesTitle: "State"
---
It's been said that OOP is essentially the combining of _behavior_ and _state_. Asynchronous code has no problem expressing _behavior_; it is functional in nature, after all. When you design asynchronous objects, most of the problems that arise are due to _state_.

## Exposed State

As we discussed last week, properties should represent the _current state_ of an object. This can cause a problem regarding semantics: what is the "current state" of an object that has an asynchronous operation in progress?

One familiar example of this problem is `Stream.Position`, which represents the current offset of the stream "pointer". When you call `Read` or `Write`, the actual reading/writing is done and `Position` is updated to reflect the new position, all before the `Read` or `Write` method returns.

Now, consider `ReadAsync` and `WriteAsync`: when is `Position` updated? When the reading/writing is complete, or before it actually happens? If it happens before, is it updated synchronously or could it happen after the actual `ReadAsync` or `WriteAsync` method returns?

> As a side note, this problem affects all asynchronous code, not just `async` code. The same questions about `ReadAsync` can be asked about `BeginRead`.

This is a great example of how a property that _exposes state_ has perfectly clear semantics for synchronous code, but no obviously correct semantics for asynchronous code. It's not the end of the world - you just need to think about your entire API when `async`-enabling your types, and _document the semantics you choose_.

## Hidden State

Hidden state is state that can impact asynchronous operations, even if that state isn't exposed through properties. `Stream.Position` only causes problems if the code needs to use that property; hidden state impacts other asynchronous operations.

A prominent example of hidden state is in `HttpWebRequest`, which can only perform one HTTP request at a time. There is no indication that a request is in progress; if you attempt to start a second asynchronous request, you'll receive an `InvalidOperationException`.

`HttpClient` improves the situation; a single `HttpClient` instance supports multiple simultaneous HTTP requests. There are still a few state-related problems (e.g., you can't set `HttpClient.Timeout` once a request is in progress), but it does reduce the impact of hidden state when compared to `HttpWebRequest`.

I am generally against hidden state from an API design perspective, but implementation is often easier if you do allow hidden state. In these cases, I prefer to have both a lower-level type (like `HttpWebRequest`) with well-documented hidden state and then have a higher-level type (like `HttpClient`) without hidden state.

## Output State

There's another kind of "state" that should be addressed: when _changes_ to state are treated as _output_ and used to update other state (usually including the UI, if you're using MVVM or a similar system). In this case, you have data-bound properties that notify listeners when they change.

I don't have much to add to [last week's discussion of data-bound properties]({% post_url 2013-01-24-async-oop-3-properties %}). Just keep in mind that updates should be done on the UI context, and that properties should always represent the _current state_. So you may need an "indeterminate" or "unknown" value to start with (e.g., `null`), which can be asynchronously updated.

