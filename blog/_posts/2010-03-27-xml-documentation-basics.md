---
layout: post
title: "XML Documentation: The Basics"
---
This is the first in a new topic of blog posts: dealing with XML documentation. XML docs are great, but suffer from a bit of a lack of tooling. I put most of my focus on using XML documentation for IntelliSense and CHM output, since there isn't an end-user distribution story for Help v2. (As of this writing, there isn't an end-user distribution story for Help v3 a.k.a. "Microsoft Help 1" either...)

IntelliSense help is easy; just tell Visual Studio to generate an XML documentation file along with the binary, and it will be detected and used as long as it stays in the same directory as the binary.

CHM help files are a bit more complex; you'll need a suite of tools to transform the XML into HTML, which is then compressed into a CHM file. Historically, there has been a lot of "project churn" as solutions were developed and abandoned. However, the current leader appears to have a lot of staying power, and even has concessions of former-leader compatibility.

This current leader is SandCastle. My preferred toolset includes:

- [Sandcastle](http://sandcastle.codeplex.com/) - the core "compiler" for xml documentation.
- [Sandcastle Styles - bug fixes and style updates for Sandcastle.](http://sandcastlestyles.codeplex.com/)
- [Sandcastle Help File Builder](http://shfb.codeplex.com/) - a nice GUI (now within Visual Studio) for working with Help project files.
- [HTML Help Workshop](http://msdn.microsoft.com/en-us/library/ms669985.aspx?WT.mc_id=DT-MVP-5000058) - the same, sad old buggy software we've been dependent on for years...

Once you've got all those installed, then you're ready to start authoring XML docs that can be used for CHM output as well as IntelliSense.

