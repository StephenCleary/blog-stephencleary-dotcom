---
layout: post
title: "DotNet / NetCore Versions"
description: "An attempt to keep developer sanity when upgrading dotnet / netcore projects."
---

Well, the .NET Core 1.0 RTM release went out yesterday, and everything went just fine!

No, just kidding. When does the first major release of a new platform ever go just fine? :)

Currently, there's a bit of confusion out there because the *runtime* (netcore) RTM'd, but the *tooling* (dotnet) is still in preview. I'm writing down how all the pieces fit together (along with recent history) so I can keep it all straight in my head.

So, this is my short reference blog post pointing out which versions of which go with what. And stuff.

## The Right Bits

There have been several reports of odd Visual Studio behavior due to conflicting installs.

So, the **first** thing to do is to uninstall all old DNX or dotnet tooling, and any pre-release netcore runtimes. See the end of this post for uninstall helps.

Then, install the [proper bits](https://www.microsoft.com/net/core).

For a Windows machine running Visual Studio, the proper bits are Visual Studio 2015 Update 3 followed by the .NET Core for Visual Studio Official MSI Installer (which includes some RTM pieces *and* some preview pieces).

For a command-line setup, the proper bits are the .NET Core Runtime followed by the .NET Core SDK (which is in preview).

## DotNet and NetCore

As previously noted, .NET Core did RTM yesterday, but the DotNet tooling is still in preview. Here's a rundown of the recent releases and how they match:

<table class="table">
<thead>
<tr><th>.NET Core (runtime) Version</th><th>DotNet (tooling) Version</th></tr>
</thead>
<tbody>
<tr><td>RC1</td><td><span markdown="1">`1.0.0-rc1-update2` (RC1)</span></td></tr>
<tr><td>RC2</td><td><span markdown="1">`1.0.0-preview1-002702` (Preview 1)</span></td></tr>
<tr><td></td><td><span markdown="1">`1.0.0-beta-002071`</span></td></tr>
<tr><td>1.0.0 (RTM)</td><td><span markdown="1">`1.0.0-preview2-003121` (Preview 2)</span></td></tr>
</tbody>
</table>

The *tooling* version is what should be referenced in your `global.json`. So a `global.json` targeting the current tooling release would look like:

{% highlight json %}
{
    "projects": [ "src", "test" ],
    "sdk": {
		"version": "1.0.0-preview2-003121"
    }
}
{% endhighlight %}

The tooling version is also what gets reported from `dotnet --version`:

    > dotnet --version
	1.0.0-preview2-003121

<div class="alert alert-info" markdown="1">
<i class="fa fa-hand-o-right fa-2x pull-left"></i>

The naming definitely got messed up for the tooling.

It looks like it was originally tracking with the runtime release names (the "RC1" release names match), but then broke off on its own as "Preview 1" when the runtime went to "RC2".

Then there was an intermediate release between the two preview releases, to fix the tooling for F# projects. It was called "beta" for some reason.

So the tooling has gone from RC to Preview 1 to Beta to Preview 2. However, those were not "real" Beta/RC releases - they were really just incremental previews. Future versions will doubtless have "real" Beta/RC names.
</div>

## Installing Tooling on CI Machines

Continuous Integration build servers also need to have the appropriate tooling installed. For a variety of reasons, I prefer to have my .NET Core projects locally install the exact version of tooling that they need.

The DotNet tooling has supported "scripted installs" via PowerShell / Bash scripts for a long time. It's not too hard to install the tooling as part of your build script, if you know where the files are.

The basic idea is that you first download an "install" script and then invoke it with the desired "channel" and "version" to get the actual tooling.

<table class="table">
<thead>
<tr><th>DotNet (tooling) Version</th><th>Install Script Location</th><th>Install Script Invocation</th></tr>
</thead>
<tbody>
<tr><td><span markdown="1">`1.0.0-rc1-update2`</span></td><td><span markdown="1">[https://raw.githubusercontent.com/&#8203;aspnet/&#8203;Home/&#8203;dev/&#8203;dnvminstall.ps1](https://raw.githubusercontent.com/aspnet/Home/dev/dnvminstall.ps1)</span></td><td><span markdown="1">`dnvm install 1.0.0-rc1-update2`</span></td></tr>
<tr><td><span markdown="1">`1.0.0-preview1-002702`</span></td><td><span markdown="1">[https://raw.githubusercontent.com/&#8203;dotnet/&#8203;cli/&#8203;rel/&#8203;1.0.0-preview1/&#8203;scripts/&#8203;obtain/&#8203;dotnet-install.ps1](https://raw.githubusercontent.com/dotnet/cli/rel/1.0.0-preview1/scripts/obtain/dotnet-install.ps1)</span></td><td><span markdown="1">`dotnet-install.ps1 -Channel "preview" -Version "1.0.0-preview1-002702"`</span></td></tr>
<tr><td><span markdown="1">`1.0.0-beta-002071`</span></td><td><span markdown="1">[https://raw.githubusercontent.com/&#8203;dotnet/&#8203;cli/&#8203;rel/&#8203;1.0.0-preview1/&#8203;scripts/&#8203;obtain/&#8203;dotnet-install.ps1](https://raw.githubusercontent.com/dotnet/cli/rel/1.0.0-preview1/scripts/obtain/dotnet-install.ps1)</span></td><td><span markdown="1">`dotnet-install.ps1 -Channel "preview" -Version "1.0.0-beta-002071"`</span></td></tr>
<tr><td><span markdown="1">`1.0.0-preview2-003121`</span></td><td><span markdown="1">[https://raw.githubusercontent.com/&#8203;dotnet/&#8203;cli/&#8203;rel/&#8203;1.0.0-preview2/&#8203;scripts/&#8203;obtain/&#8203;dotnet-install.ps1](https://raw.githubusercontent.com/dotnet/cli/rel/1.0.0-preview2/scripts/obtain/dotnet-install.ps1)</span></td><td><span markdown="1">`dotnet-install.ps1 -Channel "preview" -Version "1.0.0-preview2-003121"`</span></td></tr>
</tbody>
</table>

The Bash install scripts are located in the same place, just with a `.sh` extension.

A full "install local" PowerShell script for the current tooling version would look something like this:

{% highlight PowerShell %}
# Download the "dotnet-install.ps1" script.
Invoke-WebRequest "https://raw.githubusercontent.com/dotnet/cli/rel/1.0.0-preview2/scripts/obtain/dotnet-install.ps1" -OutFile ".\dotnet-install.ps1"

# Decide to install into a ".dotnetcli" local directory.
$env:DOTNET_INSTALL_DIR = "$pwd\.dotnetcli"

# Do the actual install.
& .\dotnet-install.ps1 -Channel "preview" -Version "1.0.0-preview2-003121" -InstallDir "$env:DOTNET_INSTALL_DIR"
{% endhighlight %}

## Misc Tips

As a general rule, you want use the *highest* available release of the `NETStandard.Library` dependency (if you're using it), but the *lowest* possible `netstandard` target (to reach the largest number of platforms). For more details, see the conceptual links below.

## Helpful Links

Good luck!

### Conceptual

[All about netstandard](https://github.com/dotnet/corefx/blob/master/Documentation/architecture/net-platform-standard.md)

[Packages, Metapackages, and Frameworks](https://docs.microsoft.com/en-us/dotnet/articles/core/packages)

[Dependency Trimming](https://docs.microsoft.com/en-us/dotnet/articles/core/deploying/reducing-dependencies)

### Upgrading

[Upgrading to ASP.NET Core RTM from RC2 (Rick Strahl)](https://weblog.west-wind.com/posts/2016/Jun/27/Upgrading-to-ASPNET-Core-RTM-from-RC2)

[xUnit on .NET Core](https://xunit.github.io/docs/getting-started-dotnet-core.html)

[OpenCover requires the -oldStyle flag to work on the .NET Core 1.0.0/RTM runtime](https://github.com/dotnet/corefx/issues/8880)

[Moq 4.6.25-alpha is missing a dependency, so you need to declare it manually](http://stackoverflow.com/questions/37288385/moq-netcore-failing-for-net-core-rc2)

[[ASP.NET] Migrating from ASP.NET Core RC2 to ASP.NET Core 1.0](https://aspnet-aspnet.readthedocs-hosted.com/en/latest/migration/rc2-to-rtm.html)

[[ASP.NET] Migrating from ASP.NET 5 RC1 to ASP.NET Core 1.0](https://aspnet-aspnet.readthedocs-hosted.com/en/latest/migration/rc1-to-rtm.html)

[Migrating from DNX](https://docs.microsoft.com/en-us/dotnet/articles/core/migrating-from-dnx)

## Uninstalling Old versions

Some developers have reported that Visual Studio .NET Core RC2 will not uninstall without the MSI. If you need the MSI, you can get it from [here](https://download.microsoft.com/download/4/6/1/46116DFF-29F9-4FF8-94BF-F9BE05BE263B/DotNetCore.1.0.0.RC2-VS2015Tools.Preview1.exe).

Some developers have had problems with old uninstallers not cleaning up older versions of the tooling, and the problems with naming (where "RC1" and "RC2" tooling versions are *older* than "Preview 2") cause the wrong version of tooling to be found. The solution is to manually delete [the old versions](https://files.gitter.im/aspnet/Home/ufDJ/2016-06-28-20_04_03-sdk.png) from `%PROGRAMFILES%\dotnet\sdk`.