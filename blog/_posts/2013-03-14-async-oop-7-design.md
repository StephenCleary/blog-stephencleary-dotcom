---
layout: post
title: "Async OOP 7: Design"
series: "Async OOP"
seriesTitle: "Async OOP 7: Design"
---
I'd like to conclude this [little Async/OOP series]({ % post_url TODO % }) by circling back around to _design_. Most of the Async/OOP series has been looking at best practices in real-world situations; today's post is a bit more theoretical and definitely more controversial. :)



I started out the [introductory post]({% post_url 2013-01-03-async-oop-0-introduction %}) talking about functional programming. All of the problems that we've had to solve in the Async/OOP series are due to asynchronous code being naturally functional, and how to get that to play well with OOP design (and in a mostly-OOP language). Today I'm going to step away from OOP and think about how `async` C# code would look if we embraced the functional nature of asynchronous code.



Personally, I have written some asynchronous components that are functional rather than OOP. This does not make sense all the time, though. I find that if the _process_ dominates your design, then a functional implementation is well-suited. OTOH, if your _state_ dominates your design, then OOP would be a much better choice. Also, most programmers are well-versed in OOP but are leery of (or not familiar with) functional programming.



Without further ado... Functional programming in C# is most naturally expressed in static methods of static classes. You could argue that static classes are just workarounds for the lack of freestanding functions, but I find them useful as containers. Since all your core methods will be static, they could in theory go in any static class. To keep the code organized, I find it helpful to group methods by _purpose_. This is not the same as grouping them by _type_ (in an OOP design).



You can pass state and data around in a functional program. So each functional method can take state as arguments and produce state as output. It's useful to have these state representations be immutable (and, strictly speaking, a pure functional program would _always_ use immutable types).



If we allow in just a bit of OOP, we can get some interesting benefits. Instead of using a static class, define an interface with a concrete implementation. It's still functional programming, because every method in that implementation does not change any state in its instance; if it weren't for the interface, those methods could be static methods. This gives us a layer of abstraction for a section of our functional code; you can use this for mocking, etc. This is where the "grouping by purpose" really helps.



That's all I have to say today: just some semi-random ramblings about functional design. Which I don't have much experience in, so take this with a hefty grain of salt! :)



As a closing note, I'll point out that the functional nature of `async` methods really helps them work well as extension methods. Many types can be made `async`-friendly by nothing more than a few (functional) extension methods in a static class. If that's all you need to do, then that's all you _should_ do!


