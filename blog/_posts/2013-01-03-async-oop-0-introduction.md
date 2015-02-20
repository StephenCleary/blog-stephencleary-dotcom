---
layout: post
title: "Async OOP 0: Introduction"
series: "Async OOP"
seriesTitle: "Introduction"
---
As programmers adopt `async`, eventually they come across problems trying to fit asynchronous code into OOP (_classical_ OOP, that is). The root cause of this is that asynchronous code is naturally functional. And by "functional", I don't mean "working". I mean the style of programming where _behavior_ is paramount and _state_ is minimized.

This is the first in a series of posts that will examine combining `async` with OOP.

## Async is Functional

Historically, `async` can trace its roots (or at least one major root) back to F#'s asynchronous workflows, but this is not what is dictating the functional nature of `async`.

_All_ asynchronous code is functional by nature. I used to teach asynchronous programming (in C++, then in C#, well before `async`), and one of the key pieces of advice for writing asynchronous code is: "you have to turn your mind inside out." That's regular-person-speak for "think functionally, not procedurally."

The major breakthrough with `async` is that you can still think procedurally while programming asynchronously. This makes asynchronous methods easier to write and understand. However, under the covers, asynchronous code is still functional in nature; and this causes some problems when people try to force `async` methods into classical OOP designs.

<div class="alert alert-info" markdown="1">
<i class="fa fa-hand-o-right fa-2x pull-left"></i>

Update (2014-12-01): For more details, see Chapter 10 in my [Concurrency Cookbook]({{ '/book/' | prepend: site.url_www }}){:.alert-link}.
</div>
