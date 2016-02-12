---
layout: post
title: "React/Redux TodoMVC, Part 4: Debugging"
series: "React/Redux TodoMVC"
seriesTitle: "Debugging"
description: "React/Redux debugging helpers."
---

The debugging support available for React and Redux is amazing! When you do make mistakes in your app, React will give you helpful console error messages.

In fact, when we converted the HTML to JSX, React started giving us errors in the console:

{:.center}
![]({{ site_url }}/assets/ReactWarnings.png)

Take a moment and actually read those error messages! Those are nice and detailed, telling you exactly what you did wrong, what the effect will be, how to fix it, and where to fix it! We'll fix those errors when we make those controls into actual React components; until then, they'll just be read-only, as the warning states.

Redux goes even further with its own kind of debugging view, which we'll add today. This thing looks simple but is just mindblowing in its power (and I was *so* late in adding this to DotNetApis!). We'll be adding this dark box on the right:

{:.center}
![]({{ site_url }}/assets/ReduxDevTools.png)

It's called [Redux DevTools](https://github.com/gaearon/redux-devtools), and not only does it display a history of all your actions and application state, but it also lets you rewind history and pretend actions never happened!

## Creating DevTools

Interestingly enough, the first step in incorporating Redux DevTools is to create the monitors that we want to use to display them.

For our system, we'll use the semi-standard `LogMonitor`-within-`DockMonitor` approach, in a new `devTools.jsx` file:

    import React from 'react';
    import { createDevTools } from 'redux-devtools';
    import LogMonitor from 'redux-devtools-log-monitor';
    import DockMonitor from 'redux-devtools-dock-monitor';

    export default createDevTools(
        <DockMonitor toggleVisibilityKey="ctrl-h" changePositionKey="ctrl-j" defaultSize={0.2}>
            <LogMonitor theme="bright" />
        </DockMonitor>
    );

Pretty straightforward: we take a `LogMonitor`, wrapped in a `DockMonitor`, and pass that to the `createDevTools` method from `redux-devtools`.

## Using DevTools

We also have to include this new `DevTools` component in our application, which we can do right in `main.jsx`:

    window.onload = () => {
        const root = (
            <Provider store={store}>
                <div>
                    <Main/>
                    <DevTools/>
                </div>
            </Provider>
        );
        render(root, document.getElementById('app'));
    }

Note that we had to put an extra `<div>` wrapper around our `Main` and `DevTools` components. This is because `Provider` can only have one child component. I find that my React applications do end up with some extra `<div>` wrappers scattered throughout, just due to React limitations. It's not a big deal, though.

Finally, we need to hook up the dev tools to our store, in `store.jsx`:

    export default createStore(reducers, DevTools.instrument());

And that's it! We now have a powerful Redux debugging environment built right in to our application!



[Source code at this revision](https://github.com/StephenCleary/todomvc-react-redux/tree/) - [Live site at this revision](http://htmlpreview.github.io/?https://github.com/StephenCleary/todomvc-react-redux/blob//index.html) (ignore the "startup flicker"; that's just due to the way it's hosted)

[Most current source code](https://github.com/StephenCleary/todomvc-react-redux) - [Most current live site](http://stephencleary.github.io/todomvc-react-redux/)

FUTURE: singleton immutable state (as opposed to a messaging bus) means applications grow in complexity linearly instead of geometrically