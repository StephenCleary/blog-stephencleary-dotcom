---
layout: post
title: "Using VSCode with PEGJS"
description: "Enabling VSCode's Problems window for PEGJS compilation by parsing its output."
---

## Using VSCode with PEGJS

VSCode has a pretty decent extension system. This week I was playing around with [PEG JS](https://pegjs.org/), a [PEG](https://en.wikipedia.org/wiki/Parsing_expression_grammar) parser generator for JavaScript. I found [an extension](https://github.com/SrTobi/code-pegjs-language) for VSCode syntax highlighting of `pegjs` files. There are a couple of other options for extensions, but it looks like none of them support reporting errors in VSCode's `Problems` window.

It's pretty straightforward to run the pegjs executable from a `package.json` npm script. In this example, I'm compiling my `pegjs` file into a `js` output file:

{% highlight json %}
"peg": "pegjs -o src/json-filter-expr.js src/json-filter-expr.pegjs"
{% endhighlight %}

The problem is that if I run this task from VSCode and the compilation fails, there's no indication in the VSCode Problems window.

## Problem Matchers

The solution is to write a [problem matcher](https://code.visualstudio.com/docs/editor/tasks#_defining-a-problem-matcher). This will enable VSCode to parse the `pegjs` output and report it just like any other kind of compilation error.

There's one other bump in the road. `pegjs` writes out its error messages like this:

{% highlight text %}
40:3: Expected "=", comment, end of line, or whitespace but "/" found.
{% endhighlight %}

However, problem matchers *must* be able to parse out at least a `file`, `line`, and `message`. `pegjs` is giving us `line`, `column`, and `message`, but not `file`.

VSCode requires the `file`, which makes sense since it deals with a whole workspace and needs to know which file the problem is in. On the other hand, `pegjs` does not include a filename in its output, which makes sense because it can be run on only one file at a time, and the user specified the file name right there in the command line.

## The Solution

The solution I ended up going with is quite simple: I echo the filename from my npm script and then use a multiline problem matcher to parse out the filename:

{% highlight json %}
"peg": "echo src/json-filter-expr.pegjs && pegjs -o src/json-filter-expr.js src/json-filter-expr.pegjs"
{% endhighlight %}

Now the output from `npm run peg` looks like this:

{% highlight text %}
src/json-filter-expr.pegjs
40:3: Expected "=", comment, end of line, or whitespace but "/" found.
{% endhighlight %}

And this can be matched with a multiline problem matcher that looks like this:

{% highlight json %}
"problemMatcher": {
    "owner": "pegjs",
    "fileLocation": [
        "relative",
        "${workspaceFolder}"
    ],
    "pattern": [
        {
            "regexp": "^([^\\s].*)$",
            "file": 1
        },
        {
            "regexp": "^(\\d+):(\\d+):\\s*(.*)$",
            "line": 1,
            "column": 2,
            "message": 3,
            "loop": true
        }
    ]
},
{% endhighlight %}

With this in place, I can get the nice output from VSCode whenever I have an error in my pegjs file:

{:.center}
[![]({{ site_url }}/assets/vscode-pegjs.png)]({{ site_url }}/assets/vscode-pegjs.png)

Full source code is [available online](https://github.com/StephenCleary/json-filter-expr/blob/8a5c0fa113fce6cb065a72d93d4ff0ff79a389e5/.vscode/tasks.json).