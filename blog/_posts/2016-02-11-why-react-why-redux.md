---
layout: post
title: "React/Redux TodoMVC, Part 0B: Why React? Why Redux?"
series: "React/Redux TodoMVC"
seriesTitle: "Why React? Why Redux?"
description: "Why I chose React and Redux."
---

I was a casualty of the Version 4 Browser Wars.

Those of you who are old enough to remember know exactly what I'm talking about. For those who weren't there, maybe I'll write a post about it someday. But anyway, I became a desktop developer for many years as a direct result of that horrible early web experience. It is only recently (the last few years) that I've cautiously stepped out into frontend development again.

I say this to emphasize that I'm not an expert frontend developer (yet). I've dabbled in Knockout, Angular, Durandal, and Aurelia. I'm much more familiar with common libraries like lodash and jQuery. But when it came to choose a framework for [DotNetApis](http://dotnetapis.com), I chose something I had never used before: React.

## Why React?

Well, in short, because [Cory House](http://www.bitnative.com/) convinced me. I was able to attend his excellent talk at ThatConference 2015 [Angular, Backbone, and Knockout are great, so why choose React?](https://www.thatconference.com/sessions/session/9108), and followed that up with [his Pluralsight course on React and Flux](https://www.pluralsight.com/courses/react-flux-building-applications). Cory House is convincing. :)

It all boils down to this simple fact: React represents component boundaries as files. In other words, a single component belongs in a single file.

In a modern MVVM-style data-binding system, you usually end up with *pairs* of files. You have the view and the view model:

    app/
      components/
        todo/
          todoViewModel.js
          todoView.html
        footer/
          footerViewModel.js
          footerView.html

I've written a few components like this, enough to know something's wrong. In particular, when you have this kind of structure in your app and you need to modify a component, which file do you end up modifying? Usually both of them. New functionality generally requires *both* the view and view model to change. This is an indication that the view and view model are really dealing with the same underlying concern.

According to Wikipedia, the Single Responsibility Principle is "every module or class should have responsibility over a single part of the functionality provided by the software, and that responsibility should be entirely encapsulated by the class." Take special note of the second half of that definition: "that responsibility should be entirely encapsulated". That's why it's harder to have multiple files making up a single component. There's a lack of cohesion.

Another hurdle with modern MVVM-style data-binding systems is that they use templating. This means that you have *three* languages to deal with: JavaScript (or some variant) for the view model, HTML for most of the view, and whatever HTML extensions exist as part of the templating solution. That's a fair amount of context switching.

None of these hurdles are insurmountable, of course. Large systems have been written with these techniques, and they work well. But we can do better.

React addresses these problems by restructuring a "component". Instead of having two files, with one of them containing a programming (templating) language within HTML, React just reverses that and puts HTML in the JavaScript.

    app/
      components/
        todo.jsx
        footer.jsx

This seems like a small thing, but it really does make a difference in day-to-day work. One component is in one file, which contains both the HTML and logic. When a component changes, only one file changes. And there's only two languages: JavaScript (or some variant) and HTML. There's no separate templating language; if you need a `for` loop around some content, you just write it in JavaScript.

React *simplifies* component work. Then it gets out of your way.

## My First Attempt (Flux, not Redux)

React only addresses the "view" part of the frontend. It doesn't have any opinions about how you store your data. There's a pretty common pattern of data management called [Flux](https://facebook.github.io/flux/).

When I started using React, I decided to go with "vanilla Flux"; that is, to have a singleton dispatcher implementation. Flux works like this:

{:.center}
![]({{ site_url }}/assets/FluxAsPromised.png)

You have "action creators", which are just functions that return actions. Those actions are data (objects) that represent some action, such as a user clicking a button or typing into a textbox. The actions are sent to the dispatcher, which is a singleton. The dispatcher then sends out the action to each store that has registered with it (this is just a simple pub/sub pattern).

The stores are where the application state lives. Each store updates itself by responding to actions coming from the dispatcher. Each time it updates, it notifies *its* subscribers, which are the views (the actual JSX components). Those views in turn will respond to user interaction by calling the action creators and dispatching those actions.

This diagram is a bit simplified; the "Views" here are really made up of two different kind of views: ["smart" or "page" or "container" or "route"](https://medium.com/@dan_abramov/smart-and-dumb-components-7ca2f9a7c7d0#.o5lr2g6lz) views, which sit at the top level and subscribe to store updates, and "dumb" views, which only receive data from their parent views.

This is all well and good, but as DotNetApis grew more complex, I started running into problems.

In particular, I had several different "stores", each one containing part of the application state, divided up logically. Also, I had several different "smart" views, some of which were children of other "smart" views, and each of which had to manage subscriptions to one or more stores. What I ended up with looked more like this diagram:

{:.center}
![]({{ site_url }}/assets/FluxInReality.png)

It was just getting too complex. I reached out for help on The Twitter, and [Cory House](https://twitter.com/housecor) and [Ryan Lanciaux](https://twitter.com/ryanlanciaux) were kind enough to point me to Redux.

## Why Redux?

[Redux](https://rackt.org/redux/) is a further simplification of React. Redux declares that you should only have one store, and all of the application state lives in that single store.

Hmmm, simpler:

{:.center}
![]({{ site_url }}/assets/ReduxToTheRescue.png)

Now, Redux *does* introduce some new concepts. In short, it implements your store *for* you, so you need to fill in a few missing pieces.

In particular, you need to define how actions will change your application state. In Redux, these are called [reducers](https://rackt.org/redux/docs/basics/Reducers.html).

A real-world Redux application ends up looking more like this (note that the shaded parts are mostly or entirely implemented by libraries, not by the application author):

{:.center}
![]({{ site_url }}/assets/ReduxInReality.png)

Once I started using Redux, I've never looked back. Everything is simpler!

The source hasn't changed since last time, but if you missed it:

[Source code at this revision](https://github.com/StephenCleary/todomvc-react-redux/tree/9b881b0bea8070f850c8c78a6fcf4701287101ae) - [Live site at this revision](http://htmlpreview.github.io/?https://github.com/StephenCleary/todomvc-react-redux/blob/9b881b0bea8070f850c8c78a6fcf4701287101ae/index.html) (ignore the "startup flicker"; that's just due to the way it's hosted)

[Most current source code](https://github.com/StephenCleary/todomvc-react-redux) - [Most current live site](http://stephencleary.github.io/todomvc-react-redux/)
