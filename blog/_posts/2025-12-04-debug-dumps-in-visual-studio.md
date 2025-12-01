---
layout: post
title: "Debug Dumps in Visual Studio"
description: "Best practices for debugging memory dumps in Visual Studio."
---

This post is part of [C# Advent](https://csadvent.christmas/) organized by [@mgroves](https://x.com/mgroves).

Did you know that Visual Studio can debug memory dumps directly these days? It's actually been capable of that for many years. This post is a memory dump (heh) of my recommended settings when using Visual Studio to debug memory dumps.

## Memory Dumps?

If you're not familiar with the concept, you're going to love this! A memory dump is a file that contains a copy of the memory of a specific process (hence the name). Windows memory dump files generally have the `.dmp` extension.

This is useful when you have one of those super annoying bugs that only reproduce in production. And yes, this is generally a last resort! Normally you catch bugs by reproducing the issue locally and then you can just run the code in the debugger, which is an incredibly useful tool for... well... _debugging_. Sometimes reproducing the issue is challenging (or impossible), and a pretty decent fallback technique (I've found) is to just read the code. But if you can't find/reproduce the bug in a reasonable amount of time, taking a memory dump is a great technique to get additional insight.

## Capturing Memory Dumps

The best tool for this is [ProcDump](https://learn.microsoft.com/en-us/sysinternals/downloads/procdump?WT.mc_id=DT-MVP-5000058): you can capture dumps on demand, or capture based on some monitoring trigger (process crash, high CPU/memory usage, etc).

However, ProcDump requires access to the machine, so this works great only for on-prem or virtual machine deployments. If you're in an Azure WebApp situation, you can [capture memory dumps there as well](https://learn.microsoft.com/en-us/troubleshoot/azure/app-service/capture-memory-dumps-app-service?WT.mc_id=DT-MVP-5000058#collect-a-memory-dump-feature).

## Loading the Dump

Long gone are the days when you had to [install a different debugger](https://learn.microsoft.com/en-us/windows-hardware/drivers/debugger/?WT.mc_id=DT-MVP-5000058) (with... let's say "interesting" UI design choices), learn about `SOS.dll`, set up a symbol server cache linked to the upstream Microsoft symbol server, learn who John Robbins is, and start typing arcane commands while desperately searching [Tess Ferrandez's](https://www.tessferrandez.com/) totally awesone blog, hoping that she has already explained what you need to do!

Ah, good times!

Yeah, these days you can literally just _drag and drop the `dbg` file_ onto Visual Studio 🤯. But before you do that, I do have some tips to get the best experience.

## Recommended Visual Studio Settings

All screenshots are from VS2026, which is current at the time of writing this post.

### Just My Code

First, you should uncheck [Just My Code](https://learn.microsoft.com/visualstudio/debugger/just-my-code?view=visualstudio&WT.mc_id=DT-MVP-5000058). I just find it more useful to see the full picture when debugging a dump file.

{:.center}
[![Just My Code]({{ site_url }}/assets/VS2026-disable-just-my-code.png)]({{ site_url }}/assets/VS2026-disable-just-my-code.png)

### Symbols

The next step is to enable loading of symbols (e.g., `.pdb` files). Visual Studio uses symbols to decipher the compiled code, especially if it was compiled in `Release`. Symbols are often necessary to make any sense of the stack at all.

I have found the best option is to tell Visual Studio to load _all_ symbols when loading a dump file. This is **not** a setting I keep normally set, because loading all those symbols takes a _long_ time. But if I'm debugging a dump file, I always turn this on.

{:.center}
[![Load All Symbols]({{ site_url }}/assets/VS2026-load-all-symbols.png)]({{ site_url }}/assets/VS2026-load-all-symbols.png)

Next, you need to tell Visual Studio where to download those symbol files from. The two primary sources are [Microsoft](https://learn.microsoft.com/windows-hardware/drivers/debugger/microsoft-public-symbols?WT.mc_id=DT-MVP-5000058) and [NuGet](https://learn.microsoft.com/nuget/create-packages/symbol-packages-snupkg?WT.mc_id=DT-MVP-5000058#nugetorg-symbol-server) (`symbols.nuget.org`). Microsoft provides symbols for their OS (at least); the NuGet symbol server is the common place for .NET open source library symbols. These are both so common that you don't have to type them in anymore; they're just checkboxes in Visual Studio now.

{:.center}
[![Symbol Servers]({{ site_url }}/assets/VS2026-symbol-servers.png)]({{ site_url }}/assets/VS2026-symbol-servers.png)

Finally, I recommend setting up a local symbol cache. This is a local folder that will hold all those `pdb` files. I recommend using a folder on your [dev drive](https://learn.microsoft.com/windows/dev-drive/?WT.mc_id=DT-MVP-5000058) for this; mine is at `D:\Cache\symbols`, a sibling folder of `D:\Cache\nuget` which acts as my NuGet package cache. I like keeping them both under `D:\Cache` because I know I can just delete anything under there if I need more disk space.

{:.center}
[![Symbol Server Cache]({{ site_url }}/assets/VS2026-symbol-server-cache.png)]({{ site_url }}/assets/VS2026-symbol-server-cache.png)

### Sources

Now we have Visual Studio ready to aggressively load and debug all the symbols it can find. So let's help it find the actual source code!

I always enable [source server](https://learn.microsoft.com/visualstudio/debugger/specify-symbol-dot-pdb-and-source-files-in-the-visual-studio-debugger?view=visualstudio&WT.mc_id=DT-MVP-5000058#other-symbol-options-for-debugging) support. Source servers are a way for Visual Studio to load the exact version of the original source files actually used to compile the program. These days, source servers are primarily used for unmanaged code, although some legacy .NET libraries may use them.

{:.center}
[![Enable Source Server Support]({{ site_url }}/assets/VS2026-enable-source-server-support.png)]({{ site_url }}/assets/VS2026-enable-source-server-support.png)

While you're there, I also recommend enabling Git Credential Manager for Source Link. [Source Link](https://learn.microsoft.com/visualstudio/debugger/how-to-improve-diagnostics-debugging-with-sourcelink?view=visualstudio&WT.mc_id=DT-MVP-5000058) is the modern replacement for source servers, at least as far as .NET is concerned. Enabling GCM means you'll be able to pull the original source files from your private source code repository. Actually, I'm not sure why this isn't enabled by default; I can't think of a reason I'd want it off.

{:.center}
[![Enable Git Credential Manager for Source Link]({{ site_url }}/assets/VS2026-enable-GCM-sourcelink.png)]({{ site_url }}/assets/VS2026-enable-GCM-sourcelink.png)

Source Link itself is enabled by default, so you're good there.

At this point, if you have access to the source code, Visual Studio should now load it automagically.

## Load the Memory Dump

Visual Studio is now ready to load a memory dump file; you can just drag-and-drop it right into VS. Then go get a cup of coffee; loading all those symbols the first time is no joke, and it will take a while before it's ready for you! As your cache fills up, dump files will load faster; the first load is generally the slowest.

Visual Studio does ask you what kind of debugger you want to launch; I always choose `Mixed` - again, if I don't know what a problem is, I want to be able to see everything.

Once Visual Studio loads the dump file and the debugger, it will drop you into what looks like a debugging session. Of course, the application is not actually _running_, so you can't unpause or step the debugger or anything like that. However, you can poke around. As a general rule, I find the [Parallel Stacks](https://learn.microsoft.com/visualstudio/debugger/using-the-parallel-stacks-window?view=visualstudio&WT.mc_id=DT-MVP-5000058), [Threads](https://learn.microsoft.com/visualstudio/debugger/walkthrough-debugging-a-multithreaded-application?view=visualstudio&WT.mc_id=DT-MVP-5000058), [Call Stack](https://learn.microsoft.com/visualstudio/debugger/how-to-use-the-call-stack-window?view=visualstudio&WT.mc_id=DT-MVP-5000058), and [Modules](https://learn.microsoft.com/visualstudio/debugger/how-to-use-the-modules-window?view=visualstudio&WT.mc_id=DT-MVP-5000058) debugger windows to be good starting points on trying to figure out what's going on.

Happy debugging!