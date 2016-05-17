---
layout: post
title: "React/Redux TodoMVC, Part 7: Displaying Store Data"
series: "React/Redux TodoMVC"
seriesTitle: "Displaying Store Data"
description: "Retrieving data from the Redux store and displaying it."
---

My goal for today is to read the todo items that are actually in the Redux store, and show them in the views.

First, let's split out that `todo-list` unordered list into its own component (in `components/todoList.jsx`):

    import React from 'react';

    export default function TodoList() {
        return (
            <ul className="todo-list">
                {/* These are here just to show the structure of the list items */}
                {/* List items should get the class `editing` when editing and `completed` when marked as completed */}
                <li className="completed">
                    <div className="view">
                        <input className="toggle" type="checkbox" checked/>
                        <label>Taste JavaScript</label>
                        <button className="destroy"/>
                    </div>
                    <input className="edit" value="Create a TodoMVC template"/>
                </li>
                <li>
                    <div className="view">
                        <input className="toggle" type="checkbox"/>
                        <label>Buy a unicorn</label>
                        <button className="destroy"/>
                    </div>
                    <input className="edit" value="Rule the web"/>
                </li>
            </ul>
        );
    }

Next let's start on the actual todo list item, and focus on this for a bit (in `components/todoItem.jsx`):

    import React from 'react';
    
    export default function TodoItem() {
        // List items should get the class `editing` when editing and `completed` when marked as completed.
        return (
            <li className="completed">
                <div className="view">
                    <input className="toggle" type="checkbox" checked/>
                    <label>Taste JavaScript</label>
                    <button className="destroy"/>
                </div>
                <input className="edit" value="Create a TodoMVC template"/>
            </li>
        );
    }

One thing really sticks out to me about this example template: the `<input>` element is always present, even when not editing. It must be getting hidden by the CSS when the parent `<li>` doesn't have the `editing` class. Personally, I would just remove the `<input>` from the DOM entirely when not editing, but, eh, whatever.

## Receiving Data

In React, components can get their underlying data from several sources: passed down as ["props" from their parent](https://facebook.github.io/react/docs/tutorial.html#using-props), passed down as ["context" from any ancestor](https://facebook.github.io/react/docs/context.html), or stored in the component itself as [part of its "state"](https://facebook.github.io/react/docs/tutorial.html#reactive-state). Redux limits this further; since all state lives in the store, components cannot have their own state. However, Redux does [allow components to read from the store](https://github.com/reactjs/react-redux/blob/master/docs/api.md#connectmapstatetoprops-mapdispatchtoprops-mergeprops-options).

<div class="alert alert-info" markdown="1">
<i class="fa fa-hand-o-right fa-2x pull-left"></i>

React/Redux components are not ever supposed to have their own state! If you're using Redux, just pretend that the [`React.Component.getState` and `React.Component.replaceState` APIs](https://facebook.github.io/react/docs/component-api.html) don't even exist.
</div>

Here's my current understanding of when these different options should be used:

- Whenever feasible, [pass props down from the parent](https://facebook.github.io/react/docs/tutorial.html#using-props). This encourages the smart/dumb component distinction, which is a React best practice.
- Top-level or "smart" components should [connect to the Redux store](https://github.com/reactjs/react-redux/blob/master/docs/api.md#connectmapstatetoprops-mapdispatchtoprops-mergeprops-options).
- Be cautious about [passing context down from an ancestor](https://facebook.github.io/react/docs/context.html). Context is implicit and thus is more difficult to follow through the code. Context would be good to use when you have some piece of data that would have to be passed to practically every component.

The general approach would be to have the smart components connect to the store, and pass that data down to the dumb components. Context should only be considered if you find yourself with data that is being passed everywhere.

With that in mind, should our `TodoItem` component connect directly to the store, or just use data passed to it from its parent? By default, we should try to make a dumb component. Even if it *did* connect to the store as a smart component, it would have to at least know *which* todo item it is, so it would need an id or index from its parent component anyway. It seems pretty clear that `TodoItem` is a dumb component.

OK, so we'll have `TodoItem` receive its data (the todo item) from its parent component. We need to define a "shape" for this todo item. To keep things consistent (and simple!), I'll just use the same type of todo item that is being kept in our store. This shape is defined implicitly in our reducer (from `reducers.jsx`):

    export default handleActions({
        [ActionTypes.ADD_TODO]: (state, action) => [...state, { completed: false, text: action.payload }]
    }, []);


<div class="alert alert-info" markdown="1">
<i class="fa fa-hand-o-right fa-2x pull-left"></i>

The structure of your application state is defined (implicitly) by your reducers.
</div>

So, the shape of our todo item is `{ completed: boolean, text: string }`. We'll just have our `TodoItem` component take an `item` that matches that same pattern, and use its properties to generate the HTML (in `components/todoItem.jsx`):

    function TodoItem({item}) {
        // List items should get the class `editing` when editing and `completed` when marked as completed.
        const className = item.completed ? 'completed' : '';
        return (
            <li className={className}>
                <div className="view">
                    <input className="toggle" type="checkbox" checked/>
                    <label>{item.text}</label>
                    <button className="destroy"/>
                </div>
                <input className="edit" value="Create a TodoMVC template"/>
            </li>
        );
    }

I'm using [object destructuring](http://exploringjs.com/es6/ch_destructuring.html) in the parameter declaration. In React, the first parameter of function components are the properties (attributes) set by the parent. They're passed as a single object, which is normally called `props`. In this case, I only care about `props.item`.

Next, I define a local variable that holds the `class` for the `<li>` element.

Finally, I use both the local variable `className` and the incoming `item` in my JSX. You can think of JSX as having two "modes": JavaScript and XML. To enter XML mode, just write an opening XML tag (like `<li`). XML mode ends when that tag is closed. When you're in XML mode, you can enter a *nested* JavaScript mode by wrapping your JavaScript in `{` and `}`. You can then nest XML within *that* JavaScript, etc., but before you get too carried away, please consider maintainability and use child components or at least helper methods. :)

The expression `{className}` just takes the value of `className` and uses it as the `className` property of the `<li>` element (which eventually becomes the `class` attribute of the `<li>` in the DOM). Note that there's no templating here - there's no need for quotes around `{className}` in the JSX. This is because the value being passed is a true value, not just a string. OK, this is a bad example of this because both `className` and `item.text` *are* strings, but you'll see more what I mean when we move to the parent component.

Also of note is that `{item.text}` is an actual *expression*. In fact, *any* kind of valid JavaScript expression can be embedded within JSX! In this example I could easily remove the local `className` variable and use `{item.completed ? 'completed' : ''}` instead. However, I know that more logic will go into that expression when I add support for editing, so I pulled it out right away. I prefer my JSX expressions to not be too complex.

## PropTypes

Before moving on, one last React best practice: let's have our component tell React what types of `props` it's expecting. Eventually, I hope this kind of information will be used by IDEs to drive JSX autocompletion (and conformance warnings), but even today PropTypes are useful since they are checked at runtime if you run the non-minimized version of React (which we are).

Defining the `propTypes` is straightforward (in `components/todoItem.jsx`):

    TodoItem.propTypes = {
        item: React.PropTypes.shape({
            completed: React.PropTypes.bool.isRequired,
            text: React.PropTypes.string.isRequired
        }).isRequired
    };

Defining `propTypes` is not required, but is recommended.

Confession time: I do not use `propTypes` (currently) in DotNetApis. However, that project (at least for now) is maintained by only one person: me. If I were using React as part of a team, I would always use `propTypes`, and encourage everyone else to, also.

I hope that in the future, `propTypes` could be implied by flow annotations, so in a future version of React/JavaScript, I can just use Flow and get `propTypes` for free, along with all the other static typing goodness.

## Using the TodoItem

Now let's move to the `TodoList` component. We want this component to get the collection of todo items from the store, and then pass each one down to a `TodoItem` instance.

Let's connect it to the store first, using [Redux's `connect`](https://github.com/reactjs/react-redux/blob/master/docs/api.md#connectmapstatetoprops-mapdispatchtoprops-mergeprops-options) (in `components/todoList.jsx`):

	import React from 'react';
	import { connect } from 'react-redux';

	function TodoList() {
		return (
			<ul className="todo-list">
			</ul>
		);
	}

	export default connect(x => x)(TodoList);

Note that `connect(x => x)` is an anti-pattern for performance reasons. However, I always find it useful to use when starting out with Redux, or introducing Redux to an existing codebase. If you're following along with your own app, be sure to write this down: "TODO: fix connect calls." We *will* come back and fix this a little later in this series.

Unfortunately, it didn't work:

{:.center}
![]({{ site_url }}/assets/ReduxConnectError.png)

What this error message is saying is that the function we pass to Redux's `connect` needs to return an *object*, but we're actually returning an *array*. From experience, I know that our application state will become a real object instead of an array very soon, but for now we can just cheat a bit by changing our `connect` call:

    export default connect(x => ({ todos: x }))(TodoList);

OK, that fixed the error message:

{:.center}
![]({{ site_url }}/assets/EmptyTodos.png)

Now that `TodoList` is connected to the store, we need to have it use the `TodoItem` component. In fact, we need to do a kind of "foreach" over the todo item collection in the store, and map each one to a new `TodoItem` instance.

There's a straightforward way to map a source array (of todo items) to a result array (of `TodoItem`s): [`Array.prototype.map`](https://developer.mozilla.org/en-US/docs/Web/JavaScript/Reference/Global_Objects/Array/map).

<div class="alert alert-info" markdown="1">
<i class="fa fa-hand-o-right fa-2x pull-left"></i>

Tip: It's much more natural to approach React/Redux using functional programming rather than imperative. In this example, `map` (functional) is a better fit than `for of` (imperative). Both solutions are possible, but the functional approach is less awkward.
</div>

Using `map`, our `TodoList` can look like this (in `component/todoList.jsx`):

	function TodoList({todos}) {
		return (
			<ul className="todo-list">
				{todos.map((item, index) => <TodoItem key={index} item={item}/>)}
			</ul>
		);
	}

This pattern is extremely common in React apps, so let's examine it in detail.

The function `TodoList` now accepts `todos` as one of its `params` (this is passed to our component by Redux's `connect`). This `todos` is an array of todo items (the full `propTypes` is [in the source code]()).

The `TodoList` component returns a single `<ul>`. The children of that `<ul>` are dynamically determined by `todos.map((item, index) => <TodoItem key={index} item={item}/>)`.

This expression first takes the `todos` array, and calls `map`, passing a mapping function. This mapping function is called for each item in the array; `map` passes the array item itself as the first parameter, and the item index as the second parameter.

For each data item in the array, our mapping function then creates a `<TodoItem>` component to return. We set two properties on the `<TodoItem>` component: `key` and `item`.

The `item` property should be obvious; our `TodoItem` takes a single `item` property, which is the todo item itself. JSX passes actual values as its properties, so we can just pass the store data item straight through. `item` is much more than just a string.

<div class="alert alert-info" markdown="1">
<i class="fa fa-hand-o-right fa-2x pull-left"></i>

React treats your components just the same as its own components!
</div>

The other property is not familiar: `key`. In React, when you have a *collection* of children to render, you [should give them all a unique `key` property](https://facebook.github.io/react/docs/multiple-components.html#dynamic-children). This property is special to React (and yes, this means you shouldn't ever name one of your properties "key").

In a more real-world scenario, our todo data items would probably have some kind of unique identifier, and our parent component would look more like this:

	<ul className="todo-list">
		{todos.map(item => <TodoItem key={item.id} item={item}/>)}
	</ul>

In this scenario, though, our todo items do not have any kind of unique identifier, so I'm just using their array index as their `key`. This is not as efficient as a true identifier key would be, but it suffices.

One final note on the `key` property: it's not the end of the world if you *don't* specify it at all. In fact, in our example where we pass an array of child components to `<ul>`, React will automatically set the `key` property to the array index. However, it will also give you this warning:

{:.center}
![]({{ site_url }}/assets/ReactChildKey.png)

By specifying the `key` explicitly, I'm just telling React that I know this isn't the most efficient solution, but that there isn't a better one available. If the todo items *did* have a unique `id`, I would use that instead.

## What We've Done So Far

At this point, we have:

- Our application data locked inside a Redux store.
- An action `ADD_TODO` that allows adding a todo item.
- A reducer that handles `ADD_TODO` by appending a todo item to the existing list of todo items.
- An input box that triggers the `ADD_TODO` item.
- Our own `TodoList` and `TodoItem` components which show the todo items.

This completes the first "loop". We now have the capability to dynamically add todo items via the input box, and our todo list will update itself by creating a new todo item component.

Note that all the data flows in one direction, through one central store. In particular, the input box does *not* tell the `TodoList` component that there's a new todo. Instead, all updates are dispatched to the store, and the store notifies all components.

This establishes the store as the single source of truth in our application. It seems like a lot of overhead for a simple application, but in more complex applications, having a single source of truth is incredibly empowering.

[Source code at this revision](https://github.com/StephenCleary/todomvc-react-redux/tree/c12e47b7e5a4abd74e79d14f047a9d29831c343e) - [Live site at this revision](http://htmlpreview.github.io/?https://github.com/StephenCleary/todomvc-react-redux/blob/c12e47b7e5a4abd74e79d14f047a9d29831c343e/index.html) (ignore the "startup flicker"; that's just due to the way it's hosted)

[Most current source code](https://github.com/StephenCleary/todomvc-react-redux) - [Most current live site](http://stephencleary.github.io/todomvc-react-redux/)
