---
layout: post
title: "Rx Release Brings Nice Surprises!"
---
Just in case you haven't heard, last week the [Rx Framework](http://msdn.microsoft.com/en-us/devlabs/ee794896.aspx?WT.mc_id=DT-MVP-5000058) has been (pre)released!

This is an exciting event! And I don't get excited often... :)

Up until the release, what had come out of Microsoft amounted to this: the .NET 4.0 BCL would include the basic supporting types for Rx, but some of the useful operators (e.g., conversions back and forth between IEnumerable and IObservable) would not be included; those additional operators would be released as the Rx framework after .NET 4.0 comes out.

However, last week's Rx prerelease had a very nice surprise: Microsoft is (currently) planning to backport Rx to .NET 3.5 SP1! This includes not just all the Rx operators, but also the gaps that the Rx team been filling in LINQ.

Even better: the Rx backport also has backports of Tasks and PLINQ!

I think this is **awesome!** It enables software companies to take advantage of the tremendous Task/PLINQ/Rx enhancements without having to upgrade everything to .NET 4.0 / VS2010. There is still motivation to eventually move to .NET 4, of course: DLR, better distribution story, etc. But backporting Task/PLINQ/Rx is a great help to those of us who don't have time to upgrade everything just yet.

I've downloaded the (prerelease) Rx for .NET 3.5 SP1 backport. It appears to include:

- System.CoreEx.dll: General-purpose supporting types.
- System.Threading.dll: Tasks, PLINQ, and other .NET 4 concurrency stuff (Concurrent collections, Lazy initialization, new synchronization objects, etc).
- System.Reactive.dll: Rx
- System.Interactive.dll: Additions to LINQ, several of which were inspired by Rx operators.

Tasks, PLINQ, and other System.Threading items like the concurrent collections have been discussed elsewhere; for pre-release software, there's a surprising amount of documentation already available.

The Rx framework is a relative newcomer, and they're currently on fast-forward to get videos and blog posts out, so there's at least some documentation on Rx. Rx can be a bit confusing for many programmers because it's rooted in functional programming concepts, and the majority of programmers have traditionally only used imperative languages. Anyway, there's currently not much documentation, but keep an eye on the new [Rx Team blog](https://docs.microsoft.com/en-us/archive/blogs/rxteam/?WT.mc_id=DT-MVP-5000058); they're currently doing a video per day, with blog posts (on individual team member blogs) as well.

One piece of the Rx framework isn't getting the love that the others are getting, though: the LINQ extensions. I've decided to document at least a little bit on some of the operators that the Rx team has added to IEnumerable. I've been working on some of my own additional LINQ operators, and Jon Skeet has a [MoreLINQ](http://code.google.com/p/morelinq/) project in the same vein.

One place to bookmark for Rx is the [Rx Wiki](http://rxwiki.wikidot.com/), a community-run site about all things Rx.

