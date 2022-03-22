---
layout: post
title: "Framework Profiles in .NET"
---
There are a lot of different .NET runtimes. There's the desktop framework, Windows Phone, Silverlight, and Windows Store. There's also a number of lesser-known frameworks. You can download [targeting packs](http://msdn.microsoft.com/en-US/hh454951.aspx?WT.mc_id=DT-MVP-5000058) to target different frameworks.

Every once in a while (usually while [working with NuGet](http://docs.nuget.org/docs/creating-packages/creating-and-publishing-a-package)), I find myself needing a refresher on the frameworks and profiles. It's a pain to look all this up again, so I'm collecting it here for future reference.

## FrameworkName and Version

A target framework is indicated by a [FrameworkName](http://msdn.microsoft.com/en-us/library/system.runtime.versioning.frameworkname.aspx?WT.mc_id=DT-MVP-5000058), which has three components: a required framework Identifier, a required framework [Version](http://msdn.microsoft.com/en-us/library/system.version.aspx?WT.mc_id=DT-MVP-5000058), and an optional framework Profile.

Both Identifier and Profile are always case-insensitive. The FrameworkName constructor allows some flexibility when it parses strings (and NuGet allows even more flexibility), but the canonical structure is as such: _Identifier_ ",Version=v" _Version_ [ ",Profile=" _Profile_ ].

Note that the Version applies to the Identifier; there is no version on a Profile.

## Getting the FrameworkName

You can type the following into the Package Manager Console window to view the target framework for any project:

    $p = Get-Project "MyProjectName"
    $p.Properties.Item("TargetFrameworkMoniker").Value

## Known Framework Identifiers and Profiles

### .NETFramework

The **.NETFramework** identifier is used for the regular desktop framework. For example, ".NETFramework,Version=v3.5" refers to .NET 3.5. You can also target specific updates, e.g., ".NETFramework,Version=v4.0.2" refers to [.NET 4.0 Platform Update 2](http://support.microsoft.com/kb/2544514).

If there is no profile specified, the framework refers to the full profile.

**Client** specifies the client profile; e.g., ".NETFramework,Version=v4.0,Profile=Client" refers to the .NET 4.0 Client Profile. Note that the client profile is not supported in .NET 4.5.

<div class="alert alert-info" markdown="1">
<i class="fa fa-hand-o-right fa-2x pull-left"></i>

Historical note: The **CompactFramework** profile specifies the .NET Compact Framework. This probably should have been its own identifier, but doesn't really matter anymore since Visual Studio 2008 was the last version to support CF directly.
</div>

### Silverlight

The **Silverlight** identifier is used for the Silverlight framework. For example, "Silverlight,Version=v4.0" refers to Silverlight 4.

If there is no profile specified, the framework refers to the desktop Silverlight framework.

**WindowsPhone** specifies the original Windows Phone profile; e.g., "Silverlight,Version=v3.0,Profile=WindowsPhone" refers to Windows Phone 7.0. I believe this profile is only applicable to Silverlight version 3.0.

**WindowsPhone71** specifies the newer Windows Phone profile; e.g., "Silverlight,Version=v4.0,Profile=WindowsPhone71" refers to Windows Phone 7.5 (Mango). That's not a typo: "7.5" came from marketing; the internal version numbers are all "7.1". However, some (all?) Microsoft tools <!-- like Portable Libraries --> will treat **WindowsPhone75** just like **WindowsPhone71**, so you may be able to get away with that. I believe this profile is only applicable to Silverlight version 4.0.

<div class="alert alert-info" markdown="1">
<i class="fa fa-hand-o-right fa-2x pull-left"></i>

The "Windows Phone" profiles of Silverlight are a historical oddity; starting with Windows Phone 8, Microsoft correctly gave Windows Phone its own identifier (see below).
</div>

### .NETCore

The **.NETCore** identifier is used for the new .NET framework for Windows Store applications. For example, ".NETCore,Version=v4.5" refers to the original Windows Store framework. Note that the first version of this framework will be 4.5. Also, remember that Windows Store is different than the desktop .NET 4.5 ".NETFramework,Version=v4.5", which was released at the same time.

### WindowsPhone

The **WindowsPhone** identifier is used for newer Windows Phone projects. For example, "WindowsPhone,Version=v8.0" refers to Windows Phone 8. Earlier versions of Windows Phone used special profiles of the **Silverlight** identifier.

There are no known profiles for the **WindowsPhone** identifier.

### Xbox and .NETMicroFramework

The **Xbox** identifier is used for XBox 360 projects. For example, "Xbox,Version=v4.0" refers to the XBox 360 platform. The **.NETMicroFramework** identifier targets (surprise) the .NET Micro Framework.

And that's all I know about those.

### .NETPortable

<div class="alert alert-danger" markdown="1">
<i class="fa fa-hand-o-right fa-2x pull-left"></i>

Please note that Portable Class Libraries are deprecated. They have been replaced by netstandard. PCLs are only useful if you need to support older platforms.
</div>

The **.NETPortable** identifier is used for [portable libraries](http://go.microsoft.com/fwlink/?LinkId=210823). Each portable library may run on a number of different platforms, indicated by a profile named **Profile_N_**. For example, ".NETPortable,Version=v4.0,Profile=Profile1" refers to a portable library that can run on .NET 4.0, Silverlight 4, Windows Phone 7, Metro, or XBox 360.

The "Version" for the .NETPortable identifier looks like it refers to the maximum version supported by all the platforms in that profile.

The .NETPortable identifier requires a profile. The profiles are listed below (or [click here to open in a separate page](https://portablelibraryprofiles.stephencleary.com/)):

<iframe src="//portablelibraryprofiles.stephencleary.com/" style="width:100%; height:40em;"></iframe>