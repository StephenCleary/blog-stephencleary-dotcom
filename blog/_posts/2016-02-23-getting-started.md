---
layout: post
title: "React/Redux TodoMVC, Part 2: Getting Started"
series: "React/Redux TodoMVC"
seriesTitle: "Getting Started"
description: "Converting the TodoMVC to JSX."
---

Let's introduce some React! The `react` package contains almost everything, and the `react-dom` package is the piece that injects our React application into the browser DOM.

<div class="alert alert-info" markdown="1">
<i class="fa fa-hand-o-right fa-2x pull-left"></i>

Most modern JavaScript libraries do not assume they're running in a browser. This is to enable server-side rendering. It's not as crazy as it sounds! Check out [this video](https://channel9.msdn.com/Events/ASPNET-Events/ASPNET-Fall-Sessions/ASPNET--Spa) of [work being done on ASP.NET Core *right now*](https://github.com/aspnet/NodeServices) where ASP.NET hosts Node, runs the web app on the server, and then sends the results to the client.
</div>

## Our First React Component

For now, I just want to move the boilerplate HTML code into my JSX file. We'll tear it apart more properly later. So, I imported React and wrote a `Main` method (in `main.jsx`):

    import React from 'react';
    function Main() {
        return (

	    );
	}

and pasted all the `<section class="todoapp">` HTML in there.

JSX isn't *quite* HTML; it's just pretty close. Here's a list of tidying up I had to do (note that ESLint will catch all of these):

- Unclosed elements are not allowed. In particular, `<input>` tags must become `<input/>` tags. This is totally understandable.
- JSX has no notion of comments. You can work around this by embedding JavaScript comments. So all `<!--` must be replaced by `{/*`, and all `-->` by `*/}`. This is not understandable; it's annoying.
- JSX is case-sensitive, and prefers camelCasing. E.g., `autofocus` needs to be `autoFocus`.
- There's a few reserved words in JavaScript that JSX has to work around. In particular, the common `class` HTML attribute has to be `className` in JSX.

It's also time to turn off some ESLint warnings that are just too pedantic. [jsx-no-literals](https://github.com/yannickcr/eslint-plugin-react/blob/master/docs/rules/jsx-no-literals.md) and [jsx-max-props-per-line](https://github.com/yannickcr/eslint-plugin-react/blob/master/docs/rules/jsx-max-props-per-line.md) are history.

This gets us down to a more reasonable number of warnings:

{:.center}
![]({{ site_url }}/assets/ESLintWarnings.png)

Of these, I'm going to fix `autoFocus` in the source, and turn off [jsx-sort-props](https://github.com/yannickcr/eslint-plugin-react/blob/master/docs/rules/jsx-sort-props.md), which leaves us with the display name.

The [display name](https://github.com/yannickcr/eslint-plugin-react/blob/master/docs/rules/display-name.md) warning triggers when you don't explicitly give React a name for your component that it can use in error messages. In this case, though, Babel will do it automatically for us, so we just need to let ESLint know that it's OK (in `.eslintrc`):

	"react/display-name": [1, { "acceptTranspilerName": true }],

## Sticking It in the DOM

First, let's define a placeholder for our app in th HTML; I added this line where I removed the `<section class="todoapp">` (in `index.html`):

    <div id="app"></div>

This will create an extra `<div>` wrapper in the output, but that's usually not anything to worry about.

Next, we import `render` from `react-dom` and then load our app after the DOM is loaded (in `main.jsx`):

    import { render } from 'react-dom';
	
	// Definition of Main

	window.onload = () => render(Main(), document.getElementById('app'));

Now we can build and run our app. It still doesn't do anything, but now it's a *React* component that doesn't do anything. :)

[Source code at this revision](https://github.com/StephenCleary/todomvc-react-redux/tree/f54223a8647b43b99d62a547505c4b5908459bb6) - [Live site at this revision](http://htmlpreview.github.io/?https://github.com/StephenCleary/todomvc-react-redux/blob/f54223a8647b43b99d62a547505c4b5908459bb6/index.html) (ignore the "startup flicker"; that's just due to the way it's hosted)

[Most current source code](https://github.com/StephenCleary/todomvc-react-redux) - [Most current live site](http://stephencleary.github.io/todomvc-react-redux/)
