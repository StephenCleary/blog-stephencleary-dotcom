---
layout: post
title: "Dynamically loading 32-bit or 64-bit code from a platform-agnostic executable"
---
Have you ever had a `BadImageFormatException`? It can happen if your platform-agnostic .NET code attempts to load your old x86 dll on a new x64 machine...

Most Microsoft native code dlls support x86, x64, and IA64 architectures. We have an interop dll for one of these (using managed C++'s IJW) that was recently updated to support x64 and IA64 as well as x86.

The main executable for this project is C#, platform-agnostic, and we wanted to keep it that way. Normally, the installer would just install the exe and then choose one of the interop dll's to install, based on the architecture. However, we had to create a demo system that could be run without installing - so, the question became: how does one detect the platform at runtime and bind to the appropriate dll?

Well, after spending a lot of time researching ways it wouldn't work (`<probing>`, `GetSystemInfo`, `AppendPrivatePath`), and rejecting setting up a second AppDomain (too much pain and overhead for one simple problem), we finally hit upon a ridiculously simple solution: handle the assembly's `ModuleResolve` event.

`IntPtr.Size` gives you a hint on how to proceed, and from there, `ModuleResolveEventHandler` just needs a bit of `try`...`catch` to distinguish x64 from IA64. You just have to be careful to handle re-entry situations (in case the dll really _is_ missing).

