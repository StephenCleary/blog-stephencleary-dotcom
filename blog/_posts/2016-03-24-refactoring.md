---
layout: post
title: "React/Redux TodoMVC, Part 6: Refactoring"
series: "React/Redux TodoMVC"
seriesTitle: "Refactoring"
description: "React prefers smaller components."
---

Today we're going to start breaking up that huge `Main` component. Yes, I said "huge" - in the React world, smaller components are more normal. React encourages the creation of small, reusable components, which are then composed together.

There's three big parts to the `Main` component: a header, a main part, and a footer:

    <section className="todoapp">
	    <header className="header">
		    ...
		</header>
		<section className="main">
		    ...
		</section>
		<footer className="footer">
		    ...
		</footer>
	</section>

Since I already have a `Main` component (oops), I'm going to rename my `Main` component to be `TodoApp` instead. My goal here is consistency: I'm going to break up `TodoApp` into `Header`, `Main`, and `Footer`, in a way that the `class` of each HTML element will match the name of its React component. This just results in more maintainable code.

<div class="alert alert-info" markdown="1">
<i class="fa fa-hand-o-right fa-2x pull-left"></i>

If I had more control over the HTML output, I could change the `class` for the main section from `main` to something like `todos`, and keep my old `Main` component as it is.

However, in this case, the TodoMVC app should have a fairly specific HTML output, to enable things like [automated acceptance testing](https://github.com/tastejs/todomvc/tree/master/tests).
</div>

So, I have to rename my old `Main` component because I didn't think far enough ahead. Sorry about that. I *am* totally making this up as I go along...




[Source code at this revision](https://github.com/StephenCleary/todomvc-react-redux/tree/) - [Live site at this revision](http://htmlpreview.github.io/?https://github.com/StephenCleary/todomvc-react-redux/blob//index.html) (ignore the "startup flicker"; that's just due to the way it's hosted)

[Most current source code](https://github.com/StephenCleary/todomvc-react-redux) - [Most current live site](http://stephencleary.github.io/todomvc-react-redux/)

FUTURE: singleton immutable state (as opposed to a messaging bus) means applications grow in complexity linearly instead of geometrically