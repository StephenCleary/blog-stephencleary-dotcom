---
layout: post
title: "A New Web Site"
---
This weekend, I _finally_ updated [my personal home page](http://stephencleary.com) (it's only been about 5 years...). The new site uses several technologies I wanted to play around with.

The site is served by [GitHub Pages](http://pages.github.com/), though I don't use their automatic page generator thingy. Instead, on my local machine, I have two checkouts of the repo side-by-side: "master" and "gh-pages". The "master" contains [the actual source code for the site](https://github.com/StephenCleary/stephencleary-dotcom), while the "gh-pages" contains [the site itself](https://github.com/StephenCleary/stephencleary-dotcom/tree/gh-pages). This setup is [described by Chris Jacob in this Gist](https://gist.github.com/833223).

The site itself (technically) uses ASP.NET MVC and jQuery Mobile UI. Inspired by John Papa's recent [excellent blog series on single-page applications](http://johnpapa.net/spapost10), my site is also an SPA, only it's served statically, without any dynamic parts (yet). :)

To get the (dynamic) ASP.NET MVC converted to a (static) GitHub page, I run a [publish script](https://github.com/StephenCleary/stephencleary-dotcom/blob/master/Publish.ps1) that captures the ASP.NET MVC output and writes it (along with its content files) to the "gh-pages" directory. This way, I get the full ASP.NET MVC support (including NuGet packages and C#) without having to execute it on the server.

As part of this web page, I developed my own [C#-to-HTML formatter](https://github.com/StephenCleary/stephencleary-dotcom/blob/master/Api/Api/Business/CSharpFormatter.cs) wrapped in an [HTML helper](https://github.com/StephenCleary/stephencleary-dotcom/blob/master/StephenCleary.com/Helpers/CSharpHtmlHelper.cs). The C# formatter uses backticks (\`) to surround type names, and a backtick command (\`!) to surround highlighting. Both [inline](https://github.com/StephenCleary/stephencleary-dotcom/blob/master/StephenCleary.com/Views/Home/Index.cshtml#L58) and [block](https://github.com/StephenCleary/stephencleary-dotcom/blob/master/StephenCleary.com/Views/Home/Index.cshtml#L234) C# code segments are supported.

As soon as Azure Web Sites support .NET 4.5, I'll wrap up the formatter into an actual WebAPI.

