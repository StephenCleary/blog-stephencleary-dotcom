---
layout: post
title: "Using ILMerge with .NET 4.0 and/or Rx"
---
## The Problem

Want to use ILMerge? Using .NET 4.0 (possibly with Rx)? Frustrated with "Unresolved assembly reference not allowed" and StackOverflowExceptions from ILMerge?



Then use one of these arguments:



- /targetplatform:v4,"%ProgramFiles%\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.0\Profile\Client"
- /targetplatform:v4,"%ProgramFiles%\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.0"


## The Long Answer

Microsoft provides a tool called [ILMerge](http://research.microsoft.com/en-us/people/mbarnett/ilmerge.aspx) that combines multiple assemblies into one. It hasn't been updated for the most recent version of .NET 4.0 yet, though, so if you attempt to merge .NET 4.0 assemblies, you'll end up with an error: _Unresolved assembly reference not allowed: System.Core._



This is because ILMerge understands .NET 4.0 versions v4.0.20926 (which I assume was an internal release between Beta 1 and Beta 2) and v4.0.20107 (which I assume was an internal release before Beta 1), but not v4.0.30319 (the RTW version). If ILMerge can't find the right framework libraries, it will fall back to using whatever version of the framework it's running (2.0 by default).



As of now, the Google search "ilmerge Unresolved assembly reference not allowed: System.Core" has a first hit of [this blog entry](http://geekswithblogs.net/michelotti/archive/2010/06/02/ilmerge---unresolved-assembly-reference-not-allowed-system.core.aspx), which suggests adding a /lib option with the "C:\Windows\Microsoft.NET\Framework\v4.0.30319" argument, or supplying an ILMerge config file to force it to use the 4.0 runtime. Unfortunately, this approach will not work in all situations.



To be specific, it does not work when trying to merge with [Microsoft's Rx library](http://msdn.microsoft.com/en-us/devlabs/ee794896.aspx). In fact, ILMerge will crash with a StackOverflowException.



The ILMerge page itself recommends using the /targetplatform switch instead of the /lib switch (passing "C:\Windows\Microsoft.NET\Framework\v4.0.30319"), and this does work for some situations (e.g., if you're only using System.Interactive.dll from Rx). However, it will still not work for any assembly referring to WPF. In particular, Rx's System.Reactive.dll has a reference to WindowsBase (to schedule observable streams to a Dispatcher), so ILMerge will fail with a familiar error message: _Unresolved assembly reference not allowed: WindowsBase._



It turns out that the WPF assemblies in the .NET 4.0 runtime are under a subdirectory of the runtime directory (C:\Windows\Microsoft.NET\Framework\v4.0.30319\WPF), and ILMerge does not search subdirectories. One solution is to specify the runtime directory with /targetplatform and specify the subdirectory with /lib, but a better solution is to specify the _reference assembly directory_ with /targetplatform.



If you're targeting .NET 4.0 client profile, then this would be _/targetplatform:v4,"%ProgramFiles%\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.0\Profile\Client"_. For the full .NET 4.0 framework, this would be _/targetplatform:v4,"%ProgramFiles%\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.0"_

