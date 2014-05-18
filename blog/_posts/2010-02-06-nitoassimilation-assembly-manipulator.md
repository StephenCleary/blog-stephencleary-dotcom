---
layout: post
title: "Nito.Assimilation - the Assembly Manipulator"
tags: ["Rx", ".NET"]
---


There's a growing need for a nicer "cross-platform" story. The [Nito.Linq](http://nitolinq.codeplex.com/) library has temporarily stalled, due to the complexity of the project with regards to multiple platforms. Currently, we support:




- .NET 3.5 SP1 (Client profile compatible) with Rx
- .NET 3.5 SP1 (Client profile compatible) without Rx
- Compact Framework 3.5
- Silverlight 3 with Rx
- Silverlight 3 without Rx




However, we're going to have to add .NET 4.0 Beta 2 to the mix (with and without Rx), and other targets will only continue to add to what is already a mess. The current source code situation is not difficult, but Visual Studio is having an absolute fit.





Microsoft's Rx team had a similar problem; they have a single code base to run on a number of different platforms. Their solution was ingenious: they developed some in-house tools to manage the definitions of these various platforms and retarget an already-existing assembly.





Nito.Assimilation is the open-source equivalent. It's intended to be a tool for .NET library writers needing to target multiple versions/editions of the framework. The current roadmap is to provide a few primary tasks:




 1. Creating metadata assemblies from reference assemblies.
 1. Combining multiple metadata assemblies into a multitarget metadata assembly.
 1. Providing assembly targeting (converting a multitargeted library assembly into a targeted assembly). A "multitargeted library assembly" is one that has multitarget metadata assemblies as its assembly references; and a "targeted assembly" is an assembly that has been bound to a specific platform.
 1. (Possibly) Defining a standard means to define "profiles", "targets", and "multitargets".
 1. (Possibly) Providing assembly retargeting (converting a targeted assembly into a multitargeted library assembly).




The terminology can certainly get confusing! I'm still brainstorming for better words.





Anyway, Nito.Assimilation has reached its first milestone: it is capable of creating metadata assemblies from regular assemblies (along with XML documentation, of course). It is currently included in the source code of the [Nito.Linq](http://nitolinq.codeplex.com/) library (though eventually it will probably become its own project). It's been successfully used to create "metadata profiles" of the .NET 3.5 SP1 Client profile and the .NET 3.5 Compact Framework. Applications built against these metadata assemblies have working IntelliSense and execute without problems (binding to the real assemblies at runtime).





Enjoy!

