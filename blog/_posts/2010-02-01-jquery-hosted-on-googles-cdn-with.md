---
layout: post
title: "jQuery hosted on Google's CDN with IntelliSense"
---
ASP.NET MVC (which I'm using for my church's web site) comes packaged with [jQuery](http://jquery.com/); the appropriate JavaScript files are placed into the Scripts folder of the default MVC project. It is a good idea, though, to [let Google (or Microsoft) host jQuery for you](http://encosia.com/2008/12/10/3-reasons-why-you-should-let-google-host-jquery-for-you/) over their CDN (content delivery network).

However, you lose out on the cool jQuery IntelliSense! There are various workarounds to fix this, but most of them only succeed on VS2010 (which works much better with JavaScript IntelliSense). I'm still using VS2008; if you find yourself in the same boat, be sure to install [KB958502](http://code.msdn.microsoft.com/KB958502/Release/ProjectReleases.aspx?ReleaseId=1736) first. Then you can do this:

    <%= "<script type='text/jscript' src='http://ajax.googleapis.com/ajax/libs/jquery/1.3.2/jquery.min.js'></script>" %>
    <% if (false) { %><script type="text/javascript" src="../../App_Data/jquery-1.3.2.js"></script><% } %>

It's a nice little trick to make the Visual Studio editor ignore the jQuery on Google's CDN (because it's injected as a string into the ASP.NET response stream), while ignoring the local file when executed.

You may notice that I've stuck my local jquery-1.3.2.js and jquery-1.3.2-vsdoc.js files into the App_Data directory of my project. This is just because it's easier to Publish the web site that way (they don't actually get copied). The code in this post will work for any local JavaScript file, regardless of its location.

## Update (2010-02-03)

The original solution above used a comment block instead of an "if (false)" block:

    <%= "<script type='text/jscript' src='http://ajax.googleapis.com/ajax/libs/jquery/1.3.2/jquery.min.js'></script>" %>
    <% /* %><script type="text/javascript" src="../../App_Data/jquery-1.3.2.js"></script><% */ %>

Unfortunately, this caused C# IntelliSense to fail! The new solution allows both C# and JavaScript IntelliSense.

