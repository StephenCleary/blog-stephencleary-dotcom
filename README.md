# To build:

- Install PortableJekyll from http://www.madhur.co.in/blog/2013/07/20/buildportablejekyll.html
- Copy setpath.ps1 to the directory where you uninstalled that zip (e.g., D:\Programs\PortableJekyll)
- Install Python 2.
- Run "gem install jekyll"
- Run "gem install pygments"
- Run "python -m pip install -U pip setuptools"
- Run "easy_install Pygments"

To serve locally (with future posts):
> D:\Programs\PortableJekyll\setpath.ps1
> .\serve.ps1

To build:
> D:\Programs\PortableJekyll\setpath.ps1
> .\build.ps1

# Patterns:

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

Blah blah [with links](http://example.com){:.alert-link}), and blah blah.
</div>
````

Centered, linked image:

````
{:.center}
[![]({{ site_url }}/assets/image-file-name.jpg)]({{ site_url }}/assets/image-file-name.jpg)
````
