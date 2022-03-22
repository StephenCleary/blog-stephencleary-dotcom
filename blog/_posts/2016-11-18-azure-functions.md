---
layout: post
title: "Azure Functions"
description: "Azure Functions are available."
---

This past week was [Microsoft's Connect 2016 conference](https://connectevent.microsoft.com/). There were a lot of announcements, but IMO the one that most stands out is that Azure Functions have been officially released.

I've been following the development of Azure Functions with quite a bit of interest recently. I think they'll have a *huge* long-term impact on the cloud ecosystem.

Azure Functions (on the surface) are roughly equivalent to Amazon's Lambdas, or Google's Cloud Functions, with the notable exception that only Azure Functions work with .NET natively. All three of these (in conjunction with other cloud services such as queues and distributed data stores) provide the backbone of what is commonly called "serverless computing". "Serverless computing" is a bit of a misnomer, since there *are* servers involved in running code (of course), but the idea is that those servers are only *borrowed* at a very abstract level. The idea is that you only pay for when your code is actually *running*.

Azure Functions have a ways to go before they're fully ready for adoption by most organizations (more detail below), but with this week's announcement, you can start using them today if you are determined enough. :)

In my opinion, Azure Functions will essentially replace WebJobs. Functions give you the most abstract (read: most productive and cheapest) option for writing backend code:

{:.center}
[![]({{ site_url }}/assets/Abstractions.png)]({{ site_url }}/assets/Abstractions.png)

To summarize: Azure Functions are awesome!

## My Own Adventure

I've been playing with Azure Functions for a while on an [open-source project](https://github.com/StephenClearyApps/NetStandardTypes) that will (eventually) provide a website for searching for netstandard-compatible .NET types across all of NuGet.

There are two Azure Functions in that system: one that periodically checks for new NuGet packages, and one that loads those packages into memory, extracts their types, and updates the search index.

This has been a fun and instructive project so far, because I'm using Azure Functions *and* Azure Search for the first time.

Azure Functions today are geared more towards simple implementations. Image resizing is the standard demo. Azure Functions work great when all the logic can fit into a single file. In my application, though, I have more advanced needs. I depend heavily on various NuGet packages (about 80 total), and I split up my own logic into several dlls. This is far beyond a "Hello, World" demo.

## Rough Spots

One of the main problems with .NET Azure Functions is the lack of tooling. .NET Azure Functions have a `csx` file as an entry point; this is a [C# script file](http://scriptcs.net/), which have been around for a while but haven't received any official support from Visual Studio or Visual Studio Code. Supposedly, there's some way to get IntelliSense working with VSCode, but I was unable to get it set up properly.

So, the `csx` file ends up with a very limited IntelliSense. This isn't the end of the world; the common workaround is to have all your actual logic in a separate dll, and your `csx` file literally just forwards to an entry point in that dll. Within the normal C# dll, you get full tooling support.

I am confident that this will be addressed in the near future, and Visual Studio (or at least Code) will fully understand `csx` files for Azure Functions.

The other main problem I've run into is deployment. Currently, Azure Functions has a very script-centered mentality, so their continuous deployment assumes that the source *is* what is deployed. It *will* build solutions/projects that it finds, but what ends up in your repository has to follow a very particular folder structure. I've created a [separate branch](https://github.com/StephenClearyApps/NetStandardTypes/tree/functions) in my repo that contains the build outputs, and this is what is actually hooked up to the Azure Functions continuous deployment system. Fow now, I'm just copying the outputs by hand into the separate branch (yeah, bad, I know...).

I am hopeful that this will be addressed in the near future, with some kind of output folder working for the continuous deployment process rather than assuming the code *is* the deployment.

Finally, I wasn't able to get references working to [NuGet packages installed by the Azure Functions continuous deployment system](https://docs.microsoft.com/en-us/azure/azure-functions/functions-reference-csharp#package-management?WT.mc_id=DT-MVP-5000058). As a workaround, I just copy all the dlls from my project's output directory (except the [ones already preloaded by the Azure Functions runtime](https://docs.microsoft.com/en-us/azure/azure-functions/functions-reference-csharp#referencing-external-assemblies?WT.mc_id=DT-MVP-5000058)) into the `bin` folder of my function.

## Tips

There's a few tips that I've picked up when getting started with Azure Functions.

- Ensure your [folder structure](https://docs.microsoft.com/en-us/azure/azure-functions/functions-reference#folder-structure?WT.mc_id=DT-MVP-5000058) is correct, and don't forget your hosts.json file.
- The Azure Functions portal has some awesome shortcuts (that really should be in other portals too, I'm looking at you, App Services!). The dev console and Kudu are particularly useful when setting up deployment.
- There are a few situations where restarting the Azure Functions host is necessary. For example, if you update a dll but do *not* update its version, then your Function won't "see" the updated dll. To force it to update, you can select "App Service Settings" from the Azure Functions portal and from there Restart your Azure Functions service.

## The Future

The future is bright for Azure Functions! This is one of the most important improvements to Azure, and really sets up Microsoft for domination in the cloud space. Once the tooling and deployment are ironed out a bit, Azure Functions will become the default go-to choice for backend work in the cloud.

I'm still learning Azure Functions myself, and you're welcome to join me - or just watch me struggle - [on GitHub](https://github.com/StephenClearyApps/NetStandardTypes).