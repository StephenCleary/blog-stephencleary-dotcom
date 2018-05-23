---
layout: post
title: "Managed Services Roundup"
seriesOrder: 1
series: "Managed Services"
seriesTitle: "Intro"
---
A [recent Tip of the Day](http://blogs.technet.com/b/tip_of_the_day/archive/2013/09/16/9-19-tip-of-the-day-interactive-services-detection.aspx) prompted me to update [one of my Managed Services posts]({% post_url 2011-05-12-managed-services-and-uis %}), and it seems like a good time to write out a quick summary.

First, what is a "managed service"? We're all familiar with the normal applications that Windows runs; there's the traditional desktop app as well as the newer Win8 apps. These apps interact with the user. But Windows also runs another kind of process: Win32 services. A Win32 service is an application (without a UI) that runs all the time, whether a user is logged on or not. And the .NET (full) framework supports writing Win32 services on .NET - what I call a "managed service."

I've got a number of resources on my blog for writing managed services:

- [Basics of managed services]({% post_url 2009-10-21-managed-windows-services-basics %}), which helps fill in the gaps in the .NET documentation (unfortunately, the MSDN docs are insufficient to actually write a managed service). I also cover the threading model and one common "gotcha" about the default current directory.
- [Services and the network]({% post_url 2009-10-22-windows-services-and-network %}), where I describe how drive mappings (a _per-user_ concept) have no place in Win32 services.
- [Services and user interfaces]({% post_url 2011-05-12-managed-services-and-uis %}), where I explain why services should _not_ have user interfaces.
