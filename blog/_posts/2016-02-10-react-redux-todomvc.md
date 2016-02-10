---
layout: post
title: "React/Redux TodoMVC, Part 0: Introduction"
series: "React/Redux TodoMVC"
seriesTitle: "Introduction"
description: "Getting started with react and redux."
---

As of this writing, the front end of [DotNetApis](http://dotnetapis.com) is entirely written in [React](https://facebook.github.io/react/). This was my first React project, and there were a *lot* of things that I learned along the way. My app certainly had "growing pains," and a lot of React best practices were added rather late in that project.

So, I thought it would be beneficial to do a series of posts on how to do a new React project, with the benefit of hindsight that I did not have when writing the first version of DotNetApis.

<div class="alert alert-info" markdown="1">
<i class="fa fa-hand-o-right fa-2x pull-left"></i>

Disclaimer: I am still very new to React. It's <s>possible</s> likely that I am entirely missing some really helpful best practices. If you know of any, please let me know in the comments!

Disclaimer Part 2: In fact, the whole React community is still new to React. Best practices are still "in flux" (heh), and may not be the same years from now.
</div>

I'll be developing this project in the open [on GitHub](https://github.com/StephenCleary/todomvc-react-redux). I've decided to implement the ubiquitous [TodoMVC app](http://todomvc.com/), which is often used to help decide on client-side MVC frameworks. You can argue that React/Redux isn't really "MVC"-ish, but eh, whatever.

The only problem with TodoMVC is that it doesn't have examples of asynchronous backend communication, which can make or break a framework decision. In keeping with the spirit of TodoMVC, my React/Redux implementation will also not have asynchronous communication; if I remember, I'll write a separate blog post describing how to do that after this series wraps up.

So, let's get started!

Here's what I've done so far, just some housekeeping work at the start:

- Forked the [TodoMVC app template](https://github.com/tastejs/todomvc-app-template).
- Moved everything to the [`gh-pages` branch](https://pages.github.com/), where I'll do all the development *and* deployment simultaneously.
- Removed the (empty) `app.css` from the app template, since I'm quite sure I won't be needing it.
- Filled out some of the placeholders in the HTML.
- Tweaked the `.gitignore` to include the "boilerplate" css that comes as dependencies of the app template.

Groundbreaking, eh? Don't worry, this is just getting started! :)

[Source code at this revision](https://github.com/StephenCleary/todomvc-react-redux/tree/9b881b0bea8070f850c8c78a6fcf4701287101ae)

[Live site at this revision](http://htmlpreview.github.io/?https://github.com/StephenCleary/todomvc-react-redux/blob/9b881b0bea8070f850c8c78a6fcf4701287101ae/index.html) (ignore the "startup flicker"; that's just due to the way it's hosted)

[Most current source code](https://github.com/StephenCleary/todomvc-react-redux)

[Most current live site](http://stephencleary.github.io/todomvc-react-redux/)
