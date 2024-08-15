---
layout: post
title: "NuGet No Longer Supports Code Contracts"
---
Unfortunately, the latest two releases of NuGet (1.2 and 1.3) do not support Code Contracts. Previous versions of NuGet work fine, but the current version will add contract assemblies as references.

Workaround: When you add a library package reference, remove the reference(s) to any ".Contracts.dll" files.

This does affect every Nito library released to NuGet.

**Update 2011-05-26:** All Nito NuGet packages have been changed to drop Code Contract support; they have also all been changed to [include support for seamless source debugging](http://blog.davidebbo.com/2011/04/easy-way-to-publish-nuget-packages-with.html). When NuGet fixes their Code Contract support, the Nito packages will be re-released with both Code Contract and source debugging support.

**Update 2011-08-31:** Nuget 1.5 resumes support for Code Contracts! The Nito NuGet packages will be updated shortly.

