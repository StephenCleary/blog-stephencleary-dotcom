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

Since I already have a `Main` component (oops), at the end of this point I'm going to rename my `Main` component to be `TodoApp` instead. My goal here is consistency: I'm going to break up `TodoApp` into `Header`, `Main`, and `Footer`, in a way that the `class` of each HTML element will match the name of its React component. This just results in more maintainable code.

<div class="alert alert-info" markdown="1">
<i class="fa fa-hand-o-right fa-2x pull-left"></i>

If I had more control over the HTML output, I could choose to change the `class` for the main section from `main` to something like `todos`, and keep my old `Main` component as it is.

However, in this case, the TodoMVC app should have fairly specific HTML output, to enable things like [automated acceptance testing](https://github.com/tastejs/todomvc/tree/master/tests).
</div>

So, I have to rename my old `Main` component because I didn't think far enough ahead. Sorry about that. I *am* totally making this up as I go along...

## Breaking Up is Easy to Do

Let's break out the child components first. In React, a common pattern is to use a `components` subdirectory to hold all the components. In larger apps, there can be further subdirectories underneath this, but this app is small enough that all the components should fit. I think. :)

I'm going to (temporarily) call that middle child `Todos` instead of `Main` (I'll fix this in the next step). The `Todos` ends up looking like this (in `components/todos.jsx`):

    import React from 'react';

    export default function Todos() {
        // This section should be hidden by default and shown when there are todos
        return (
            <section className="main">
			  (copied from main.jsx)
            </section>
        );
    }

Well, that was pretty easy... The `Footer` follows the exact same pattern (exposing a `Footer` component from `components/footer.jsx`).

The header is a bit more difficult, because we have the `dispatch` method coming in and used in the header. There are three common ways to approach this.

1. For those who like their dumb components to be *really* dumb, then the `dispatch` method is only connected to the smart component parent (in this case, `Main`). `Main` will then bind `dispatch` to the action creator (`TodoActions.add`), resulting in a new function. That function is then passed down to the dumb component, which uses it to respond to the user action.
2. For those who like their dumb components to be *somewhat* dumb, then the `dispatch` method is only connected to the smart component parent (`Main`). `Main` will then pass `dispatch` down to the dumb component, and the dumb component calls the action creator (`TodoActions.add`) and passes the resulting action to the `dispatch` it got from its parent.
3. For those who like their dumb components to be *only a little* dumb, then the `dispatch` method is connected to the dumb component directly. The dumb component calls the action creator (`TodoActions.add`) and passes the resulting action to its own `dispatch`.

You could argue that the first option is the "most pure", and the last one is "least pure". While there are advantages to purity (namely, reusability), in my (limited) experience I feel that the repetitive boilerplate required by the purer approaches outweighs their advantages. For this reason, I take the third approach and just connect the dumb component so it gets its own `dispatch` directly.

Applying this to the `Header` yields this (in `components/header.jsx`):

    import React from 'react';
    import { connect } from 'react-redux';
    import TodoActions from '../actions/todoActions';

    function Header({dispatch}) {
        return (
            <header className="header">
                <h1>todos</h1>
                <input className="new-todo" placeholder="What needs to be done?" autoFocus onBlur={e => dispatch(TodoActions.add(e.target.value))}/>
            </header>
        );
    }

    export default connect()(Header);

As you can see, I'm connecting the `Header` component, so that it gets `dispatch`, which it then uses directly to dispatch the `ADD_TODO` action at the appropriate time.

Now that all the components are broken out, `Main` is quite simple (in `main.jsx`):

    function Main() {
        return (
            <section className="todoapp">
                <Header/>
                <Todos/>
                <Footer/>
            </section>
        );
    }

## Renaming

Finally, I have to do a bit of cleanup, because I chose a poor name (`Main`) without thinking of my HTML structure first.

So, I'm going to rename the old `Main` to `TodoApp` instead (and also change the file name from `main.jsx` to `todoApp.jsx`), and then rename `Todos` to `Main` (and change the file name from `components/todos.jsx` to `components/main.jsx`).

Now the component naming scheme is more maintainable: each part's `class` attribute matches the component name and the file name in which that component lives.

[Source code at this revision](https://github.com/StephenCleary/todomvc-react-redux/tree/9c9959be0a85965098c40db1878f5a84420ae015) - [Live site at this revision](http://htmlpreview.github.io/?https://github.com/StephenCleary/todomvc-react-redux/blob/9c9959be0a85965098c40db1878f5a84420ae015/index.html) (ignore the "startup flicker"; that's just due to the way it's hosted)

[Most current source code](https://github.com/StephenCleary/todomvc-react-redux) - [Most current live site](http://stephencleary.github.io/todomvc-react-redux/)
