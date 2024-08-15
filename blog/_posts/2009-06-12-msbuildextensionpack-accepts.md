---
layout: post
title: "MSBuild.ExtensionPack Accepts DynamicExecute Task"
---
Sorry for the lack of blog postings lately. I have been busy the last week polishing, refactoring, and documenting my first (and hopefully only) MSBuild custom task: DynamicExecute. DynamicExecute makes it possible to define and execute .NET methods using MSBuild 3.5.

[Mike Fourie](http://freetodev.spaces.live.com/default.aspx) has [accepted](http://freetodev.spaces.live.com/blog/cns!EC3C8F2028D842D5!927.entry) DynamicExecute for the next release of the [MSBuild Extension Pack](https://github.com/mikefourie-zz/MSBuildExtensionPack). It's available as Beta in the source code download until the next official release, when it will be included in the regular binaries.

DynamicExecute is similar to the inline tasks that are planned for MSBuild 4.0. Both DynamicExecute and inline tasks allow a build master to write C# code within the build script that is compiled and then executed as part of the build. There are a few differences, though:

- DynamicExecute does not support referencing an assembly in the GAC by a partial name. e.g., the CTP of MSBuild 4.0 allows an assembly reference of "System.Windows.Forms", whereas DynamicExecute requires the full name "System.Windows.Forms, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089". Local assemblies may be referenced by short name, however. (This is not actually considered a limitation of DynamicExecute; loading GAC assemblies by partial names is not a good idea).
- Once an inline task is created using the task factory, it may be referenced directly just like any other task. DynamicExecute methods must be called using the DynamicExecute task.

DynamicExecute does have one big advantage, though: it can be used now. :) Documentation (temporarily) is available online [here](http://www.msbuildextensionpack.com/help/3.5.4.0/temp/dynamicexecute.htm).

