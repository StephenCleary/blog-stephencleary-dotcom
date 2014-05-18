---
layout: post
title: "MS Bug: MvcBuildViews with One-Click Publish"
tags: ["ASP.NET", "MSbug"]
---


If you set MvcBuildViews to true in your web application project file, one-click publishing (at least to the local file system) will fail with a rather obscure error message:



> It is an error to use a section registered as allowDefinition='MachineToApplication' beyond application level.  This error can be caused by a virtual directory not being configured as an application in IIS.




The steps to reproduce this situation (in VS2010) are as follows:



1. File -> New Project -> ASP.NET MVC 2 Empty Web Application
1. Right-click project file -> Unload Project
1. Right-click project file -> Edit .csproj
1. Change <MvcBuildViews> to true and save
1. Right-click project file -> Reload project
1. Create Publish Settings (File System, c:\_test, Delete all existing files prior to publish)
1. Publish (succeeds)
1. Publish (fails); all future Publish or Build commands will fail




There is a simple workaround: run the Clean command on the web project, and then the Publish command will work again (once).





This bug has been posted to [Microsoft Connect](http://connect.microsoft.com/VisualStudio/feedback/details/556312/mvcbuildviews-does-not-play-well-with-one-click-publish).



## Why Post It Here?



Just this morning, I realized that two critical Microsoft bugs that I was watching were quietly removed from Microsoft Connect. These are bugs that I had invested a lot of time in discovering, exploring, reproducing, and detailing. One of them was closed recently as "external," so I posted a comment asking which group I should ask about the bug. Instead of replying, both of those bugs were silently removed from the entire Microsoft Connect system.





I attempted to retrieve the detailed bug reports, which had cost me many hours of development time. Google had refreshed since my bugs were censored, so the Google cache was no help; same with Bing. The Internet Archive wasn't even able to get those pages; apparently Microsoft Connect disabled the Archive's access way back in May of 2008.





The end result: all of the hard work I had put into those bug reports is gone. Some Microsoft team probably got brownie points for reducing their bug count. And I no longer trust Microsoft Connect. From now on, I will cross-post all Microsoft bugs to my blog.



## Update (2010-05-13):



After contacting Microsoft Connect technical support, my deleted bugs have been recovered. I do still plan to cross-post, however, just in case.

