---
layout: post
title: "VS2010 WebDeploy and VS2010 Web Deployment Projects Beta 1"
---
## Edit 2010-04-15

**VS2010 will overwrite SourceWebPhysicalPath when re-loading a solution containing a WDP, so the fix below will not work. It is kept on this site for historical purposes only.**

VS2010 includes a [number of enhancements](http://live.visitmix.com/MIX10/Sessions/FT14) to web deployment, as [Scott](http://www.hanselman.com/blog/) points out. One of the coolest is the [web.config transformations](http://msdn.microsoft.com/en-us/library/dd465326(VS.100).aspx). They also included all kinds of functionality for automatically setting up IIS as part of a "deployment package" (getting pretty close to re-writing Windows Installer, actually).

The end result is a really powerful solution that a mom-and-pop web guy like me doesn't really need. I would like the ability to precompile a web application, and this is one of the "blind spots" of VS2010.

Web Deployment Projects (WDP) does have a (beta) release for VS2010, though it starts to show its age when lined up next to VS2010's web deployment. It does, however, have the capability to precompile web apps.

It's possible to have the best of both worlds: web.config transformations from VS2010 and precompiling from WDP. All you have to do is:

1. Create a VS2010 deployment to the local file system, in a "staging" directory. Set up the web.config transformations and make any other necessary changes.
1. Create a WDP for the web application. By default, it will copy the web application project itself rather than the deployment output.
1. Change the WDP project file to set the SourceWebPhysicalPath property to your staging directory.

Example:

    <SourceWebPhysicalPath>c:\staging\mywebsite</SourceWebPhysicalPath>

This will work as long as you don't need the advanced IIS application setup options available through VS2010 deployment. If you do, then you're probably better off incorporating ASP.NET compilation and merging as a post-build event or within the MSBuild file.

