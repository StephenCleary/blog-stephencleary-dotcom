---
layout: post
title: "Grand Rapids Day of DotNET Slides Available"
---
Just got back from giving a talk at Day of .NET in Grand Rapids entitled "Designing Application Protocols for TCP/IP". I gave the same talk last year at BarCampGR, but this time I removed some of the introductory information and added an introduction to the Socket API at the end.

I did forget to mention during my talk that juggling multithreading concerns as well as TCP/IP concerns can be very challenging. I wrote some simple socket wrapper classes as part of the [Nito.Async](http://nitoasync.codeplex.com/) library (soon to be moved to the Nito.Communication library). These wrappers take care of all the multithreading concerns, so you can just focus on the TCP/IP concerns.

Slides are available [here](http://www.landmarkbaptist.ws/misc/Designing%20Application%20Protocols%20for%20TCPIP.pptx) (thanks to [Landmark Baptist Church](http://www.landmarkbaptist.ws/) for hosting).

