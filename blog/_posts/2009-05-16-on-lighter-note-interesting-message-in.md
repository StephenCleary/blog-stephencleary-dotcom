---
layout: post
title: "On a Lighter Note: Interesting Message in AutoChk"
tags: ["Lighter Notes"]
---


A lot of my recent blog entries for the last couple weeks have been almost articles, and I'm a bit tired. Writing is hard work!





So, this blog post is a bit in a "lighter" vein, just a little curiosity I found mucking about.





I'm doing some testing on a Win32 resource manager written purely in managed code. One of the tests is to load various resources from operating system files, and make sure everything looks "right". (Don't get me started on how undocumented some of this stuff is...)





Anyway, an unexpected message was found in Vista x64's "autochk.exe" (that thing that runs at bootup if your hard drive needs repairing). It's in the MessageTable resource, with message ID 0x427:



> This never gets printed.




How interesting.





Future interesting blog posts will doubtless follow.

