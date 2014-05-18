---
layout: post
title: "ASP.NET MVC Faking Web Services"
tags: ["ASP.NET"]
---


At the most recent [Northern Michigan .NET User's Group](http://nmichigan.net/) meeting, Derek Smith gave a great introduction to ASP.NET MVC. One thing that he touched on in his presentation was the URL routing feature of MVC (which can be used independently of the rest of the MVC framework). In response to my question, he went on to explain that the MVC controllers (which are routed to by the URL routing) may return data instead of just a view (in particular, XML and JSON).





This means that ASP.NET MVC may be used to directly expose the controllers as web services, without all the configuration necessary to set up a web-friendly WCF interface hosted in IIS. In going the MVC route, one would lose the automatic metadata export for the WCF service, but one would gain ease of implementation and an easy-to-use "friendly URL" router.





This is an interesting concept (and Google reveals that it's one that's been kicking around a few years). Manning has a [free chapter covering URL routing](http://www.manning.com/palermo/) from their "ASP.NET MVC in Action" book (which I just bought; it is 50% off today only).

