---
layout: post
title: "MSBuild: A Real-World Recursive Application"
---
I recently posted on this blog a "toy application" of MSBuild that [calculates factorials]({% post_url 2009-05-28-msbuild-factorial %}). Well, this weekend I was working on the new build script for the [Nito.Async library](http://nitoasync.codeplex.com/), and surprised myself by finding an actual real-world application for this code!

It turns out that this is useful when autogenerating publisher policies. Nito.Async follows a simple _major.minor_ version numbering scheme, where changes in _minor_ are always fully backwards-compatible and changes in _major_ never are. Publisher policies are a way of declaring backwards compatibility for strongly-named assemblies in the GAC (more info on [MSDN](http://msdn.microsoft.com/en-us/library/dz32563a.aspx?WT.mc_id=DT-MVP-5000058) and in [KB891030](http://support.microsoft.com/kb/891030)).

To autogenerate publisher policies for a version _maj.min_, the build script must build a separate dll for each version in the range [_maj_.0, _maj.min_). It turns out that the recursive behavior in my "factorial.proj" toy was exactly what I needed; I just changed the return value to concatenate a list of numbers instead of multiplying them together.

There was one other small hurdle to overcome; I had to perform a cross product of two different item groups (the list of "previous minor versions" and the list of library dlls). This is not exactly straightforward in MSBuild, and is a common question (just Google for "MSBuild cross product").

The updated build script for Nito.Async has been checked into CodePlex, so if you want to see the details on how this works, you can view it online [here](http://nitoasync.codeplex.com/SourceControl/changeset/view/17989#324550). I'm not going to post it on the blog here, for sake of space.

