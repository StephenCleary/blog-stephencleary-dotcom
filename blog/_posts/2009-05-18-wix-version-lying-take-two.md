---
layout: post
title: "WiX Version Lying, Take Two"
---
In [an earlier post]({% post_url 2009-05-12-dealing-with-wix-data-files %}), I described how a friend of mine solved an installer update problem by _version lying_ about a data file. Just yesterday they ran into a similar but more sinister problem.



## The Problem, In a Nutshell

One of their libraries depended on an ocx control. This was added as a COM reference to the library. They built and distributed the first version of this software, being sure to include the "COM Interop" DLL that is automatically created by the compiler. They followed the Windows Installer component guidelines and made the interop dll and the dependent library separate components. It worked; no problems.



As part of a company initiative to sign their distributed code, they strong-named their library for the next release. Since strong-named assemblies may only load other strong-named assemblies, the compiler automatically strong-named its COM interop DLL on the next build.



The problem: The new (signed) COM interop DLL had the same version as the old (unsigned) COM interop DLL. The company realized while testing their release that if they upgraded a previous installation instead of performing a clean install, then their library would fail to load.



Of course, this is because during upgrades, Windows Installer will examine the file version, see that there are no differences, and skip installing that file. It ignores the last-modified time. The end result is that after an upgrade, the newer (signed) library was trying to load the new (signed) COM interop DLL, but only the old (unsigned) COM interop DLL was present.



## Attempted Solutions

**Set File.DefaultVersion**. This is the old "version lying" trick, which worked before. However, WiX will always ignore File.DefaultVersion as long as the file actually has version information (and in this case it does; the compiler always adds version information to the COM interop DLL). WiX can be instructed to ignore _all_ file version information, but this would require major (and ugly) changes to the installer files - essentially, they would have to do version lying on every single file in the msi. They decided this was not an acceptable option.



**Mess with COMReference**. The (poorly-documented) COMReference element in the MSBuild file does have VersionMajor and VersionMinor child elements, which do control the version of the COM interop DLL. Unfortunately, they also control the version of the COM/ActiveX object that is loaded. If they are set to anything other than the correct COM object version, the build fails; so, they cannot be used to set the COM interop DLL version.



**Write an installer transform**. Windows Installer does support the notion of "installer transforms", which are databases of changes to the installer database. An installer transform could do the version lying. That way, MSBuild would create the COM interop DLL, WiX would place it into the installer database along with its version information, and then the installer transform would overwrite the version information in the database. This is the cleanest solution, but ended up getting rejected due to lack of experience with installer transforms.



## The Accepted Solution

A few days ago, I posted [an interesting message I found in a Windows executable]({% post_url 2009-05-16-on-lighter-note-interesting-message-in %}) while testing out a Win32 resource manager. I talked my friend into letting me have a crack at their problem.



With a few modifications of this very, very pre-release code, I created a small console program that could change the version number of a PE/PE+ file ("PE/PE+" means EXEs, DLLs, OCXs, etc., either x86/AnyCPU or x64). They included it into their build process, and it worked quite nicely.



> Note: what they ended up doing is unconventional and not recommended, but it is a useful workaround for WiX upgrade scenarios. They do plan to change this in a future version of the installer.


The reason this works is because Windows Installer will only consider the version information on disk (for the original file) and in the installer database (for the updated file). In contrast, the .NET loader will verify the strong name signature against the .NET assembly version (AssemblyVersionAttribute) and ignores the file version (AssemblyFileVersionAttribute).

