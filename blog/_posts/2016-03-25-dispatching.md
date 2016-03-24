---
layout: post
title: "React/Redux TodoMVC, Part 5: Dispatching"
series: "React/Redux TodoMVC"
seriesTitle: "Dispatching"
description: "Dispatching actions with Redux."
---

Well, I think it's about time our app did something!

The official [TodoMVC application specification](https://github.com/tastejs/todomvc/blob/master/app-spec.md) says we're supposed to create a TODO item if the user types in the input box at the top and then tabs out of it.

Let's do it!

## Getting the Dispatcher

Redux provides a way to get a `dispatch` method passed to our view components. Currently, our `Main` view component looks like this:

    function Main() {
        ...
    }

With Redux, we can use [the `connect` method](https://github.com/rackt/react-redux/blob/master/docs/api.md#connectmapstatetoprops-mapdispatchtoprops-mergeprops-options) to notify the `Provider` that we will need the `dispatch` method. `Main` now looks like this (in `main.jsx`):

    function MainImpl({dispatch}) {
        ...
    }
    const Main = connect()(MainImpl);

This code would be a bit cleaner if we were defining `Main` in its own file. And we will, shortly. :)

## Using the Dispatcher

Now, when the user tabs off that input field, we want to:

- Read the text of that field.
- Pass that text to our `ADD_TODO` action creator, which returns us an `ADD_TODO` action.
- Pass that `ADD_TODO` action to `dispatch`.

I'm going to just do this all inline for now. The old JSX element:

    <input className="new-todo" placeholder="What needs to be done?" autoFocus/>

and the new JSX element:

    <input className="new-todo" placeholder="What needs to be done?" autoFocus onBlur={e => dispatch(TodoActions.add(e.target.value))}/>

And now, our application can actually create a todo!

## Seeing It in Action

Our application doesn't actually *display* the todo items yet, but we can see the results in our Redux DevTools:

{:.center}
![]({{ site_url }}/assets/ReduxResult1.png)

There you can see the `ADD_TODO` action that we created, and the result that it had on the application state (namely, adding a todo). We can expand that state:

{:.center}
![]({{ site_url }}/assets/ReduxResult2.png)

and see that the new todo item was added with the text I typed in, and with a `completed` value of `false`.

It's working! :)

[Source code at this revision](https://github.com/StephenCleary/todomvc-react-redux/tree/3f564477ba32604024f4fa3406f8edf9272ba798) - [Live site at this revision](http://htmlpreview.github.io/?https://github.com/StephenCleary/todomvc-react-redux/blob/3f564477ba32604024f4fa3406f8edf9272ba798/index.html) (ignore the "startup flicker"; that's just due to the way it's hosted)

[Most current source code](https://github.com/StephenCleary/todomvc-react-redux) - [Most current live site](http://stephencleary.github.io/todomvc-react-redux/)
