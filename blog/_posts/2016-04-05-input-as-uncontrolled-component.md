---
layout: post
title: "React/Redux TodoMVC, Part 8: Uncontrolled and Controlled Components"
series: "React/Redux TodoMVC"
seriesTitle: "Uncontrolled and Controlled Components"
description: "Adding todo items with uncontrolled components, and marking them as complete with controlled components."
---

Now that we have the basic Redux pattern in place, the rest of the implementation starts to follow pretty easily. And this is really the point of the entire Redux system: the central store for our application's state restricts how quickly the complexity of the code can grow.

## Finishing the Input Control (as an Uncontrolled Component)

Taking a look back at the [spec](https://github.com/tastejs/todomvc/blob/master/app-spec.md), the input box in the header of the page needs to:

- Create a new item whenever it loses focus or the user presses the enter key.
- Trim the new item's text, and not create an empty item.
- Clear the input box so it's ready to create the next item.

Like most frameworks, React provides a [cross-browser event system](https://facebook.github.io/react/docs/events.html) to make this kind of work straightforward. First, let's handle the trimming and clearing appropriately (in `components/header.jsx`):

    function addTodo(dispatch, e) {
        const text = e.target.value.trim();
        if (text) {
            dispatch(TodoActions.add(text));
        }
        e.target.value = '';
    }

	function Header({dispatch}) {
        return (
            <header className="header">
                <h1>todos</h1>
                <input className="new-todo" placeholder="What needs to be done?" autoFocus onBlur={e => addTodo(dispatch, e)}/>
            </header>
        );
    }

Now our `onBlur` handler just calls a helper method that handles all the logic.

And adding support for the enter key (in `components/header.jsx`):

    const ENTER_KEY = 13;

    function inputKeyPress(dispatch, e) {
        if (e.keyCode === ENTER_KEY) {
            addTodo(dispatch, e);
        }
    }

	// In Header
    <input className="new-todo" placeholder="What needs to be done?" autoFocus onBlur={e => addTodo(dispatch, e)} onKeyDown={e => inputKeyPress(dispatch, e)}/>

This is an example of an [uncontrolled component](https://facebook.github.io/react/docs/forms.html). An "uncontrolled component" is one whose value does *not* come from the store. In this case, the text of the new todo item lives only in the `value` of the textbox (in the DOM), and is only dispatched to the store when the user indicates they are done.

What we're actually seeing here is a minor violation of Redux: when the user is typing a new todo item, we're storing state in the DOM rather than the store. This is OK, though, because we can choose to ignore the "intermediate" state of the todo item.

Most of the time, though, you'll probably want to use *controlled components*.

## Marking Items Complete (with Controlled Components)

We're going to need a new action to mark items complete (or more properly, to *toggle* their completion state).



[Source code at this revision](https://github.com/StephenCleary/todomvc-react-redux/tree/) - [Live site at this revision](http://htmlpreview.github.io/?https://github.com/StephenCleary/todomvc-react-redux/blob//index.html) (ignore the "startup flicker"; that's just due to the way it's hosted)

[Most current source code](https://github.com/StephenCleary/todomvc-react-redux) - [Most current live site](http://stephencleary.github.io/todomvc-react-redux/)



FUTURE:

The problem with data-binding frameworks (MVC / MVVM)
singleton immutable state (as opposed to a messaging bus) means applications grow in complexity linearly instead of geometrically

Reducer composition (when we add editing state)

Inline CSS

React/Redux vs jQuery (why the fight: it's actually a fight over where state is stored)

React + whatever; Redux + whatever

React Native

To fix:
- State is not an object.
- connect(x => x), once reducers are composed.