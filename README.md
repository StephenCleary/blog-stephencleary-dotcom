# Getting Started

You need a Windows machine with Git, Docker, and VSCode installed.

```
git clone --recurse-submodules https://github.com/StephenCleary/blog-stephencleary-dotcom.git
git submodule foreach 'git checkout gh-pages'
```

## To Serve Locally

```
npm run serve
```

# Patterns

Link to other posts: `[text to be highlighted]({% post_url 2016-01-01-post-file-name %})`

Highlight code block:

````
{% highlight csharp %}
code here
{% endhighlight %}
````

Info block:

````
<div class="alert alert-info" markdown="1">
<i class="fa fa-hand-o-right fa-2x pull-left"></i>

Blah blah [with links](http://example.com){:.alert-link}, and blah blah.
</div>
````

Centered, linked image:

````
{:.center}
[![]({{ site_url }}/assets/image-file-name.jpg)]({{ site_url }}/assets/image-file-name.jpg)
````
