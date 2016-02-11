---
layout: post
title: "React/Redux TodoMVC, Part 1: Language and Tooling"
series: "React/Redux TodoMVC"
seriesTitle: "Language and Tooling"
description: "Setting up Babel, ESLint, and Webpack for ES2015+/JSX."
---

Modern JavaScript development is lightyears ahead of where it used to be. Last summer, ES2015 (ES6) was officially standardized, and even though browsers are taking their time adopting it, transpilers have existed for some time.

## Babel

After trying out a few options for DotNetApis, I settled on Babel. Babel allows you to specify exactly what language you want to support, and I have mine set up as ES2015, plus JSX, plus object spread/rest properties.

[ES2015](https://babeljs.io/docs/learn-es2015/) is the most basic standard for JavaScript development these days. If you're not on ES2015, then you're developing in the dark ages. Seriously. It will change your life.

[JSX](https://facebook.github.io/jsx/) is the syntactic extension that React uses for putting HTML in the JavaScript. Technically, React can work completely without JSX, but it's harder to write and read. And it's verbosely ugly, too.

[Object rest/spread properties](https://github.com/sebmarkbage/ecmascript-rest-spread) are a [Stage 2 proposal](http://www.2ality.com/2015/11/tc39-process.html) that will likely be in a future version of JavaScript. I include them in this project for reasons that will become clear when we develop our React reducers.

My `.babelrc` file just enables these three presets:

    {
      "presets": ["es2015", "react", "stage-2"]
    }

## No Typings - Steve Sad

Unfortunately, there are no static typings with this setup. I tried really, *really* hard to get them working, because at my core I'm a static typing kind of guy. I like my code completion, and I like my compiler checking!

You *can* get (modern versions of) [TypeScript](http://www.typescriptlang.org/) to work with React. However, there are a few problems that I ran into.

- TypeScript is (much) slower than Babel at picking up new language features. TypeScript does support JSX now, but there's no support for object rest/spread properties, which are very convenient.
- It seems that Anders' team is more interested in getting it working for Visual Studio [Code] than in developing a true cross-platform language. When you see what issues the team prioritizes, you just get that feeling.
- The existing typings repository is quite incomplete.
- The value of a more strict object-oriented system bolted onto JavaScript is dubious at best. I prefer libraries and patterns that take advantage of JavaScript's inherently functional (or multi-paradign) nature.
- TSLint is way less mature and flexible than ESLint.

That said, I do really like the benefits of static typing, and I tried quite hard to get TypeScript working with DotNetApis. However, all the little frustrations with TypeScript added up, and that initiative didn't make it.

My problem is that I didn't really want to change languages to TypeScript. All I wanted was to use TypeScript as a static type checker for *modern* JavaScript, and it's this goal that I couldn't get working. TypeScript is a fine language, but I wanted to use JavaScript.

There's another, lesser known (for now) static typing system on the block: [Flow](https://code.facebook.com/posts/1505962329687926/flow-a-new-static-type-checker-for-javascript/).

Flow looks exactly like what I need: static typing for JavaScript. Babel even has support for Flow ready to go!

Unfortunately, I just couldn't get the (unofficial) Windows port to behave reliably. Flow works great for other platforms, but they need to fully support Windows if they're going to edge out TypeScript.

Hopefully in the future, either TypeScript or Flow will meet my needs. For now, neither one does. :(

## Supporting Tools: ESLint

When it comes to linting modern JavaScript, [ESLint](http://eslint.org/) is the most flexible option. There's an [ESLint plugin for React](https://github.com/yannickcr/eslint-plugin-react) that adds a lot of React-specific linting rules.

For now, I'm just going to turn on every rule. I'll relax some of these as I go along, but for now ESLint will be very strict. My current `.eslintrc` starts by setting up the language (ES2015 + JSX + object rest/spread) and environment (Browser, ES2015/ES6):

    "extends": "eslint:recommended",
    "plugins": [
      "react"
    ],
    "ecmaFeatures": {
      "modules": true,
      "jsx": true,
      "experimentalObjectRestSpread": true
    },
    "env": {
      "browser": true,
      "es6": true
    },

After this, I have a long section enabling all of the React/JSX rules, that I just copied and pasted from the [ESLint React plugin homepage](https://github.com/yannickcr/eslint-plugin-react/tree/8fe83a0e716ca2db225e98b37b4efa5e2f277848).

## Supporting Tools: Webpack

We're also going to need a "bundler". DotNetApis uses Webpack, so I'll use it, too. I don't have any experience with other bundlers; my initial research indicated that Webpack was popular, and I haven't run into any problems with it, so that's what I ended up with.

The TodoMVC framework expects a single `./js/app.js` file to be our application, with other supporting source files also under `./js`. So I'll use `.jsx` for our source files, and specify `./js/main.jsx` as the "main entry point".

The `webpack.config.js` will start with that `main.jsx`, load all `.jsx` files via Babel, and output `app.js`. Also, I'm going to include [webpack source maps](https://webpack.github.io/docs/configuration.html#devtool). The webpack config file now looks like this:

    module.exports = {
        entry: './js/main.jsx',
        output: {
            filename: './js/app.js'
        },
        module: {
            loaders: [
                {
                    test: /\.jsx$/,
                    loader: 'babel-loader'
                }
            ]
        },
        devtool: 'inline-source-map'
    };

With just a bit of tweaking, we can use [`eslint-loader`](https://github.com/MoOx/eslint-loader) to lint while we build:

    module: {
        loaders: [
            {
                test: /\.jsx$/,
                loader: 'babel-loader'
            }
        ],
        preLoaders: [
            {
                test: /\.jsx$/,
                loader: 'eslint-loader',
                exclude: /node_modules/
            }
        ]
    },
    devtool: 'inline-source-map',
    eslint: {
        configFile: './.eslintrc'
    }

## Supporting Tools: npm scripts

Finally, we're ready to bring it all together! All we have to do is add a `build` script to our `package.json` that runs `webpack`:

    "scripts": {
      "build": "node_modules/.bin/webpack"
    },

And now we can build our app!

    npm run build

## Watching

Watching is a piece of cake (in `package.json`):

    "scripts": {
      "build": "node_modules/.bin/webpack",
      "watch": "node_modules/.bin/webpack --watch"
    },

Then we can have one command prompt open all the time, just for rebuilding our code files as they change:

    npm run watch

## Supporting Tools: Babel Runtime

Babel is an excellent transpiler, but it does require a [runtime polyfill](https://babeljs.io/docs/usage/polyfill/) for some language features. I'll just include that for now in my `main.jsx`:

    import 'babel-polyfill';

## Extra Credit: Local Dev Server

DotNetApis uses a real ASP.NET backend, so when I debug locally, I'm actually running the dev ASP.NET server. That won't work in this scenario, so I checked out dev servers for npm. Of course, webpack has one, but it prevents webpack from writing its results to disk, which works great for some scenarios but is not what I'm wanting.

So I looked around and found a promising looking one called [`http-server`](https://www.npmjs.com/package/http-server) (where "promising" means "used by a lot of other people so if I have problems they're easy to solve").

Install it, add a `serve` script, and we're all set! (in `package.json`):

    "scripts": {
      "build": "node_modules/.bin/webpack",
      "watch": "node_modules/.bin/webpack --watch",
      "serve": "node_modules/.bin/http-server -o"
    },

I can now open a second command prompt for my HTTP server:

    npm run serve

## Current State

At this point, we've got automatic recompiling anytime our source files change, a local dev server to see the results, and the beginnings of a great development experience. Pop open your browser dev tools (F12), and you should see original source files - even though they're not actually sent to the browser:

{:.center}
![]({{ site_url }}/assets/SourceMaps.png)

[Source code at this revision](https://github.com/StephenCleary/todomvc-react-redux/tree/41de4bc84d575443fcaa42e48eec7812e0e5b4c3) - [Live site at this revision](http://htmlpreview.github.io/?https://github.com/StephenCleary/todomvc-react-redux/blob/41de4bc84d575443fcaa42e48eec7812e0e5b4c3/index.html) (ignore the "startup flicker"; that's just due to the way it's hosted)

[Most current source code](https://github.com/StephenCleary/todomvc-react-redux) - [Most current live site](http://stephencleary.github.io/todomvc-react-redux/)
